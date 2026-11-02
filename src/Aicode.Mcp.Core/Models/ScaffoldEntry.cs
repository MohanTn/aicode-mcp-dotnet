using YamlDotNet.Serialization;

namespace Aicode.Mcp.Core.Models;

/// <summary>A boilerplate or feature entry in scaffold.yaml.</summary>
public class ScaffoldEntry
{
    [YamlMember(Alias = "name")]
    public string Name { get; set; } = "";

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = "";

    [YamlMember(Alias = "variables_schema")]
    public Dictionary<string, object?>? VariablesSchema { get; set; }

    /// <summary>Entries look like "src->dest", or "src" and an optional "?key=value" condition.</summary>
    [YamlMember(Alias = "includes")]
    public List<string> Includes { get; set; } = new();
}
