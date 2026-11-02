using YamlDotNet.Serialization;

namespace Aicode.Mcp.Core.Models;

/// <summary>One rule in RULES.yaml, with optional examples.</summary>
public class RuleItem
{
    [YamlMember(Alias = "rule")]
    public string Rule { get; set; } = "";

    [YamlMember(Alias = "example")]
    public string? Example { get; set; }

    [YamlMember(Alias = "codeExample")]
    public string? CodeExample { get; set; }
}
