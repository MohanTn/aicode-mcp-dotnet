using Aicode.Mcp.Core.Infrastructure;
using Aicode.Mcp.Core.Models;

namespace Aicode.Mcp.Core.Architect;

public class RuleFinder
{
    /// <summary>
    /// Looks in templates/{template}/RULES.yaml first, then templates/RULES.yaml.
    /// A section matches when its globs match the project-relative path. Sections without globs only serve as inherit targets.
    /// </summary>
    public RuleMatch? Find(string templatesPath, string template, string relativePath)
    {
        var candidates = new[]
        {
            Path.Combine(templatesPath, template, "RULES.yaml"),
            Path.Combine(templatesPath, "RULES.yaml"),
        };

        foreach (var rulesPath in candidates)
        {
            var config = YamlLoader.Load<RulesConfig>(rulesPath);
            var section = config?.Rules?.FirstOrDefault(s =>
                s.Globs is { Count: > 0 } && GlobMatcher.Matches(relativePath, s.Globs));
            if (config is not null && section is not null)
            {
                return Resolve(config, section);
            }
        }

        return null;
    }

    private static RuleMatch Resolve(RulesConfig config, RuleSection section)
    {
        var mustDo = new List<RuleItem>();
        var shouldDo = new List<RuleItem>();
        var mustNotDo = new List<RuleItem>();
        Collect(config, section, new HashSet<string>(), mustDo, shouldDo, mustNotDo);
        return new RuleMatch(section.Pattern, section.Description, Distinct(mustDo), Distinct(shouldDo), Distinct(mustNotDo));
    }

    private static void Collect(
        RulesConfig config,
        RuleSection section,
        HashSet<string> visited,
        List<RuleItem> mustDo,
        List<RuleItem> shouldDo,
        List<RuleItem> mustNotDo)
    {
        if (!visited.Add(section.Pattern))
        {
            return;
        }

        foreach (var parentName in section.Inherits ?? new List<string>())
        {
            var parent = config.Rules.FirstOrDefault(r => r.Pattern == parentName);
            if (parent is not null)
            {
                Collect(config, parent, visited, mustDo, shouldDo, mustNotDo);
            }
        }

        mustDo.AddRange(section.MustDo ?? new List<RuleItem>());
        shouldDo.AddRange(section.ShouldDo ?? new List<RuleItem>());
        mustNotDo.AddRange(section.MustNotDo ?? new List<RuleItem>());
    }

    private static List<RuleItem> Distinct(List<RuleItem> items) =>
        items.GroupBy(i => i.Rule).Select(g => g.First()).ToList();
}
