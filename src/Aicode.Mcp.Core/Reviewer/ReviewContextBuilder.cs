using System.Text.RegularExpressions;
using Aicode.Mcp.Core.Architect;
using Aicode.Mcp.Core.Projects;
using Aicode.Mcp.Core.Scaffold;

namespace Aicode.Mcp.Core.Reviewer;

/// <summary>The scaffold.yaml feature template a changed file would have been generated from.</summary>
public record FeatureTemplateMatch(string Method, string TemplateFile, string Content, bool Truncated);

/// <summary>Everything a reviewer needs to check one changed file against its repo's own templates.</summary>
public record FileReviewContext(
    string Path,
    string Template,
    RuleMatch? Rules,
    IReadOnlyList<DesignPatternMatch> DesignPatterns,
    FeatureTemplateMatch? FeatureTemplate);

/// <summary>
/// Pairs each changed file with the RULES.yaml rules, architect.yaml patterns and scaffold.yaml
/// feature template that apply to it. Files with no sourceTemplate or nothing matched are left out.
/// </summary>
public class ReviewContextBuilder(RuleFinder ruleFinder, DesignPatternFinder patternFinder, ScaffoldService scaffold)
{
    public const int MaxTemplateCharacters = 4096;

    private static readonly Regex Placeholder = new(@"\{\{(.*?)\}\}", RegexOptions.Compiled);

    public IReadOnlyList<FileReviewContext> Build(IEnumerable<string> paths, string workspacePath)
    {
        var result = new List<FileReviewContext>();
        foreach (var path in paths)
        {
            var context = ProjectContext.Resolve(path, workspacePath);
            var template = context.Project?.SourceTemplate;
            if (template is null)
            {
                continue;
            }

            var rules = ruleFinder.Find(context.TemplatesPath, template, context.RelativePath);
            var patterns = patternFinder.Find(context.TemplatesPath, template, context.RelativePath);
            var feature = FindFeatureTemplate(context.TemplatesPath, template, context.RelativePath);
            if (rules is null && patterns.Count == 0 && feature is null)
            {
                continue;
            }

            result.Add(new FileReviewContext(path, template, rules, patterns, feature));
        }

        return result;
    }

    /// <summary>The feature include whose rendered target best fits the path. The one with the most literal characters wins.</summary>
    private FeatureTemplateMatch? FindFeatureTemplate(string templatesPath, string template, string relativePath)
    {
        var config = scaffold.LoadConfig(templatesPath, template);
        if (config is null)
        {
            return null;
        }

        var best = config.Features
            .SelectMany(feature => feature.Includes.Select(include => (feature.Name, Include: ScaffoldService.ParseInclude(include))))
            .Select(candidate => (candidate.Name, candidate.Include, Score: Score(candidate.Include.Target, relativePath)))
            .Where(candidate => candidate.Score >= 0)
            .OrderByDescending(candidate => candidate.Score)
            .FirstOrDefault();
        if (best.Include is null)
        {
            return null;
        }

        var file = Path.Combine(templatesPath, template, best.Include.Source);
        if (!File.Exists(file))
        {
            return null;
        }

        var content = File.ReadAllText(file);
        var truncated = content.Length > MaxTemplateCharacters;
        return new FeatureTemplateMatch(best.Name, best.Include.Source, truncated ? content[..MaxTemplateCharacters] : content, truncated);
    }

    /// <summary>
    /// Literal character count of the target when it matches the path, otherwise -1.
    /// Each {{ variable }} matches one path segment part, and a repeated variable must repeat the same text.
    /// </summary>
    public static int Score(string target, string relativePath)
    {
        var groups = new Dictionary<string, string>();
        var pattern = "^";
        var last = 0;
        foreach (Match placeholder in Placeholder.Matches(target))
        {
            pattern += Regex.Escape(target[last..placeholder.Index]);
            var key = placeholder.Groups[1].Value.Trim();
            if (groups.TryGetValue(key, out var name))
            {
                pattern += $@"\k<{name}>";
            }
            else
            {
                name = $"v{groups.Count}";
                groups[key] = name;
                pattern += $"(?<{name}>[^/]+)";
            }

            last = placeholder.Index + placeholder.Length;
        }

        pattern += Regex.Escape(target[last..]) + "$";
        if (!Regex.IsMatch(relativePath.Replace('\\', '/'), pattern))
        {
            return -1;
        }

        return Placeholder.Replace(target, "").Length;
    }
}
