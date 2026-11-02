using Aicode.Mcp.Core.Reviewer;

namespace Aicode.Mcp.Tests;

/// <summary>In-memory merge request provider. Set <see cref="Failure"/> to make every call throw.</summary>
public sealed class FakeMergeRequestProvider(MergeRequestInfo mergeRequest) : IMergeRequestProvider
{
    public List<string> Notes { get; } = new();

    public Exception? Failure { get; init; }

    public Task<MergeRequestInfo> GetMergeRequestAsync(string project, int mergeRequestIid, CancellationToken cancellationToken) =>
        Failure is null ? Task.FromResult(mergeRequest) : Task.FromException<MergeRequestInfo>(Failure);

    public Task PostNoteAsync(string project, int mergeRequestIid, string body, CancellationToken cancellationToken)
    {
        if (Failure is not null)
        {
            return Task.FromException(Failure);
        }

        Notes.Add(body);
        return Task.CompletedTask;
    }
}
