using System.Globalization;
using Aicode.Mcp.Core.Infrastructure;
using Aicode.Mcp.Core.Models;
using Fluid;
using Fluid.Values;

namespace Aicode.Mcp.Core.Scaffold;

/// <summary>Reads scaffold.yaml and renders template files (Liquid syntax) into a target folder.</summary>
public class ScaffoldService
{
    private static readonly FluidParser Parser = new();
    private static readonly TemplateOptions Options = BuildOptions();

    public ScaffoldConfig? LoadConfig(string templatesPath, string template) =>
        YamlLoader.Load<ScaffoldConfig>(Path.Combine(templatesPath, template, "scaffold.yaml"));

    /// <summary>Names of template folders that contain a scaffold.yaml.</summary>
    public IReadOnlyList<string> ListTemplates(string templatesPath)
    {
        if (!Directory.Exists(templatesPath))
        {
            return Array.Empty<string>();
        }

        return Directory.GetDirectories(templatesPath)
            .Where(d => File.Exists(Path.Combine(d, "scaffold.yaml")))
            .Select(d => Path.GetFileName(d))
            .OrderBy(n => n, StringComparer.Ordinal)
            .ToList();
    }

    public static ParsedInclude ParseInclude(string include)
    {
        var parts = include.Split('?', 2);
        var conditions = new Dictionary<string, string>();
        if (parts.Length == 2)
        {
            foreach (var pair in parts[1].Split('&', StringSplitOptions.RemoveEmptyEntries))
            {
                var kv = pair.Split('=', 2);
                if (kv.Length == 2 && kv[0].Trim().Length > 0)
                {
                    conditions[kv[0].Trim()] = kv[1].Trim();
                }
            }
        }

        var arrow = parts[0].Split("->", 2);
        return arrow.Length == 2
            ? new ParsedInclude(arrow[0].Trim(), arrow[1].Trim(), true, conditions)
            : new ParsedInclude(parts[0].Trim(), parts[0].Trim(), false, conditions);
    }

    /// <summary>Names from the entry's variables_schema.required that the caller did not supply.</summary>
    public static IReadOnlyList<string> MissingRequired(ScaffoldEntry entry, IDictionary<string, object?> variables)
    {
        if (entry.VariablesSchema is null ||
            !entry.VariablesSchema.TryGetValue("required", out var raw) ||
            JsonValues.Normalize(raw) is not List<object?> required)
        {
            return Array.Empty<string>();
        }

        return required
            .Select(r => r?.ToString() ?? "")
            .Where(name => name.Length > 0 && !variables.ContainsKey(name))
            .ToList();
    }

    public async Task<ScaffoldResult> GenerateAsync(
        string templatesPath,
        string template,
        ScaffoldEntry entry,
        string targetFolder,
        IDictionary<string, object?> variables,
        CancellationToken cancellationToken)
    {
        var missing = MissingRequired(entry, variables);
        if (missing.Count > 0)
        {
            throw new ArgumentException($"Missing required variables: {string.Join(", ", missing)}");
        }

        var templateDir = Path.GetFullPath(Path.Combine(templatesPath, template));
        var targetRoot = Path.GetFullPath(targetFolder);
        var created = new List<string>();
        var skipped = new List<string>();

        foreach (var include in entry.Includes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var parsed = ParseInclude(include);
            if (!ConditionsMet(parsed.Conditions, variables))
            {
                skipped.Add($"{parsed.Source} (condition not met)");
                continue;
            }

            var sourceFile = ResolveInside(templateDir, parsed.Source);
            if (!File.Exists(sourceFile))
            {
                throw new FileNotFoundException($"Template file not found: {parsed.Source}", sourceFile);
            }

            var renderedTarget = await RenderAsync(parsed.Target, variables);
            if (!parsed.HasArrow && renderedTarget.EndsWith(".liquid", StringComparison.Ordinal))
            {
                renderedTarget = renderedTarget[..^".liquid".Length];
            }

            var targetFile = ResolveInside(targetRoot, renderedTarget);
            if (File.Exists(targetFile))
            {
                skipped.Add($"{renderedTarget} (already exists)");
                continue;
            }

            Directory.CreateDirectory(Path.GetDirectoryName(targetFile)!);
            var bytes = await File.ReadAllBytesAsync(sourceFile, cancellationToken);
            if (Array.IndexOf(bytes, (byte)0) >= 0)
            {
                await File.WriteAllBytesAsync(targetFile, bytes, cancellationToken);
            }
            else
            {
                var text = await RenderAsync(System.Text.Encoding.UTF8.GetString(bytes), variables);
                await File.WriteAllTextAsync(targetFile, text, cancellationToken);
            }

            created.Add(renderedTarget);
        }

        return new ScaffoldResult(created, skipped);
    }

    public static async Task<string> RenderAsync(string source, IDictionary<string, object?> variables)
    {
        if (!Parser.TryParse(source, out var template, out var error))
        {
            throw new InvalidOperationException($"Template error: {error}");
        }

        var context = new TemplateContext(Options);
        foreach (var (key, value) in variables)
        {
            context.SetValue(key, value!);
        }

        return await template.RenderAsync(context);
    }

    private static bool ConditionsMet(IReadOnlyDictionary<string, string> conditions, IDictionary<string, object?> variables) =>
        conditions.All(c =>
            variables.TryGetValue(c.Key, out var value) &&
            string.Equals(
                Convert.ToString(value, CultureInfo.InvariantCulture),
                c.Value,
                StringComparison.OrdinalIgnoreCase));

    /// <summary>Joins a relative path onto a root and rejects anything that escapes the root.</summary>
    private static string ResolveInside(string root, string relative)
    {
        var full = Path.GetFullPath(Path.Combine(root, relative));
        var rootWithSeparator = root.EndsWith(Path.DirectorySeparatorChar) ? root : root + Path.DirectorySeparatorChar;
        if (!full.StartsWith(rootWithSeparator, StringComparison.Ordinal))
        {
            throw new ArgumentException($"Path escapes its folder: {relative}");
        }

        return full;
    }

    private static TemplateOptions BuildOptions()
    {
        var options = new TemplateOptions();
        AddCaseFilter(options, "pascalCase", Casing.Pascal);
        AddCaseFilter(options, "camelCase", Casing.Camel);
        AddCaseFilter(options, "kebabCase", Casing.Kebab);
        AddCaseFilter(options, "snakeCase", Casing.Snake);
        return options;
    }

    private static void AddCaseFilter(TemplateOptions options, string name, Func<string, string> convert) =>
        options.Filters.AddFilter(name, (input, _, _) =>
            new ValueTask<FluidValue>(new StringValue(convert(input.ToStringValue()))));
}
