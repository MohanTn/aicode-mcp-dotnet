namespace Aicode.Mcp.Core.Reviewer;

/// <summary>A code host that can serve merge request diffs and accept review comments. GitLab is the first implementation.</summary>
public interface IMergeRequestProvider
{
    /// <param name="project">Numeric project id or "group/project" path.</param>
    Task<MergeRequestInfo> GetMergeRequestAsync(string project, int mergeRequestIid, CancellationToken cancellationToken);

    Task PostNoteAsync(string project, int mergeRequestIid, string body, CancellationToken cancellationToken);
}
