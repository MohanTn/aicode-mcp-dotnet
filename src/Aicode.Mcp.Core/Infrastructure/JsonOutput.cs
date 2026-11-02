using System.Text.Json;
using System.Text.Json.Serialization;

namespace Aicode.Mcp.Core.Infrastructure;

/// <summary>Shared JSON settings for tool and hook output (snake_case, no nulls).</summary>
public static class JsonOutput
{
    public static readonly JsonSerializerOptions Options = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
        DictionaryKeyPolicy = null,
        WriteIndented = true,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    public static string Serialize<T>(T value) => JsonSerializer.Serialize(value, Options);
}
