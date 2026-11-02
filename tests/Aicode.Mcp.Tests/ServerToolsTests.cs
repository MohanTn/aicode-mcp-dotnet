using System.Text.Json;
using Aicode.Mcp.Core.Architect;
using Aicode.Mcp.Core.Scaffold;
using Aicode.Mcp.Servers;
using ModelContextProtocol;

namespace Aicode.Mcp.Tests;

public class ServerToolsTests
{
    private static ArchitectTools Architect() => new(new RuleFinder(), new DesignPatternFinder());

    [Fact]
    public void GetFileDesignPattern_ReturnsPatternsAndRules()
    {
        using var workspace = new TestWorkspace();
        var file = workspace.PathOf("Features/Orders/CreateOrder/CreateOrderHandler.cs");

        using var doc = JsonDocument.Parse(Architect().GetFileDesignPattern(file));

        Assert.Equal("dotnet-api", doc.RootElement.GetProperty("source_template").GetString());
        Assert.Equal("Handler", doc.RootElement.GetProperty("design_patterns")[0].GetProperty("name").GetString());
        Assert.Equal("handler", doc.RootElement.GetProperty("rules").GetProperty("pattern").GetString());
    }

    [Fact]
    public void ReviewCodeChange_ReturnsRulesToCheckAgainst()
    {
        using var workspace = new TestWorkspace(sourceTemplate: "react-web");
        var file = workspace.PathOf("src/orders/order_table/page.tsx");

        using var doc = JsonDocument.Parse(Architect().ReviewCodeChange(file));

        Assert.Contains("must_do", doc.RootElement.GetProperty("feedback").GetString());
        Assert.Equal("page", doc.RootElement.GetProperty("rules").GetProperty("pattern").GetString());
    }

    [Fact]
    public void GetFileDesignPattern_ThrowsWhenNoTemplateIsConfigured()
    {
        using var workspace = new TestWorkspace(sourceTemplate: null);

        Assert.Throws<McpException>(() => Architect().GetFileDesignPattern(workspace.PathOf("a.cs")));
    }

    [Fact]
    public void ListScaffoldingMethods_ListsTheTemplateFeatures()
    {
        using var workspace = new TestWorkspace();

        using var doc = JsonDocument.Parse(new ScaffoldTools(new ScaffoldService()).ListScaffoldingMethods(workspace.Root));

        Assert.Equal("add-use-case", doc.RootElement.GetProperty("methods")[0].GetProperty("name").GetString());
    }

    [Fact]
    public async Task UseScaffoldMethod_CreatesFilesInTheProject()
    {
        using var workspace = new TestWorkspace();
        var tools = new ScaffoldTools(new ScaffoldService());

        var json = await tools.UseScaffoldMethod(
            workspace.Root,
            "add-use-case",
            """{ "rootNamespace": "Orders.Api", "feature": "Orders", "useCase": "CreateOrder" }""");

        using var doc = JsonDocument.Parse(json);
        Assert.True(File.Exists(workspace.PathOf("Features/Orders/CreateOrder/CreateOrderRequest.cs")));
        Assert.True(doc.RootElement.GetProperty("created_files").GetArrayLength() > 0);
    }

    [Fact]
    public async Task UseScaffoldMethod_ReportsMissingVariablesAsAnMcpError()
    {
        using var workspace = new TestWorkspace();
        var tools = new ScaffoldTools(new ScaffoldService());

        var error = await Assert.ThrowsAsync<McpException>(() =>
            tools.UseScaffoldMethod(workspace.Root, "add-use-case", """{ "feature": "Orders" }"""));

        Assert.Contains("useCase", error.Message);
    }

    [Fact]
    public async Task UseScaffoldMethod_RejectsAnUnknownMethod()
    {
        using var workspace = new TestWorkspace();

        await Assert.ThrowsAsync<McpException>(() =>
            new ScaffoldTools(new ScaffoldService()).UseScaffoldMethod(workspace.Root, "nope"));
    }
}
