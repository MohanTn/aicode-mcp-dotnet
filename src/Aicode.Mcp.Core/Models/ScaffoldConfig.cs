using YamlDotNet.Serialization;

namespace Aicode.Mcp.Core.Models;

/// <summary>Root of a scaffold.yaml file.</summary>
public class ScaffoldConfig
{
    [YamlMember(Alias = "boilerplate")]
    public List<ScaffoldEntry> Boilerplate { get; set; } = new();

    [YamlMember(Alias = "features")]
    public List<ScaffoldEntry> Features { get; set; } = new();

    /// <summary>Globs (relative to the project) that may be written without scaffolding.</summary>
    [YamlMember(Alias = "exclude")]
    public List<string> Exclude { get; set; } = new();
}
