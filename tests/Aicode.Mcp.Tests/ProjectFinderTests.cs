using Aicode.Mcp.Core.Projects;

namespace Aicode.Mcp.Tests;

public class ProjectFinderTests
{
    [Fact]
    public void Find_UsesNearestProjectJsonWithSourceTemplate()
    {
        using var workspace = new TestWorkspace(sourceTemplate: null);
        workspace.Write("apps/api/project.json", """{ "name": "api", "sourceTemplate": "dotnet-api" }""");

        var project = ProjectFinder.Find(workspace.PathOf("apps/api/Features/Orders/OrdersController.cs"));

        Assert.NotNull(project);
        Assert.Equal("api", project.Name);
        Assert.Equal("dotnet-api", project.SourceTemplate);
        Assert.Equal(workspace.PathOf("apps/api"), project.Root);
    }

    [Fact]
    public void Find_FallsBackToWorkspaceSettings()
    {
        using var workspace = new TestWorkspace(sourceTemplate: "react-web");

        var project = ProjectFinder.Find(workspace.PathOf("src/app/page.tsx"));

        Assert.NotNull(project);
        Assert.Equal("react-web", project.SourceTemplate);
        Assert.Equal(workspace.Root, project.Root);
    }

    [Fact]
    public void Find_ReturnsNullWhenNothingNamesATemplate()
    {
        using var workspace = new TestWorkspace(sourceTemplate: null);
        workspace.Write("apps/web/project.json", """{ "name": "web" }""");

        Assert.Null(ProjectFinder.Find(workspace.PathOf("apps/web/src/a.ts")));
    }
}
