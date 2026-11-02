using System.Net;
using System.Text;
using Aicode.Mcp.Core.Reviewer;

namespace Aicode.Mcp.Tests;

public class GitLabProviderTests
{
    private sealed class StubHandler(HttpStatusCode status, string body) : HttpMessageHandler
    {
        public List<HttpRequestMessage> Requests { get; } = new();

        public List<string> Bodies { get; } = new();

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
        {
            Requests.Add(request);
            Bodies.Add(request.Content is null ? "" : await request.Content.ReadAsStringAsync(cancellationToken));
            return new HttpResponseMessage(status) { Content = new StringContent(body, Encoding.UTF8, "application/json") };
        }
    }

    private const string ChangesJson = """
        {
          "title": "Add orders",
          "description": "desc",
          "source_branch": "feature/orders",
          "target_branch": "main",
          "web_url": "https://gitlab.example/g/p/-/merge_requests/7",
          "changes": [
            { "old_path": "a.cs", "new_path": "a.cs", "new_file": true, "deleted_file": false, "renamed_file": false, "diff": "+x" },
            { "old_path": "b.cs", "new_path": "c.cs", "new_file": false, "deleted_file": false, "renamed_file": true, "diff": "" }
          ]
        }
        """;

    private static GitLabProvider Create(StubHandler handler, string? credential = "stub-credential") =>
        new(new HttpClient(handler), new GitLabOptions("https://gitlab.example/", credential));

    [Fact]
    public async Task GetMergeRequestAsync_CallsTheChangesEndpointAndParsesTheResponse()
    {
        var handler = new StubHandler(HttpStatusCode.OK, ChangesJson);

        var mr = await Create(handler).GetMergeRequestAsync("group/project", 7, CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(
            "https://gitlab.example/api/v4/projects/group%2Fproject/merge_requests/7/changes",
            request.RequestUri!.AbsoluteUri);
        Assert.Equal("stub-credential", request.Headers.GetValues("PRIVATE-TOKEN").Single());
        Assert.Equal("Add orders", mr.Title);
        Assert.Equal("feature/orders", mr.SourceBranch);
        Assert.Equal(2, mr.Changes.Count);
        Assert.Equal("added", mr.Changes[0].Status);
        Assert.Equal("renamed", mr.Changes[1].Status);
    }

    [Fact]
    public async Task PostNoteAsync_PostsTheBodyToTheNotesEndpoint()
    {
        var handler = new StubHandler(HttpStatusCode.Created, "{}");

        await Create(handler).PostNoteAsync("42", 7, "Looks good", CancellationToken.None);

        var request = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.EndsWith("/api/v4/projects/42/merge_requests/7/notes", request.RequestUri!.AbsoluteUri);
        Assert.Contains("\"body\":\"Looks good\"", handler.Bodies.Single());
    }

    [Fact]
    public async Task GetMergeRequestAsync_ThrowsAClearErrorWhenNoAccessTokenIsConfigured()
    {
        var handler = new StubHandler(HttpStatusCode.OK, ChangesJson);

        var error = await Assert.ThrowsAsync<InvalidOperationException>(() =>
            Create(handler, credential: null).GetMergeRequestAsync("42", 7, CancellationToken.None));

        Assert.Contains("not set", error.Message);
        Assert.Empty(handler.Requests);
    }

    [Fact]
    public async Task GetMergeRequestAsync_ThrowsWithTheStatusCodeOnFailure()
    {
        var handler = new StubHandler(HttpStatusCode.NotFound, "{\"message\":\"404 Not found\"}");

        var error = await Assert.ThrowsAsync<HttpRequestException>(() =>
            Create(handler).GetMergeRequestAsync("42", 7, CancellationToken.None));

        Assert.Contains("404", error.Message);
    }
}
