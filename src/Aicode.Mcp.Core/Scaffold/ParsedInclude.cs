namespace Aicode.Mcp.Core.Scaffold;

/// <summary>One scaffold.yaml include: where to read from, where to write to (both unrendered), and when to apply it.</summary>
public record ParsedInclude(string Source, string Target, bool HasArrow, IReadOnlyDictionary<string, string> Conditions);
