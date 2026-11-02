using System.Text.Json;

namespace Aicode.Mcp.Core.Infrastructure;

public static class JsonValues
{
    /// <summary>Parses a JSON object string into plain .NET values. Null or blank input gives an empty map.</summary>
    public static Dictionary<string, object?> ParseObject(string? json)
    {
        var result = new Dictionary<string, object?>();
        if (string.IsNullOrWhiteSpace(json))
        {
            return result;
        }

        using var doc = JsonDocument.Parse(json);
        if (doc.RootElement.ValueKind != JsonValueKind.Object)
        {
            throw new ArgumentException("variables must be a JSON object, e.g. {\"feature\":\"Orders\"}");
        }

        foreach (var property in doc.RootElement.EnumerateObject())
        {
            result[property.Name] = ToObject(property.Value);
        }

        return result;
    }

    public static object? ToObject(JsonElement element) => element.ValueKind switch
    {
        JsonValueKind.String => element.GetString(),
        JsonValueKind.Number => element.TryGetInt64(out var l) ? l : element.GetDouble(),
        JsonValueKind.True => true,
        JsonValueKind.False => false,
        JsonValueKind.Array => element.EnumerateArray().Select(ToObject).ToList(),
        JsonValueKind.Object => element.EnumerateObject().ToDictionary(p => p.Name, p => ToObject(p.Value)),
        _ => null,
    };

    /// <summary>Turns YamlDotNet's object-keyed maps into string-keyed maps so System.Text.Json can write them.</summary>
    public static object? Normalize(object? value) => value switch
    {
        IDictionary<object, object> d => d.ToDictionary(kv => kv.Key.ToString() ?? "", kv => Normalize(kv.Value)),
        IDictionary<string, object?> s => s.ToDictionary(kv => kv.Key, kv => Normalize(kv.Value)),
        IList<object> l => l.Select(Normalize).ToList(),
        _ => value,
    };
}
