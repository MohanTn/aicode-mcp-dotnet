using System.ComponentModel;
using Aicode.Mcp.Core.Architect;
using Aicode.Mcp.Core.Infrastructure;
using Aicode.Mcp.Core.Projects;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Aicode.Mcp.Servers;

[McpServerToolType]
public class ArchitectTools(RuleFinder ruleFinder, DesignPatternFinder patternFinder)
{
    [McpServerTool(Name = "get-file-design-pattern")]
    [Description("Get the design patterns and coding rules that apply to a file. Call this BEFORE editing the file.")]
    public string GetFileDesignPattern([Description("Path of the file you are about to edit")] string filePath)
    {
        var context = Resolve(filePath);
        return JsonOutput.Serialize(new
        {
            file_path = filePath,
            project = context.Project?.Name,
            source_template = context.Project?.SourceTemplate,
            design_patterns = Patterns(context),
            rules = Rules(context),
        });
    }

    [McpServerTool(Name = "review-code-change")]
    [Description("Get the rules to check a file against AFTER you edited it. Review your change against them and fix violations.")]
    public string ReviewCodeChange([Description("Path of the file you edited")] string filePath)
    {
        var context = Resolve(filePath);
        var rules = Rules(context);
        return JsonOutput.Serialize(new
        {
            file_path = filePath,
            project = context.Project?.Name,
            source_template = context.Project?.SourceTemplate,
            feedback = rules is null
                ? "No rules match this file. Apply general code quality checks."
                : "Check the file against these rules. must_do and must_not_do violations must be fixed.",
            rules,
        });
    }

    private static ProjectContext Resolve(string filePath)
    {
        var context = ProjectContext.Resolve(filePath, Directory.GetCurrentDirectory());
        if (context.Project?.SourceTemplate is null)
        {
            throw new McpException(
                $"No sourceTemplate found for {filePath}. Add it to project.json or .toolkit/settings.yaml.");
        }

        return context;
    }

    private IReadOnlyList<DesignPatternMatch> Patterns(ProjectContext context) =>
        patternFinder.Find(context.TemplatesPath, context.Project!.SourceTemplate!, context.RelativePath);

    private RuleMatch? Rules(ProjectContext context) =>
        ruleFinder.Find(context.TemplatesPath, context.Project!.SourceTemplate!, context.RelativePath);
}
