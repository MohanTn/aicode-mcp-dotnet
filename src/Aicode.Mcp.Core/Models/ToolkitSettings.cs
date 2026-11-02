using YamlDotNet.Serialization;

namespace Aicode.Mcp.Core.Models;

/// <summary>.toolkit/settings.yaml at the workspace root.</summary>
public class ToolkitSettings
{
    [YamlMember(Alias = "sourceTemplate")]
    public string? SourceTemplate { get; set; }

    [YamlMember(Alias = "templatesPath")]
    public string? TemplatesPath { get; set; }

    [YamlMember(Alias = "scaffold-mcp")]
    public ScaffoldMcpSettings? ScaffoldMcp { get; set; }

    public class ScaffoldMcpSettings
    {
        [YamlMember(Alias = "hook")]
        public HookSettings? Hook { get; set; }
    }

    public class HookSettings
    {
        [YamlMember(Alias = "excludeGlobs")]
        public List<string>? ExcludeGlobs { get; set; }
    }
}
