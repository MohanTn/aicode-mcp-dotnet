namespace Aicode.Mcp.Core.Architect;

/// <summary>A design pattern from architect.yaml whose includes match a file. Source is "template" or "global".</summary>
public record DesignPatternMatch(
    string Source,
    string Name,
    string DesignPattern,
    string? Description,
    IReadOnlyList<string> Includes);
