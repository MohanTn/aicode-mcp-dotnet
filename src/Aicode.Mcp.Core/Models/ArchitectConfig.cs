using YamlDotNet.Serialization;

namespace Aicode.Mcp.Core.Models;

/// <summary>Root of an architect.yaml file.</summary>
public class ArchitectConfig
{
    [YamlMember(Alias = "features")]
    public List<ArchitectFeature> Features { get; set; } = new();
}
