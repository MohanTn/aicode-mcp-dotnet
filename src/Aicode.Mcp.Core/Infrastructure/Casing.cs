using System.Text.RegularExpressions;

namespace Aicode.Mcp.Core.Infrastructure;

/// <summary>Word-case helpers used as template filters (pascalCase, camelCase, kebabCase, snakeCase).</summary>
public static partial class Casing
{
    [GeneratedRegex("[A-Z]+(?![a-z])|[A-Z]?[a-z]+|[0-9]+")]
    private static partial Regex WordPattern();

    private static string[] Words(string value) =>
        WordPattern().Matches(value).Select(m => m.Value).ToArray();

    public static string Pascal(string value) =>
        string.Concat(Words(value).Select(w => char.ToUpperInvariant(w[0]) + w[1..].ToLowerInvariant()));

    public static string Camel(string value)
    {
        var pascal = Pascal(value);
        return pascal.Length == 0 ? pascal : char.ToLowerInvariant(pascal[0]) + pascal[1..];
    }

    public static string Kebab(string value) => string.Join('-', Words(value).Select(w => w.ToLowerInvariant()));

    public static string Snake(string value) => string.Join('_', Words(value).Select(w => w.ToLowerInvariant()));
}
