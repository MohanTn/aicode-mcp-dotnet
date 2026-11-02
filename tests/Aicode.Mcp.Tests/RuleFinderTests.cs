using Aicode.Mcp.Core.Architect;

namespace Aicode.Mcp.Tests;

public class RuleFinderTests
{
    private readonly RuleFinder _finder = new();

    [Fact]
    public void Find_MatchesControllerAndMergesInheritedRules()
    {
        using var workspace = new TestWorkspace();

        var rules = _finder.Find(workspace.TemplatesPath, "dotnet-api", "Features/Orders/OrdersController.cs");

        Assert.NotNull(rules);
        Assert.Equal("controller", rules.Pattern);
        Assert.Contains(rules.MustNotDo, r => r.Rule.StartsWith("Put business logic in a controller"));
        Assert.Contains(rules.MustNotDo, r => r.Rule.Contains(".Result"));
        Assert.Contains(rules.MustDo, r => r.Rule.Contains("CancellationToken"));
    }

    [Fact]
    public void Find_SkipsHandlerInterfaces()
    {
        using var workspace = new TestWorkspace();

        Assert.Null(_finder.Find(workspace.TemplatesPath, "dotnet-api", "Features/Orders/CreateOrder/ICreateOrderHandler.cs"));
    }

    [Fact]
    public void Find_ReadsTheReactRules()
    {
        using var workspace = new TestWorkspace(sourceTemplate: "react-web");

        var rules = _finder.Find(workspace.TemplatesPath, "react-web", "src/orders_list/order_table/page.tsx");

        Assert.NotNull(rules);
        Assert.Equal("page", rules.Pattern);
        Assert.Contains(rules.MustNotDo, r => r.Rule.Contains("any"));
    }

    [Fact]
    public void Find_ReturnsNullWhenNoGlobMatches()
    {
        using var workspace = new TestWorkspace();

        Assert.Null(_finder.Find(workspace.TemplatesPath, "dotnet-api", "docs/readme.md"));
    }

    [Fact]
    public void Find_ReturnsNullForUnknownTemplate()
    {
        using var workspace = new TestWorkspace();

        Assert.Null(_finder.Find(workspace.TemplatesPath, "missing", "Features/A/AController.cs"));
    }
}
