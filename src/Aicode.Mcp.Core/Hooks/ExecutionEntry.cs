namespace Aicode.Mcp.Core.Hooks;

public record ExecutionEntry(string FilePath, string Operation, string Decision, string? Checksum, DateTimeOffset Timestamp);
