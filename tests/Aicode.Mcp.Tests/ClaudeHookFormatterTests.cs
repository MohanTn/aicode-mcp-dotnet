using System.Text.Json;
using Aicode.Mcp.Core.Hooks;

namespace Aicode.Mcp.Tests;

public class ClaudeHookFormatterTests
{
    [Fact]
    public void Format_SkipIsAnEmptyObject()
    {
        Assert.Equal("{}", ClaudeHookFormatter.Format("PreToolUse", HookResponse.Skipped("nothing")));
    }

    [Fact]
    public void Format_PreToolUseDenyCarriesPermissionDecisionAndReason()
    {
        using var doc = JsonDocument.Parse(ClaudeHookFormatter.Format("PreToolUse", HookResponse.Denied("use scaffold")));

        var output = doc.RootElement.GetProperty("hookSpecificOutput");
        Assert.Equal("deny", output.GetProperty("permissionDecision").GetString());
        Assert.Equal("use scaffold", output.GetProperty("permissionDecisionReason").GetString());
        Assert.False(output.TryGetProperty("additionalContext", out _));
    }

    [Fact]
    public void Format_PreToolUseAllowAddsContext()
    {
        using var doc = JsonDocument.Parse(ClaudeHookFormatter.Format("PreToolUse", HookResponse.Allowed("fine")));

        Assert.Equal("fine", doc.RootElement.GetProperty("hookSpecificOutput").GetProperty("additionalContext").GetString());
    }

    [Fact]
    public void Format_PostToolUseDenyBecomesBlock()
    {
        using var doc = JsonDocument.Parse(ClaudeHookFormatter.Format("PostToolUse", HookResponse.Denied("fix it")));

        Assert.Equal("block", doc.RootElement.GetProperty("decision").GetString());
        Assert.Equal("fix it", doc.RootElement.GetProperty("reason").GetString());
    }

    [Fact]
    public void Format_PostToolUseAllowAddsContext()
    {
        using var doc = JsonDocument.Parse(ClaudeHookFormatter.Format("PostToolUse", HookResponse.Allowed("rules")));

        Assert.Equal("rules", doc.RootElement.GetProperty("hookSpecificOutput").GetProperty("additionalContext").GetString());
        Assert.False(doc.RootElement.TryGetProperty("decision", out _));
    }

    [Fact]
    public void HookInput_ParsesClaudeCodeJson()
    {
        var input = HookInput.Parse("""
            { "hook_event_name": "PreToolUse", "tool_name": "Write", "cwd": "/w", "session_id": "abc",
              "tool_input": { "file_path": "/w/a.cs", "content": "x" } }
            """);

        Assert.Equal(new HookInput("PreToolUse", "Write", "/w/a.cs", "/w", "abc"), input);
    }
}
