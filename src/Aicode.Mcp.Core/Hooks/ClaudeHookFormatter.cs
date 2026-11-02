using System.Text.Json;

namespace Aicode.Mcp.Core.Hooks;

/// <summary>
/// Turns a HookResponse into Claude Code's hook output JSON.
/// PreToolUse: deny blocks the tool call. PostToolUse: deny becomes "block". allow with a message adds context.
/// </summary>
public static class ClaudeHookFormatter
{
    public static string Format(string eventName, HookResponse response)
    {
        if (response.Decision == HookResponse.Skip)
        {
            return "{}";
        }

        var specific = new Dictionary<string, object?> { ["hookEventName"] = eventName };
        var output = new Dictionary<string, object?> { ["hookSpecificOutput"] = specific };

        if (eventName == "PostToolUse")
        {
            if (response.Decision == HookResponse.Deny)
            {
                output["decision"] = "block";
                output["reason"] = response.Message;
            }
            else if (response.Message.Length > 0)
            {
                specific["additionalContext"] = response.Message;
            }
        }
        else
        {
            specific["permissionDecision"] = response.Decision;
            specific["permissionDecisionReason"] = response.Message;
            if (response.Decision == HookResponse.Allow && response.Message.Length > 0)
            {
                specific["additionalContext"] = response.Message;
            }
        }

        return JsonSerializer.Serialize(output, new JsonSerializerOptions { WriteIndented = true });
    }
}
