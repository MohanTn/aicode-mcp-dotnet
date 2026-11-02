using Aicode.Mcp.Core.Models;

namespace Aicode.Mcp.Core.Architect;

/// <summary>Rules that apply to one file, with inherited rules already merged in.</summary>
public record RuleMatch(
    string Pattern,
    string Description,
    IReadOnlyList<RuleItem> MustDo,
    IReadOnlyList<RuleItem> ShouldDo,
    IReadOnlyList<RuleItem> MustNotDo);
