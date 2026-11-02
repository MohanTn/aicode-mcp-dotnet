namespace Aicode.Mcp.Core.Reviewer;

/// <summary>GitLab connection settings. The caller decides where the values come from.</summary>
public record GitLabOptions(string BaseUrl, string? Token);
