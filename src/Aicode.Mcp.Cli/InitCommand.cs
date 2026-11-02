using Aicode.Mcp.Core.Projects;

namespace Aicode.Mcp.Cli;

public static class InitCommand
{
    private const string DefaultTemplate = "dotnet-api";

    private const string SettingsYaml = """
        # Template folder under templates/ that new projects in this workspace follow.
        # Multi-project workspaces set sourceTemplate in each project.json instead.
        templatesPath: templates
        scaffold-mcp:
          hook:
            excludeGlobs: []
        """;

    private const string McpJson = """
        {
          "mcpServers": {
            "scaffold-mcp": { "command": "aicode-mcp", "args": ["mcp-serve", "--server", "scaffold"] },
            "architect-mcp": { "command": "aicode-mcp", "args": ["mcp-serve", "--server", "architect"] },
            "reviewer-mcp": {
              "command": "aicode-mcp",
              "args": ["mcp-serve", "--server", "reviewer", "--gitlab-token-env", "REPLACE_WITH_ENV_VAR_NAME"]
            }
          }
        }
        """;

    private const string HooksJson = """
        {
          "hooks": {
            "SessionStart": [
              { "hooks": [{ "type": "command", "command": "aicode-mcp init --quiet" }] }
            ],
            "PreToolUse": [
              { "matcher": "Write", "hooks": [{ "type": "command", "command": "aicode-mcp hook --type scaffold" }] }
            ],
            "PostToolUse": [
              { "matcher": "Edit|Write|MultiEdit", "hooks": [{ "type": "command", "command": "aicode-mcp hook --type architect" }] }
            ]
          }
        }
        """;

    /// <summary>
    /// Creates the settings and MCP config if missing, then prints the hooks to merge into .claude/settings.json.
    /// <paramref name="template"/> replaces the default template, and is ignored when a project already names one.
    /// Skip .mcp.json with <paramref name="writeMcpJson"/> when the servers are registered user-wide.
    /// </summary>
    public static int Run(
        string directory,
        TextWriter output,
        bool printHooks = true,
        string? bundledTemplates = null,
        string? template = null,
        bool writeMcpJson = true)
    {
        bundledTemplates ??= Path.Combine(AppContext.BaseDirectory, "templates");
        if (template is not null && Directory.Exists(bundledTemplates) && !Directory.Exists(Path.Combine(bundledTemplates, template)))
        {
            var known = string.Join(", ", Directory.EnumerateDirectories(bundledTemplates).Select(Path.GetFileName).Order());
            output.WriteLine($"Unknown template '{template}'. Bundled templates: {known}");
            return 1;
        }

        // Only fall back to the default template when no project.json or settings.yaml already names one.
        var settings = ProjectFinder.Find(directory) is null
            ? $"sourceTemplate: {template ?? DefaultTemplate}{Environment.NewLine}{SettingsYaml}"
            : SettingsYaml;
        WriteIfMissing(Path.Combine(directory, ".toolkit", "settings.yaml"), settings, output);
        if (writeMcpJson)
        {
            WriteIfMissing(Path.Combine(directory, ".mcp.json"), McpJson, output);
        }
        CopyTemplatesIfMissing(directory, bundledTemplates, output);
        if (printHooks)
        {
            output.WriteLine("Add this to .claude/settings.json to enable the hooks:");
            output.WriteLine(HooksJson);
        }
        return 0;
    }

    private static void CopyTemplatesIfMissing(string directory, string bundled, TextWriter output)
    {
        var target = Workspace.TemplatesPath(directory, Workspace.LoadSettings(directory));
        if (Directory.Exists(target) || !Directory.Exists(bundled))
        {
            return;
        }
        foreach (var file in Directory.EnumerateFiles(bundled, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, Path.GetRelativePath(bundled, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination)!);
            File.Copy(file, destination);
        }
        output.WriteLine($"Copied default templates to {target}");
    }

    private static void WriteIfMissing(string path, string content, TextWriter output)
    {
        if (File.Exists(path))
        {
            output.WriteLine($"Kept existing {path}");
            return;
        }

        Directory.CreateDirectory(Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content + Environment.NewLine);
        output.WriteLine($"Created {path}");
    }
}
