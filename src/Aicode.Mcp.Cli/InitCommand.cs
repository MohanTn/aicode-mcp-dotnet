namespace Aicode.Mcp.Cli;

public static class InitCommand
{
    private const string SettingsYaml = """
        # Template folder under templates/ that new projects in this workspace follow.
        # Multi-project workspaces set sourceTemplate in each project.json instead.
        sourceTemplate: dotnet-api
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
            "PreToolUse": [
              { "matcher": "Write", "hooks": [{ "type": "command", "command": "aicode-mcp hook --type scaffold" }] }
            ],
            "PostToolUse": [
              { "matcher": "Edit|Write|MultiEdit", "hooks": [{ "type": "command", "command": "aicode-mcp hook --type architect" }] }
            ]
          }
        }
        """;

    /// <summary>Creates the settings and MCP config if missing, then prints the hooks to merge into .claude/settings.json.</summary>
    public static int Run(string directory, TextWriter output)
    {
        WriteIfMissing(Path.Combine(directory, ".toolkit", "settings.yaml"), SettingsYaml, output);
        WriteIfMissing(Path.Combine(directory, ".mcp.json"), McpJson, output);
        output.WriteLine("Add this to .claude/settings.json to enable the hooks:");
        output.WriteLine(HooksJson);
        return 0;
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
