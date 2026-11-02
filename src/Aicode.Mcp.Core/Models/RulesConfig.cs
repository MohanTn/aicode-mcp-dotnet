using YamlDotNet.Serialization;

namespace Aicode.Mcp.Core.Models;

/// <summary>Root of a RULES.yaml file.</summary>
public class RulesConfig
{
    [YamlMember(Alias = "version")]
    public string Version { get; set; } = "1.0";

    [YamlMember(Alias = "template")]
    public string Template { get; set; } = "";

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = "";

    [YamlMember(Alias = "rules")]
    public List<RuleSection> Rules { get; set; } = new();
}
