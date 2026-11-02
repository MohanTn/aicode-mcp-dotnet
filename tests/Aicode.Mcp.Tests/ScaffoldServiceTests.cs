using Aicode.Mcp.Core.Models;
using Aicode.Mcp.Core.Scaffold;

namespace Aicode.Mcp.Tests;

public class ScaffoldServiceTests
{
    private readonly ScaffoldService _service = new();

    [Fact]
    public void ParseInclude_SplitsArrowAndConditions()
    {
        var parsed = ScaffoldService.ParseInclude("a/B.cs.liquid->out/{{ x }}.cs?withController=true&kind=api");

        Assert.Equal("a/B.cs.liquid", parsed.Source);
        Assert.Equal("out/{{ x }}.cs", parsed.Target);
        Assert.True(parsed.HasArrow);
        Assert.Equal("true", parsed.Conditions["withController"]);
        Assert.Equal("api", parsed.Conditions["kind"]);
    }

    [Fact]
    public void ParseInclude_WithoutArrowUsesSameSourceAndTarget()
    {
        var parsed = ScaffoldService.ParseInclude("src/a.ts");

        Assert.Equal("src/a.ts", parsed.Source);
        Assert.Equal("src/a.ts", parsed.Target);
        Assert.False(parsed.HasArrow);
    }

    [Fact]
    public async Task GenerateAsync_CreatesDotnetUseCaseFiles()
    {
        using var workspace = new TestWorkspace();
        var entry = _service.LoadConfig(workspace.TemplatesPath, "dotnet-api")!.Features.Single(f => f.Name == "add-use-case");
        var variables = new Dictionary<string, object?>
        {
            ["rootNamespace"] = "Orders.Api",
            ["feature"] = "Orders",
            ["useCase"] = "CreateOrder",
            ["withController"] = true,
        };

        var result = await _service.GenerateAsync(workspace.TemplatesPath, "dotnet-api", entry, workspace.Root, variables, CancellationToken.None);

        Assert.Contains("Features/Orders/CreateOrder/CreateOrderHandler.cs", result.CreatedFiles);
        Assert.Contains("Features/Orders/OrdersController.cs", result.CreatedFiles);
        Assert.Contains("Tests/Features/Orders/CreateOrderHandlerTests.cs", result.CreatedFiles);
        var handler = await File.ReadAllTextAsync(workspace.PathOf("Features/Orders/CreateOrder/CreateOrderHandler.cs"));
        Assert.Contains("namespace Orders.Api.Features.Orders.CreateOrder;", handler);
        var controller = await File.ReadAllTextAsync(workspace.PathOf("Features/Orders/OrdersController.cs"));
        Assert.Contains("[Route(\"api/orders\")]", controller);
    }

    [Fact]
    public async Task GenerateAsync_SkipsControllerWhenConditionIsFalse()
    {
        using var workspace = new TestWorkspace();
        var entry = _service.LoadConfig(workspace.TemplatesPath, "dotnet-api")!.Features[0];
        var variables = new Dictionary<string, object?>
        {
            ["rootNamespace"] = "Orders.Api",
            ["feature"] = "Orders",
            ["useCase"] = "CreateOrder",
            ["withController"] = false,
        };

        var result = await _service.GenerateAsync(workspace.TemplatesPath, "dotnet-api", entry, workspace.Root, variables, CancellationToken.None);

        Assert.DoesNotContain("Features/Orders/OrdersController.cs", result.CreatedFiles);
        Assert.False(File.Exists(workspace.PathOf("Features/Orders/OrdersController.cs")));
    }

    [Fact]
    public async Task GenerateAsync_NeverOverwritesExistingFiles()
    {
        using var workspace = new TestWorkspace();
        var entry = _service.LoadConfig(workspace.TemplatesPath, "dotnet-api")!.Features[0];
        var variables = new Dictionary<string, object?>
        {
            ["rootNamespace"] = "Orders.Api",
            ["feature"] = "Orders",
            ["useCase"] = "CreateOrder",
        };
        workspace.Write("Features/Orders/CreateOrder/CreateOrderHandler.cs", "// mine");

        var result = await _service.GenerateAsync(workspace.TemplatesPath, "dotnet-api", entry, workspace.Root, variables, CancellationToken.None);

        Assert.Equal("// mine", await File.ReadAllTextAsync(workspace.PathOf("Features/Orders/CreateOrder/CreateOrderHandler.cs")));
        Assert.Contains(result.SkippedFiles, s => s.StartsWith("Features/Orders/CreateOrder/CreateOrderHandler.cs"));
    }

    [Fact]
    public async Task GenerateAsync_ThrowsWhenRequiredVariablesAreMissing()
    {
        using var workspace = new TestWorkspace();
        var entry = _service.LoadConfig(workspace.TemplatesPath, "dotnet-api")!.Features[0];

        var error = await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateAsync(
            workspace.TemplatesPath, "dotnet-api", entry, workspace.Root,
            new Dictionary<string, object?> { ["feature"] = "Orders" }, CancellationToken.None));

        Assert.Contains("rootNamespace", error.Message);
        Assert.Contains("useCase", error.Message);
    }

    [Fact]
    public async Task GenerateAsync_CreatesReactComponentWithSnakeCaseFolders()
    {
        using var workspace = new TestWorkspace(sourceTemplate: "react-web");
        var entry = _service.LoadConfig(workspace.TemplatesPath, "react-web")!.Features.Single();
        var variables = new Dictionary<string, object?> { ["routePage"] = "OrdersList", ["componentName"] = "OrderTable" };

        var result = await _service.GenerateAsync(workspace.TemplatesPath, "react-web", entry, workspace.Root, variables, CancellationToken.None);

        Assert.Contains("src/orders_list/order_table/page.tsx", result.CreatedFiles);
        Assert.Contains("src/orders_list/order_table/useOrderTable.ts", result.CreatedFiles);
        var page = await File.ReadAllTextAsync(workspace.PathOf("src/orders_list/order_table/page.tsx"));
        Assert.Contains("export default function OrderTable(props: OrderTableProps)", page);
    }

    [Fact]
    public async Task GenerateAsync_RejectsTargetsOutsideTheTargetFolder()
    {
        using var workspace = new TestWorkspace();
        var entry = new ScaffoldEntry { Name = "bad", Includes = new List<string> { "features/Request.cs.liquid->../../escape.cs" } };
        var variables = new Dictionary<string, object?> { ["rootNamespace"] = "A", ["feature"] = "B", ["useCase"] = "C" };
        Directory.CreateDirectory(workspace.PathOf("out/inner"));

        await Assert.ThrowsAsync<ArgumentException>(() => _service.GenerateAsync(
            workspace.TemplatesPath, "dotnet-api", entry, workspace.PathOf("out/inner"), variables, CancellationToken.None));
    }

    [Fact]
    public void ListTemplates_ReturnsFoldersWithAScaffoldFile()
    {
        using var workspace = new TestWorkspace();

        Assert.Equal(new[] { "dotnet-api", "react-web" }, _service.ListTemplates(workspace.TemplatesPath));
    }
}
