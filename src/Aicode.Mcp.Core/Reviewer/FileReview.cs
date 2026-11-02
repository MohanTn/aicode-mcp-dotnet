using Aicode.Mcp.Core.Architect;

namespace Aicode.Mcp.Core.Reviewer;

/// <summary>One changed file with its diff and the rules and patterns the agent should check it against.</summary>
public record FileReview(
    string Path,
    string Status,
    string? SourceTemplate,
    string Diff,
    bool DiffTruncated,
    IReadOnlyList<DesignPatternMatch> DesignPatterns,
    RuleMatch? Rules);
