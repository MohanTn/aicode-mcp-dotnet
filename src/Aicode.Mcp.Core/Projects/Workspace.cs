using Aicode.Mcp.Core.Infrastructure;
using Aicode.Mcp.Core.Models;

namespace Aicode.Mcp.Core.Projects;

/// <summary>Finds the workspace root, its settings, and its templates folder.</summary>
public static class Workspace
{
    public const string DefaultTemplatesFolder = "templates";

    /// <summary>The directory to start walking up from: the path itself if it is a directory, else its parent.</summary>
    public static string StartDirectory(string path)
    {
        var full = Path.GetFullPath(path);
        return Directory.Exists(full) ? full : Path.GetDirectoryName(full) ?? full;
    }

    /// <summary>Nearest ancestor holding a .toolkit folder or a .git entry. Falls back to the start directory.</summary>
    public static string FindRoot(string path)
    {
        var start = StartDirectory(path);
        for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
        {
            if (Directory.Exists(Path.Combine(dir.FullName, ".toolkit")) ||
                Directory.Exists(Path.Combine(dir.FullName, ".git")) ||
                File.Exists(Path.Combine(dir.FullName, ".git")))
            {
                return dir.FullName;
            }
        }

        return start;
    }

    public static ToolkitSettings LoadSettings(string root) =>
        YamlLoader.Load<ToolkitSettings>(Path.Combine(root, ".toolkit", "settings.yaml")) ?? new ToolkitSettings();

    public static string TemplatesPath(string root, ToolkitSettings settings) =>
        Path.Combine(root, settings.TemplatesPath ?? DefaultTemplatesFolder);

    /// <summary>Forward-slash path of <paramref name="path"/> relative to <paramref name="root"/>.</summary>
    public static string RelativePath(string root, string path) =>
        Path.GetRelativePath(root, path).Replace('\\', '/');
}
