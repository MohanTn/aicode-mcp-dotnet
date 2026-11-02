using YamlDotNet.Serialization;

namespace Aicode.Mcp.Core.Models;

/// <summary>A group of rules that applies to files matching its globs.</summary>
public class RuleSection
{
    [YamlMember(Alias = "pattern")]
    public string Pattern { get; set; } = "";

    [YamlMember(Alias = "globs")]
    public List<string>? Globs { get; set; }

    [YamlMember(Alias = "description")]
    public string Description { get; set; } = "";

    [YamlMember(Alias = "inherits")]
    public List<string>? Inherits { get; set; }

    [YamlMember(Alias = "must_do")]
    public List<RuleItem>? MustDo { get; set; }

    [YamlMember(Alias = "should_do")]
    public List<RuleItem>? ShouldDo { get; set; }

    [YamlMember(Alias = "must_not_do")]
    public List<RuleItem>? MustNotDo { get; set; }
}
