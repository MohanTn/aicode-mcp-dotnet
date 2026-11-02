using System.ComponentModel;
using Aicode.Mcp.Core.Infrastructure;
using Aicode.Mcp.Core.Reviewer;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Aicode.Mcp.Servers;

[McpServerToolType]
public class ReviewerTools(IMergeRequestProvider provider, MergeRequestReviewer reviewer)
{
    [McpServerTool(Name = "get-merge-request")]
    [Description("Fetch a GitLab merge request: title, branches and the raw diff of every changed file.")]
    public async Task<string> GetMergeRequest(
        [Description("GitLab project id or path, e.g. group/project")] string project,
        [Description("Merge request IID (the !number)")] int mergeRequestIid,
        CancellationToken cancellationToken = default)
    {
        var mr = await Call(() => provider.GetMergeRequestAsync(project, mergeRequestIid, cancellationToken));
        return JsonOutput.Serialize(mr);
    }

    [McpServerTool(Name = "review-merge-request")]
    [Description("Pair every changed file of a merge request with the architect rules for its template. Review each diff against its rules.")]
    public async Task<string> ReviewMergeRequest(
        [Description("GitLab project id or path, e.g. group/project")] string project,
        [Description("Merge request IID (the !number)")] int mergeRequestIid,
        [Description("Local checkout of the merge request branch. Defaults to the current directory.")] string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        var workspace = Path.GetFullPath(workspacePath ?? ".", Directory.GetCurrentDirectory());
        var review = await Call(() => reviewer.ReviewAsync(project, mergeRequestIid, workspace, cancellationToken));
        return JsonOutput.Serialize(review);
    }

    [McpServerTool(Name = "post-review-comments")]
    [Description("Post your review findings on the merge request as one comment.")]
    public async Task<string> PostReviewComments(
        [Description("GitLab project id or path, e.g. group/project")] string project,
        [Description("Merge request IID (the !number)")] int mergeRequestIid,
        [Description("The comment text in Markdown")] string body,
        CancellationToken cancellationToken = default)
    {
        if (string.IsNullOrWhiteSpace(body))
        {
            throw new McpException("body must not be empty.");
        }

        await CallVoid(() => reviewer.PostSummaryAsync(project, mergeRequestIid, body, cancellationToken));
        return JsonOutput.Serialize(new { posted = true, project, merge_request = mergeRequestIid });
    }

    private static async Task CallVoid(Func<Task> action) => await Call(async () =>
    {
        await action();
        return true;
    });

    private static async Task<T> Call<T>(Func<Task<T>> action)
    {
        try
        {
            return await action();
        }
        catch (Exception ex) when (ex is HttpRequestException or InvalidOperationException)
        {
            throw new McpException(ex.Message);
        }
    }
}
