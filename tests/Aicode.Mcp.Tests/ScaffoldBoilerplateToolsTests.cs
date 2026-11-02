using System.Text.Json;
using Aicode.Mcp.Core.Scaffold;
using Aicode.Mcp.Servers;
using ModelContextProtocol;

namespace Aicode.Mcp.Tests;

public class ScaffoldBoilerplateToolsTests
{
    private static ScaffoldTools Tools() => new(new ScaffoldService());

    [Fact]
    public void ListBoilerplates_ReturnsEveryTemplatesBoilerplates()
    {
        using var workspace = new TestWorkspace();

        using var doc = JsonDocument.Parse(Tools().ListBoilerplates(workspace.Root));

        var names = doc.RootElement.GetProperty("boilerplates").EnumerateArray()
            .Select(b => (b.GetProperty("template").GetString()!, b.GetProperty("name").GetString()!))
            .ToList();
        Assert.Equal(new[] { ("dotnet-api", "dotnet-api"), ("react-web", "react-web") }, names);
    }

    [Fact]
    public void ListBoilerplates_IncludesTheVariablesSchema()
    {
        using var workspace = new TestWorkspace();

        using var doc = JsonDocument.Parse(Tools().ListBoilerplates(workspace.Root));

        var schema = doc.RootElement.GetProperty("boilerplates")[0].GetProperty("variables_schema");
        Assert.Equal("projectName", schema.GetProperty("required")[0].GetString());
    }

    [Fact]
    public async Task UseBoilerplate_CreatesTheDotnetApiProject()
    {
        using var workspace = new TestWorkspace();
        var target = workspace.PathOf("out/api");

        using var doc = JsonDocument.Parse(await Tools().UseBoilerplate(
            "dotnet-api", target, """{ "projectName": "Orders.Api" }""", workspacePath: workspace.Root));

        Assert.Equal("dotnet-api", doc.RootElement.GetProperty("template").GetString());
        Assert.True(File.Exists(Path.Combine(target, "Orders.Api.csproj")));
        Assert.True(File.Exists(Path.Combine(target, "Program.cs")));
        Assert.Contains("<RootNamespace>Orders.Api</RootNamespace>", await File.ReadAllTextAsync(Path.Combine(target, "Orders.Api.csproj")));
    }

    [Fact]
    public async Task UseBoilerplate_CreatesTheReactWebProject()
    {
        using var workspace = new TestWorkspace(sourceTemplate: "react-web");
        var target = workspace.PathOf("out/web");

        await Tools().UseBoilerplate("react-web", target, """{ "projectName": "orders-web" }""", workspacePath: workspace.Root);

        Assert.Contains("\"name\": \"orders-web\"", await File.ReadAllTextAsync(Path.Combine(target, "package.json")));
        Assert.True(File.Exists(Path.Combine(target, "src", "I18n", "index.ts")));
        Assert.True(File.Exists(Path.Combine(target, "src", "main.tsx")));
    }

    [Fact]
    public async Task UseBoilerplate_RejectsAnUnknownBoilerplate()
    {
        using var workspace = new TestWorkspace();

        var error = await Assert.ThrowsAsync<McpException>(() => Tools().UseBoilerplate(
            "nope", workspace.PathOf("out"), workspacePath: workspace.Root));

        Assert.Contains("list-boilerplates", error.Message);
    }

    [Fact]
    public async Task UseBoilerplate_ReportsMissingVariables()
    {
        using var workspace = new TestWorkspace();

        var error = await Assert.ThrowsAsync<McpException>(() => Tools().UseBoilerplate(
            "dotnet-api", workspace.PathOf("out"), "{}", workspacePath: workspace.Root));

        Assert.Contains("projectName", error.Message);
    }

    [Fact]
    public async Task UseBoilerplate_ReportsInvalidVariablesJson()
    {
        using var workspace = new TestWorkspace();

        await Assert.ThrowsAsync<McpException>(() => Tools().UseBoilerplate(
            "dotnet-api", workspace.PathOf("out"), "not json", workspacePath: workspace.Root));
    }

    [Fact]
    public async Task UseBoilerplate_AsksForTemplateNameWhenTwoTemplatesShareABoilerplate()
    {
        using var workspace = new TestWorkspace();
        workspace.Write("templates/other/scaffold.yaml", """
            boilerplate:
              - name: dotnet-api
                description: Another take
                includes: []
            """);

        var error = await Assert.ThrowsAsync<McpException>(() => Tools().UseBoilerplate(
            "dotnet-api", workspace.PathOf("out"), """{ "projectName": "A" }""", workspacePath: workspace.Root));
        Assert.Contains("templateName", error.Message);

        using var doc = JsonDocument.Parse(await Tools().UseBoilerplate(
            "dotnet-api", workspace.PathOf("out2"), """{ "projectName": "A" }""", templateName: "dotnet-api", workspacePath: workspace.Root));
        Assert.Equal("dotnet-api", doc.RootElement.GetProperty("template").GetString());
    }

    [Fact]
    public async Task UseBoilerplate_NeverOverwritesExistingFiles()
    {
        using var workspace = new TestWorkspace();
        var target = workspace.PathOf("out/api");
        workspace.Write("out/api/Program.cs", "// mine");

        using var doc = JsonDocument.Parse(await Tools().UseBoilerplate(
            "dotnet-api", target, """{ "projectName": "Orders.Api" }""", workspacePath: workspace.Root));

        Assert.Equal("// mine", await File.ReadAllTextAsync(Path.Combine(target, "Program.cs")));
        Assert.Contains(doc.RootElement.GetProperty("skipped_files").EnumerateArray(), s => s.GetString()!.StartsWith("Program.cs"));
    }

    [Fact]
    public void ListScaffoldingMethods_ThrowsWhenTheProjectHasNoTemplate()
    {
        using var workspace = new TestWorkspace(sourceTemplate: null);

        Assert.Throws<McpException>(() => Tools().ListScaffoldingMethods(workspace.Root));
    }
}
