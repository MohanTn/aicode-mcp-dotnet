using YamlDotNet.Serialization;

namespace Aicode.Mcp.Core.Models;

/// <summary>One design pattern entry in architect.yaml.</summary>
public class ArchitectFeature
{
    [YamlMember(Alias = "name")]
    public string? Name { get; set; }

    [YamlMember(Alias = "architecture")]
    public string? Architecture { get; set; }

    [YamlMember(Alias = "design_pattern")]
    public string DesignPattern { get; set; } = "";

    [YamlMember(Alias = "includes")]
    public List<string> Includes { get; set; } = new();

    [YamlMember(Alias = "description")]
    public string? Description { get; set; }
}
