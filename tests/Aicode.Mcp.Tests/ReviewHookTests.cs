using Aicode.Mcp.Core.Architect;
using Aicode.Mcp.Core.Hooks;

namespace Aicode.Mcp.Tests;

public class ReviewHookTests
{
    private static ReviewHook CreateHook(TestWorkspace workspace) =>
        new(new RuleFinder(), new DesignPatternFinder(), workspace.LogDirectory);

    private static HookInput EditInput(TestWorkspace workspace, string path, string tool = "Edit") =>
        new("PostToolUse", tool, path, workspace.Root, "s1");

    [Fact]
    public void PostToolUse_ReturnsRulesForAMatchingFile()
    {
        using var workspace = new TestWorkspace();
        var file = workspace.Write("Features/Orders/OrdersController.cs", "// controller");

        var response = CreateHook(workspace).PostToolUse(EditInput(workspace, file));

        Assert.Equal(HookResponse.Allow, response.Decision);
        Assert.Contains("Must not do:", response.Message);
        Assert.Contains("Put business logic in a controller", response.Message);
        Assert.Contains("Thin controller", response.Message);
    }

    [Fact]
    public void PostToolUse_SkipsARepeatReviewOfTheSameContent()
    {
        using var workspace = new TestWorkspace();
        var file = workspace.Write("Features/Orders/OrdersController.cs", "// controller");
        var hook = CreateHook(workspace);

        hook.PostToolUse(EditInput(workspace, file));
        var second = hook.PostToolUse(EditInput(workspace, file));

        Assert.Equal(HookResponse.Skip, second.Decision);
    }

    [Fact]
    public void PostToolUse_SkipsFilesWithNoMatchingRules()
    {
        using var workspace = new TestWorkspace();
        var file = workspace.Write("docs/guide.md", "# guide");

        Assert.Equal(HookResponse.Skip, CreateHook(workspace).PostToolUse(EditInput(workspace, file)).Decision);
    }

    [Fact]
    public void PostToolUse_SkipsFilesOutsideTheWorkingDirectory()
    {
        using var workspace = new TestWorkspace();
        using var other = new TestWorkspace();
        var file = other.Write("Features/Orders/OrdersController.cs", "// controller");

        Assert.Equal(HookResponse.Skip, CreateHook(workspace).PostToolUse(EditInput(workspace, file)).Decision);
    }

    [Fact]
    public void PostToolUse_SkipsToolsThatDoNotChangeFiles()
    {
        using var workspace = new TestWorkspace();
        var file = workspace.Write("Features/Orders/OrdersController.cs", "// controller");

        Assert.Equal(HookResponse.Skip, CreateHook(workspace).PostToolUse(EditInput(workspace, file, "Read")).Decision);
    }
}
