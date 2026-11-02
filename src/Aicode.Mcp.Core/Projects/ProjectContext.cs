using Aicode.Mcp.Core.Models;

namespace Aicode.Mcp.Core.Projects;

/// <summary>Everything the servers and hooks need to know about where a file lives.</summary>
public record ProjectContext(
    string AbsolutePath,
    string WorkspaceRoot,
    ToolkitSettings Settings,
    string TemplatesPath,
    ProjectInfo? Project,
    string RelativePath)
{
    /// <summary>Resolves a (possibly relative) file or directory path against <paramref name="baseDirectory"/>.</summary>
    public static ProjectContext Resolve(string path, string baseDirectory)
    {
        var absolute = Path.GetFullPath(path, baseDirectory);
        var root = Workspace.FindRoot(absolute);
        var settings = Workspace.LoadSettings(root);
        var project = ProjectFinder.Find(absolute);
        var relative = Workspace.RelativePath(project?.Root ?? root, absolute);
        return new ProjectContext(absolute, root, settings, Workspace.TemplatesPath(root, settings), project, relative);
    }
}
