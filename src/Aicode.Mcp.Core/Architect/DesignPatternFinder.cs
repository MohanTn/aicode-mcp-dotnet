using Aicode.Mcp.Core.Infrastructure;
using Aicode.Mcp.Core.Models;

namespace Aicode.Mcp.Core.Architect;

public class DesignPatternFinder
{
    /// <summary>Template patterns win. Global patterns (templates/architect.yaml) are used only when the template has none.</summary>
    public IReadOnlyList<DesignPatternMatch> Find(string templatesPath, string template, string relativePath)
    {
        var fromTemplate = Match(Path.Combine(templatesPath, template, "architect.yaml"), "template", relativePath);
        return fromTemplate.Count > 0
            ? fromTemplate
            : Match(Path.Combine(templatesPath, "architect.yaml"), "global", relativePath);
    }

    private static List<DesignPatternMatch> Match(string architectPath, string source, string relativePath)
    {
        var config = YamlLoader.Load<ArchitectConfig>(architectPath);
        return (config?.Features ?? new List<ArchitectFeature>())
            .Where(f => GlobMatcher.Matches(relativePath, f.Includes))
            .Select(f => new DesignPatternMatch(
                source,
                f.Name ?? f.Architecture ?? "",
                f.DesignPattern,
                f.Description,
                f.Includes))
            .ToList();
    }
}
