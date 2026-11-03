using System.Text.Json;
using Aicode.Mcp.Cli;
using Aicode.Mcp.Core.Architect;
using Aicode.Mcp.Core.Reviewer;
using Aicode.Mcp.Core.Scaffold;

namespace Aicode.Mcp.Tests;

public class ReviewContextBuilderTests
{
    private static ReviewContextBuilder Builder() => new(new RuleFinder(), new DesignPatternFinder(), new ScaffoldService());

    [Fact]
    public void Build_PairsAHandlerWithItsRulesPatternAndFeatureTemplate()
    {
        using var workspace = new TestWorkspace();

        var file = Assert.Single(Builder().Build(new[] { "Features/Orders/CreateOrder/CreateOrderHandler.cs" }, workspace.Root));

        Assert.Equal("dotnet-api", file.Template);
        Assert.Equal("handler", file.Rules?.Pattern);
        Assert.Contains(file.Rules!.MustDo, r => r.Rule.Contains("CancellationToken"));
        Assert.Contains(file.DesignPatterns, p => p.Name == "Handler");
        Assert.Equal("add-use-case", file.FeatureTemplate?.Method);
        Assert.Equal("features/Handler.cs.liquid", file.FeatureTemplate?.TemplateFile);
        Assert.Contains("{{ useCase }}Handler", file.FeatureTemplate!.Content);
    }

    [Fact]
    public void Build_PicksTheInterfaceTemplateForAnInterfaceFile()
    {
        using var workspace = new TestWorkspace();

        var file = Assert.Single(Builder().Build(new[] { "Features/Orders/CreateOrder/ICreateOrderHandler.cs" }, workspace.Root));

        Assert.Equal("features/IHandler.cs.liquid", file.FeatureTemplate?.TemplateFile);
    }

    [Fact]
    public void Build_SkipsFilesWithNothingMatched()
    {
        using var workspace = new TestWorkspace();

        Assert.Empty(Builder().Build(new[] { "README.md" }, workspace.Root));
    }

    [Fact]
    public void Build_SkipsEverythingWithoutASourceTemplate()
    {
        using var workspace = new TestWorkspace(sourceTemplate: null);

        Assert.Empty(Builder().Build(new[] { "Features/Orders/CreateOrder/CreateOrderHandler.cs" }, workspace.Root));
    }

    [Theory]
    [InlineData("Features/{{ feature }}/{{ useCase }}/{{ useCase }}Handler.cs", "Features/Orders/CreateOrder/CreateOrderHandler.cs", true)]
    [InlineData("Features/{{ feature }}/{{ useCase }}/{{ useCase }}Handler.cs", "Features/Orders/CreateOrder/DeleteOrderHandler.cs", false)]
    [InlineData("Features/{{ feature }}/{{ feature }}Controller.cs", "Features/Orders/Sub/OrdersController.cs", false)]
    [InlineData("src/{{ routePage | snakeCase }}/{{ componentName | snakeCase }}/page.tsx", "src/orders/order_table/page.tsx", true)]
    public void Score_MatchesRenderedTargetsOnly(string target, string path, bool matches)
    {
        Assert.Equal(matches, ReviewContextBuilder.Score(target, path) >= 0);
    }

    [Fact]
    public void Command_PrintsSnakeCaseJson()
    {
        using var workspace = new TestWorkspace();
        var output = new StringWriter();

        var exitCode = ReviewContextCommand.Run(workspace.Root, new[] { "Features/Orders/OrdersController.cs" }, output);

        Assert.Equal(0, exitCode);
        using var doc = JsonDocument.Parse(output.ToString());
        var file = doc.RootElement.GetProperty("files")[0];
        Assert.Equal("controller", file.GetProperty("rules").GetProperty("pattern").GetString());
        Assert.True(file.GetProperty("rules").GetProperty("must_not_do").GetArrayLength() > 0);
        Assert.Equal("features/Controller.cs.liquid", file.GetProperty("feature_template").GetProperty("template_file").GetString());
    }
}
