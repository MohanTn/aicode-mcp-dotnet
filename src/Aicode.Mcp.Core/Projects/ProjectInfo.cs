namespace Aicode.Mcp.Core.Projects;

/// <summary>A project inside the workspace and the template it was generated from.</summary>
public record ProjectInfo(string Name, string Root, string? SourceTemplate);
