using System.Text;
using Aicode.Mcp.Core.Architect;
using Aicode.Mcp.Core.Models;
using Aicode.Mcp.Core.Projects;

namespace Aicode.Mcp.Core.Hooks;

/// <summary>
/// PostToolUse hook. After Edit, Write or MultiEdit, hands the agent the RULES.yaml rules for that file
/// so it can check its own change. It never blocks, and any error fails open.
/// </summary>
public class ReviewHook(RuleFinder ruleFinder, DesignPatternFinder patternFinder, string? logDirectory = null)
{
    private static readonly HashSet<string> MutatingTools = new() { "Edit", "Write", "MultiEdit" };
    private static readonly TimeSpan Debounce = TimeSpan.FromSeconds(3);

    public HookResponse PostToolUse(HookInput input)
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
        if (input.EventName != "PostToolUse" || input.FilePath is null || input.ToolName is null ||
            !MutatingTools.Contains(input.ToolName))
        {
            return HookResponse.Skipped("Not a file edit");
        }

        var context = ProjectContext.Resolve(input.FilePath, input.Cwd);
        var cwd = Path.GetFullPath(input.Cwd);
        if (!context.AbsolutePath.StartsWith(cwd + Path.DirectorySeparatorChar, StringComparison.Ordinal))
        {
            return HookResponse.Skipped("File is outside the working directory");
        }

        var template = context.Project?.SourceTemplate;
        if (template is null)
        {
            return HookResponse.Skipped("No sourceTemplate found for this file");
        }

        var log = new ExecutionLog(input.SessionId, logDirectory);
        if (log.WasRecentlyReviewed(context.AbsolutePath, Debounce))
        {
            return HookResponse.Skipped("Reviewed within the last 3 seconds");
        }

        if (!log.HasChangedSinceLastReview(context.AbsolutePath))
        {
            return HookResponse.Skipped("File unchanged since the last review");
        }

        var rules = ruleFinder.Find(context.TemplatesPath, template, context.RelativePath);
        if (rules is null)
        {
            return HookResponse.Skipped("No rules match this file");
        }

        var patterns = patternFinder.Find(context.TemplatesPath, template, context.RelativePath);
        log.Append(context.AbsolutePath, "review", HookResponse.Allow);
        return HookResponse.Allowed(Summarize(context.RelativePath, rules, patterns));
    }

    private static string Summarize(string relativePath, RuleMatch rules, IReadOnlyList<DesignPatternMatch> patterns)
    {
        var sb = new StringBuilder();
        sb.AppendLine($"Check {relativePath} against these rules ({rules.Pattern}: {rules.Description}). Fix any violation now.");
        AppendSection(sb, "Must do", rules.MustDo);
        AppendSection(sb, "Must not do", rules.MustNotDo);
        AppendSection(sb, "Should do", rules.ShouldDo);
        foreach (var pattern in patterns)
        {
            sb.AppendLine($"Design pattern {pattern.Name}: {pattern.DesignPattern}");
        }

        return sb.ToString().TrimEnd();
    }

    private static void AppendSection(StringBuilder sb, string title, IReadOnlyList<RuleItem> items)
    {
        if (items.Count == 0)
        {
            return;
        }

        sb.AppendLine($"{title}:");
        foreach (var item in items)
        {
            sb.AppendLine($"- {item.Rule}");
        }
    }
}
