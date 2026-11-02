using System.Text.Json;

namespace Aicode.Mcp.Core.Projects;

public static class ProjectFinder
{
    /// <summary>
    /// Walks up from the path to the nearest project.json that names a sourceTemplate.
    /// Falls back to the workspace .toolkit/settings.yaml sourceTemplate (single-project repos).
    /// </summary>
    public static ProjectInfo? Find(string path)
    {
        var root = Workspace.FindRoot(path);
        for (var dir = new DirectoryInfo(Workspace.StartDirectory(path)); dir is not null; dir = dir.Parent)
        {
            var projectJson = Path.Combine(dir.FullName, "project.json");
            if (File.Exists(projectJson) && TryRead(projectJson, dir.FullName) is { } project)
            {
                return project;
            }

            if (string.Equals(dir.FullName, root, StringComparison.Ordinal))
            {
                break;
            }
        }

        var settings = Workspace.LoadSettings(root);
        return settings.SourceTemplate is null
            ? null
            : new ProjectInfo(Path.GetFileName(root), root, settings.SourceTemplate);
    }

    private static ProjectInfo? TryRead(string projectJson, string dir)
    {
        using var doc = JsonDocument.Parse(File.ReadAllText(projectJson));
        if (doc.RootElement.ValueKind != JsonValueKind.Object ||
            !doc.RootElement.TryGetProperty("sourceTemplate", out var template) ||
            template.ValueKind != JsonValueKind.String)
        {
            return null;
        }

        var name = doc.RootElement.TryGetProperty("name", out var n) && n.ValueKind == JsonValueKind.String
            ? n.GetString()!
            : Path.GetFileName(dir);
        return new ProjectInfo(name, dir, template.GetString());
    }
}
