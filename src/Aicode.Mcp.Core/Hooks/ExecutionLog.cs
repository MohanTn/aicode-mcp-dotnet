using System.Security.Cryptography;
using System.Text.Json;

namespace Aicode.Mcp.Core.Hooks;

/// <summary>
/// Per-session record of what the hooks already told the agent, kept as a JSON file in the temp folder.
/// It stops the same advice from repeating and skips reviews of files that did not change.
/// </summary>
public class ExecutionLog
{
    private readonly string _path;

    public ExecutionLog(string sessionId, string? baseDirectory = null)
    {
        var directory = baseDirectory ?? Path.Combine(Path.GetTempPath(), "aicode-mcp");
        Directory.CreateDirectory(directory);
        _path = Path.Combine(directory, Sanitize(sessionId) + ".json");
    }

    public IReadOnlyList<ExecutionEntry> Load()
    {
        if (!File.Exists(_path))
        {
            return Array.Empty<ExecutionEntry>();
        }

        try
        {
            return JsonSerializer.Deserialize<List<ExecutionEntry>>(File.ReadAllText(_path)) ?? new List<ExecutionEntry>();
        }
        catch (JsonException)
        {
            return Array.Empty<ExecutionEntry>();
        }
    }

    public void Append(string filePath, string operation, string decision)
    {
        var entries = Load().ToList();
        entries.Add(new ExecutionEntry(filePath, operation, decision, Checksum(filePath), DateTimeOffset.UtcNow));
        File.WriteAllText(_path, JsonSerializer.Serialize(entries));
    }

    public bool HasExecuted(string filePath, string decision) =>
        Load().Any(e => e.FilePath == filePath && e.Decision == decision);

    public bool WasRecentlyReviewed(string filePath, TimeSpan window)
    {
        var last = LastReview(filePath);
        return last is not null && DateTimeOffset.UtcNow - last.Timestamp < window;
    }

    /// <summary>True when the file was never reviewed, or its content differs from the last reviewed version.</summary>
    public bool HasChangedSinceLastReview(string filePath)
    {
        var last = LastReview(filePath);
        return last is null || last.Checksum != Checksum(filePath);
    }

    public static string? Checksum(string filePath)
    {
        if (!File.Exists(filePath))
        {
            return null;
        }

        using var stream = File.OpenRead(filePath);
        return Convert.ToHexString(SHA256.HashData(stream));
    }

    private ExecutionEntry? LastReview(string filePath) =>
        Load().LastOrDefault(e => e.FilePath == filePath && e.Operation == "review");

    private static string Sanitize(string sessionId)
    {
        var cleaned = new string(sessionId.Where(c => char.IsLetterOrDigit(c) || c is '-' or '_').ToArray());
        return cleaned.Length == 0 ? "default" : cleaned;
    }
}
