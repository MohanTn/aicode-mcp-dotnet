namespace Aicode.Mcp.Core.Reviewer;

public record MergeRequestReview(
    string Title,
    string SourceBranch,
    string TargetBranch,
    string WebUrl,
    string Instructions,
    IReadOnlyList<FileReview> Files,
    IReadOnlyList<string> FilesWithoutRules);
