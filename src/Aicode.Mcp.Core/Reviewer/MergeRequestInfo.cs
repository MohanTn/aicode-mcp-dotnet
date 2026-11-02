namespace Aicode.Mcp.Core.Reviewer;

public record MergeRequestInfo(
    string Title,
    string? Description,
    string SourceBranch,
    string TargetBranch,
    string WebUrl,
    IReadOnlyList<MergeRequestChange> Changes);
