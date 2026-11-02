namespace Aicode.Mcp.Core.Reviewer;

public record MergeRequestChange(string OldPath, string NewPath, bool NewFile, bool DeletedFile, bool RenamedFile, string Diff)
{
    public string Status => DeletedFile ? "deleted" : NewFile ? "added" : RenamedFile ? "renamed" : "modified";
}
