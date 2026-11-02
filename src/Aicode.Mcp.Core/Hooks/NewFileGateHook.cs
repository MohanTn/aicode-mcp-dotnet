using System.Text;
using Aicode.Mcp.Core.Infrastructure;
using Aicode.Mcp.Core.Projects;
using Aicode.Mcp.Core.Scaffold;

namespace Aicode.Mcp.Core.Hooks;

/// <summary>
/// PreToolUse hook. The first Write to a brand new file is denied once with the scaffold methods that fit the project.
/// The agent then uses a scaffold method, or repeats the Write, which is let through. Any error fails open.
/// </summary>
public class NewFileGateHook(ScaffoldService scaffold, string? logDirectory = null)
{
    public HookResponse PreToolUse(HookInput input)
    {
        try
        {
            return Evaluate(input);
        }
        catch (Exception ex)
        {
            return HookResponse.Skipped($"Hook error: {ex.Message}");
        }
    }

    private HookResponse Evaluate(HookInput input)
    {
        if (input.EventName != "PreToolUse" || input.ToolName != "Write" || input.FilePath is null)
        {
            return HookResponse.Skipped("Not a Write tool call");
        }

        var context = ProjectContext.Resolve(input.FilePath, input.Cwd);
        if (File.Exists(context.AbsolutePath))
        {
            return HookResponse.Skipped("Not a new file");
        }

        var workspaceRelative = Workspace.RelativePath(context.WorkspaceRoot, context.AbsolutePath);
        if (GlobMatcher.Matches(workspaceRelative, context.Settings.ScaffoldMcp?.Hook?.ExcludeGlobs))
        {
            return HookResponse.Allowed("File matches excludeGlobs, writing directly.");
        }

        var log = new ExecutionLog(input.SessionId, logDirectory);
        if (log.HasExecuted(context.AbsolutePath, HookResponse.Deny))
        {
            return HookResponse.Skipped("Scaffold methods were already shown for this file");
        }

        var template = context.Project?.SourceTemplate;
        if (template is null)
        {
            return HookResponse.Skipped("No sourceTemplate found for this file");
        }

        var config = scaffold.LoadConfig(context.TemplatesPath, template);
        if (config is null || GlobMatcher.Matches(context.RelativePath, config.Exclude))
        {
            return HookResponse.Allowed("No scaffold rules apply to this file, writing directly.");
        }

        if (config.Features.Count == 0)
        {
            return HookResponse.Allowed("This template has no scaffold methods, write the file directly.");
        }

        log.Append(context.AbsolutePath, "list-scaffold-methods", HookResponse.Deny);

        var message = new StringBuilder();
        message.AppendLine($"{context.RelativePath} is a new file. Prefer a scaffold method so it follows the '{template}' template.");
        message.AppendLine($"Call the scaffold-mcp tool use-scaffold-method with projectPath \"{context.Project!.Root}\" and one of:");
        foreach (var feature in config.Features)
        {
            message.AppendLine($"- {feature.Name}: {feature.Description}");
        }

        message.Append("If none of them fit, repeat the Write and it will go through.");
        return HookResponse.Denied(message.ToString());
    }
}
