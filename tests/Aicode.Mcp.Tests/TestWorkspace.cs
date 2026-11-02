namespace Aicode.Mcp.Tests;

/// <summary>A throwaway workspace with this repo's real templates copied in, so tests also check the shipped templates.</summary>
public sealed class TestWorkspace : IDisposable
{
    public TestWorkspace(string? sourceTemplate = "dotnet-api", string? extraSettings = null)
    {
        Root = System.IO.Path.Combine(System.IO.Path.GetTempPath(), "aicode-tests-" + Guid.NewGuid().ToString("N"));
        Directory.CreateDirectory(Root);
        CopyDirectory(FindRepoTemplates(), TemplatesPath);

        var settings = sourceTemplate is null ? "" : $"sourceTemplate: {sourceTemplate}\n";
        Write(".toolkit/settings.yaml", settings + extraSettings);
    }

    public string Root { get; }

    public string TemplatesPath => System.IO.Path.Combine(Root, "templates");

    public string LogDirectory => System.IO.Path.Combine(Root, ".logs");

    public string PathOf(string relative) => System.IO.Path.Combine(Root, relative);

    public string Write(string relative, string content)
    {
        var path = PathOf(relative);
        Directory.CreateDirectory(System.IO.Path.GetDirectoryName(path)!);
        File.WriteAllText(path, content);
        return path;
    }

    public void Dispose() => Directory.Delete(Root, recursive: true);

    private static string FindRepoTemplates()
    {
        for (var dir = new DirectoryInfo(AppContext.BaseDirectory); dir is not null; dir = dir.Parent)
        {
            var candidate = System.IO.Path.Combine(dir.FullName, "templates");
            if (Directory.Exists(System.IO.Path.Combine(candidate, "dotnet-api")))
            {
                return candidate;
            }
        }

        throw new DirectoryNotFoundException("Could not find the repo templates folder above the test binaries.");
    }

    private static void CopyDirectory(string source, string target)
    {
        Directory.CreateDirectory(target);
        foreach (var file in Directory.GetFiles(source))
        {
            File.Copy(file, System.IO.Path.Combine(target, System.IO.Path.GetFileName(file)));
        }

        foreach (var directory in Directory.GetDirectories(source))
        {
            CopyDirectory(directory, System.IO.Path.Combine(target, System.IO.Path.GetFileName(directory)));
        }
    }
}
