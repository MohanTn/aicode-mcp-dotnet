using Aicode.Mcp.Core.Architect;
using Aicode.Mcp.Core.Reviewer;

namespace Aicode.Mcp.Tests;

public class MergeRequestReviewerTests
{
    private static MergeRequestInfo Sample(params MergeRequestChange[] changes) =>
        new("Add orders", "desc", "feature/orders", "main", "https://gitlab.example/g/p/-/merge_requests/7", changes);

    private static MergeRequestChange Modified(string path, string diff = "@@ -1 +1 @@\n-a\n+b") =>
        new(path, path, false, false, false, diff);

    [Fact]
    public async Task ReviewAsync_PairsEachChangedFileWithItsRules()
    {
        using var workspace = new TestWorkspace();
        var provider = new FakeMergeRequestProvider(Sample(
            Modified("Features/Orders/OrdersController.cs"),
            Modified("docs/guide.md")));
        var reviewer = new MergeRequestReviewer(provider, new RuleFinder(), new DesignPatternFinder());

        var review = await reviewer.ReviewAsync("g/p", 7, workspace.Root, CancellationToken.None);

        Assert.Equal("Add orders", review.Title);
        var controller = review.Files.Single(f => f.Path == "Features/Orders/OrdersController.cs");
        Assert.Equal("controller", controller.Rules?.Pattern);
        Assert.Equal("dotnet-api", controller.SourceTemplate);
        Assert.NotEmpty(controller.DesignPatterns);
        Assert.Equal(new[] { "docs/guide.md" }, review.FilesWithoutRules);
    }

    [Fact]
    public async Task ReviewAsync_ListsDeletedFilesWithoutRules()
    {
        using var workspace = new TestWorkspace();
        var deleted = new MergeRequestChange("Features/Old.cs", "Features/Old.cs", false, true, false, "-gone");
        var reviewer = new MergeRequestReviewer(new FakeMergeRequestProvider(Sample(deleted)), new RuleFinder(), new DesignPatternFinder());

        var review = await reviewer.ReviewAsync("g/p", 7, workspace.Root, CancellationToken.None);

        var file = Assert.Single(review.Files);
        Assert.Equal("deleted", file.Status);
        Assert.Null(file.Rules);
        Assert.Empty(review.FilesWithoutRules);
    }

    [Fact]
    public async Task ReviewAsync_TruncatesLongDiffs()
    {
        using var workspace = new TestWorkspace();
        var longDiff = new string('x', MergeRequestReviewer.MaxDiffCharacters + 100);
        var reviewer = new MergeRequestReviewer(
            new FakeMergeRequestProvider(Sample(Modified("Features/Orders/OrdersController.cs", longDiff))),
            new RuleFinder(),
            new DesignPatternFinder());

        var review = await reviewer.ReviewAsync("g/p", 7, workspace.Root, CancellationToken.None);

        var file = Assert.Single(review.Files);
        Assert.True(file.DiffTruncated);
        Assert.Equal(MergeRequestReviewer.MaxDiffCharacters, file.Diff.Length);
    }

    [Fact]
    public async Task PostSummaryAsync_SendsTheCommentToTheProvider()
    {
        var provider = new FakeMergeRequestProvider(Sample());
        var reviewer = new MergeRequestReviewer(provider, new RuleFinder(), new DesignPatternFinder());

        await reviewer.PostSummaryAsync("g/p", 7, "Looks good", CancellationToken.None);

        Assert.Equal(new[] { "Looks good" }, provider.Notes);
    }
}
