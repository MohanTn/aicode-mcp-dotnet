using System.Text.Json;

namespace Aicode.Mcp.Core.Hooks;

/// <summary>The parts of Claude Code's hook JSON that these hooks use.</summary>
public record HookInput(string EventName, string? ToolName, string? FilePath, string Cwd, string SessionId)
{
    public static HookInput Parse(string json)
    {
        using var doc = JsonDocument.Parse(json);
        var root = doc.RootElement;
        string? filePath = null;
        if (root.TryGetProperty("tool_input", out var toolInput) &&
            toolInput.ValueKind == JsonValueKind.Object &&
            toolInput.TryGetProperty("file_path", out var fp) &&
            fp.ValueKind == JsonValueKind.String)
        {
            filePath = fp.GetString();
        }

        return new HookInput(
            Text(root, "hook_event_name") ?? "",
            Text(root, "tool_name"),
            filePath,
            Text(root, "cwd") ?? Directory.GetCurrentDirectory(),
            Text(root, "session_id") ?? "default");
    }

    private static string? Text(JsonElement e, string name) =>
        e.TryGetProperty(name, out var v) && v.ValueKind == JsonValueKind.String ? v.GetString() : null;
}
