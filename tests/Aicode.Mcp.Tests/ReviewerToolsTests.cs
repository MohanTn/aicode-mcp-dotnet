using System.Text.Json;
using Aicode.Mcp.Core.Architect;
using Aicode.Mcp.Core.Reviewer;
using Aicode.Mcp.Servers;
using ModelContextProtocol;

namespace Aicode.Mcp.Tests;

public class ReviewerToolsTests
{
    private static readonly MergeRequestInfo Sample = new(
        "Add orders",
        "desc",
        "feature/orders",
        "main",
        "https://gitlab.example/g/p/-/merge_requests/7",
        new[]
        {
            new MergeRequestChange("Features/Orders/OrdersController.cs", "Features/Orders/OrdersController.cs", false, false, false, "+x"),
            new MergeRequestChange("docs/guide.md", "docs/guide.md", true, false, false, "+y"),
        });

    private static ReviewerTools Create(FakeMergeRequestProvider provider) =>
        new(provider, new MergeRequestReviewer(provider, new RuleFinder(), new DesignPatternFinder()));

    [Fact]
    public async Task GetMergeRequest_ReturnsTitleBranchesAndChanges()
    {
        var tools = Create(new FakeMergeRequestProvider(Sample));

        using var doc = JsonDocument.Parse(await tools.GetMergeRequest("g/p", 7));

        Assert.Equal("Add orders", doc.RootElement.GetProperty("title").GetString());
        Assert.Equal("feature/orders", doc.RootElement.GetProperty("source_branch").GetString());
        var changes = doc.RootElement.GetProperty("changes");
        Assert.Equal(2, changes.GetArrayLength());
        Assert.Equal("Features/Orders/OrdersController.cs", changes[0].GetProperty("new_path").GetString());
        Assert.Equal("added", changes[1].GetProperty("status").GetString());
    }

    [Fact]
    public async Task GetMergeRequest_TurnsProviderFailuresIntoMcpErrors()
    {
        var tools = Create(new FakeMergeRequestProvider(Sample) { Failure = new HttpRequestException("GitLab returned 404") });

        var error = await Assert.ThrowsAsync<McpException>(() => tools.GetMergeRequest("g/p", 7));

        Assert.Contains("404", error.Message);
    }

    [Fact]
    public async Task ReviewMergeRequest_PairsFilesWithRules()
    {
        using var workspace = new TestWorkspace();
        var tools = Create(new FakeMergeRequestProvider(Sample));

        using var doc = JsonDocument.Parse(await tools.ReviewMergeRequest("g/p", 7, workspace.Root));

        var file = doc.RootElement.GetProperty("files")[0];
        Assert.Equal("Features/Orders/OrdersController.cs", file.GetProperty("path").GetString());
        Assert.Equal("controller", file.GetProperty("rules").GetProperty("pattern").GetString());
        Assert.Equal("docs/guide.md", doc.RootElement.GetProperty("files_without_rules")[0].GetString());
        Assert.Contains("post-review-comments", doc.RootElement.GetProperty("instructions").GetString());
    }

    [Fact]
    public async Task ReviewMergeRequest_TurnsMissingCredentialsIntoMcpErrors()
    {
        using var workspace = new TestWorkspace();
        var failure = new InvalidOperationException("The GitLab access token is not set.");
        var tools = Create(new FakeMergeRequestProvider(Sample) { Failure = failure });

        var error = await Assert.ThrowsAsync<McpException>(() => tools.ReviewMergeRequest("g/p", 7, workspace.Root));

        Assert.Contains("not set", error.Message);
    }

    [Fact]
    public async Task PostReviewComments_PostsTheBodyAndConfirms()
    {
        var provider = new FakeMergeRequestProvider(Sample);

        using var doc = JsonDocument.Parse(await Create(provider).PostReviewComments("g/p", 7, "Looks good"));

        Assert.True(doc.RootElement.GetProperty("posted").GetBoolean());
        Assert.Equal(7, doc.RootElement.GetProperty("merge_request").GetInt32());
        Assert.Equal(new[] { "Looks good" }, provider.Notes);
    }

    [Theory]
    [InlineData("")]
    [InlineData("   ")]
    public async Task PostReviewComments_RejectsAnEmptyBody(string body)
    {
        var provider = new FakeMergeRequestProvider(Sample);

        await Assert.ThrowsAsync<McpException>(() => Create(provider).PostReviewComments("g/p", 7, body));

        Assert.Empty(provider.Notes);
    }

    [Fact]
    public async Task PostReviewComments_TurnsProviderFailuresIntoMcpErrors()
    {
        var tools = Create(new FakeMergeRequestProvider(Sample) { Failure = new HttpRequestException("GitLab returned 403") });

        var error = await Assert.ThrowsAsync<McpException>(() => tools.PostReviewComments("g/p", 7, "hi"));

        Assert.Contains("403", error.Message);
    }
}
