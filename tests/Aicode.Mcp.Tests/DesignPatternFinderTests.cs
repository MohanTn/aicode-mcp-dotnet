using Aicode.Mcp.Core.Architect;

namespace Aicode.Mcp.Tests;

public class DesignPatternFinderTests
{
    [Fact]
    public void Find_ReturnsTemplatePatternsForMatchingFile()
    {
        using var workspace = new TestWorkspace();

        var patterns = new DesignPatternFinder().Find(
            workspace.TemplatesPath, "dotnet-api", "Features/Orders/CreateOrder/CreateOrderHandler.cs");

        var match = Assert.Single(patterns);
        Assert.Equal("Handler", match.Name);
        Assert.Equal("template", match.Source);
    }

    [Fact]
    public void Find_FallsBackToGlobalPatterns()
    {
        using var workspace = new TestWorkspace();
        workspace.Write("templates/architect.yaml", """
            features:
              - name: Docs
                design_pattern: Markdown docs
                includes:
                  - "docs/**/*.md"
            """);

        var patterns = new DesignPatternFinder().Find(workspace.TemplatesPath, "dotnet-api", "docs/guide.md");

        var match = Assert.Single(patterns);
        Assert.Equal("global", match.Source);
    }

    [Fact]
    public void Find_ReturnsEmptyWhenNothingMatches()
    {
        using var workspace = new TestWorkspace();

        Assert.Empty(new DesignPatternFinder().Find(workspace.TemplatesPath, "dotnet-api", "docs/guide.md"));
    }
}
