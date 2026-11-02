namespace Aicode.Mcp.Core.Scaffold;

public record ScaffoldResult(IReadOnlyList<string> CreatedFiles, IReadOnlyList<string> SkippedFiles);
