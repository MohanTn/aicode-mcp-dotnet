using Aicode.Mcp.Core.Hooks;

namespace Aicode.Mcp.Tests;

public class ExecutionLogTests
{
    [Fact]
    public void HasExecuted_IsTrueAfterAppendWithSameDecision()
    {
        using var workspace = new TestWorkspace();
        var log = new ExecutionLog("s1", workspace.LogDirectory);

        log.Append("/a/b.cs", "list-scaffold-methods", HookResponse.Deny);

        Assert.True(log.HasExecuted("/a/b.cs", HookResponse.Deny));
        Assert.False(log.HasExecuted("/a/b.cs", HookResponse.Allow));
        Assert.False(log.HasExecuted("/a/other.cs", HookResponse.Deny));
    }

    [Fact]
    public void Sessions_AreKeptSeparate()
    {
        using var workspace = new TestWorkspace();

        new ExecutionLog("s1", workspace.LogDirectory).Append("/a.cs", "review", HookResponse.Allow);

        Assert.False(new ExecutionLog("s2", workspace.LogDirectory).HasExecuted("/a.cs", HookResponse.Allow));
    }

    [Fact]
    public void ReviewTracking_DetectsRecentReviewAndContentChanges()
    {
        using var workspace = new TestWorkspace();
        var file = workspace.Write("a.cs", "one");
        var log = new ExecutionLog("s1", workspace.LogDirectory);

        Assert.True(log.HasChangedSinceLastReview(file));
        log.Append(file, "review", HookResponse.Allow);

        Assert.True(log.WasRecentlyReviewed(file, TimeSpan.FromSeconds(3)));
        Assert.False(log.HasChangedSinceLastReview(file));
        File.WriteAllText(file, "two");
        Assert.True(log.HasChangedSinceLastReview(file));
    }

    [Fact]
    public void SessionIdWithPathCharacters_StaysInsideTheLogFolder()
    {
        using var workspace = new TestWorkspace();

        new ExecutionLog("../../evil", workspace.LogDirectory).Append("/a.cs", "review", HookResponse.Allow);

        Assert.Single(Directory.GetFiles(workspace.LogDirectory));
    }
}
