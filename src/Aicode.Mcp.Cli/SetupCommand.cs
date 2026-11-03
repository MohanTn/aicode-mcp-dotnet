using System.ComponentModel;
using System.Diagnostics;
using System.Text.Json;
using System.Text.Json.Nodes;

namespace Aicode.Mcp.Cli;

/// <summary>
/// Registers scaffold-mcp and architect-mcp user-wide in Claude Code, Copilot CLI and Pi, adds the Claude Code hooks,
/// and installs the bundled skills. Only harnesses whose config folder exists are touched. Re-running refreshes
/// everything without duplicating it, and <c>remove</c> takes out only what setup added.
/// </summary>
public static class SetupCommand
{
    private const string HookMarker = "aicode-mcp hook";
    private const string RepoTemplateSkill = "repo-template";
    private static readonly string[] Servers = ["scaffold", "architect"];
    private static readonly JsonSerializerOptions Indented = new() { WriteIndented = true };

    public static int Run(
        string home,
        TextWriter output,
        bool remove = false,
        string? bundled = null,
        Func<string[], bool>? runClaude = null)
    {
        bundled ??= AppContext.BaseDirectory;
        runClaude ??= RunClaude;
        var verb = remove ? "removed from" : "set up for";

        var claude = Path.Combine(home, ".claude");
        if (Directory.Exists(claude))
        {
            foreach (var server in Servers)
            {
                runClaude(["mcp", "remove", "--scope", "user", $"{server}-mcp"]);
                if (!remove && !runClaude(["mcp", "add", "--scope", "user", $"{server}-mcp", "--", "aicode-mcp", "mcp-serve", "--server", server]))
                {
                    output.WriteLine($"claude CLI failed or not found, run: claude mcp add --scope user {server}-mcp -- aicode-mcp mcp-serve --server {server}");
                }
            }
            EditHooks(Path.Combine(claude, "settings.json"), remove);
            EditSkills(Path.Combine(claude, "skills"), bundled, remove, output);
            output.WriteLine($"aicode-mcp: {verb} claude");
        }
        else
        {
            output.WriteLine($"aicode-mcp: skipped claude, {claude} not found");
        }

        var copilot = Path.Combine(home, ".copilot");
        if (Directory.Exists(copilot))
        {
            EditServers(Path.Combine(copilot, "mcp-config.json"), remove, server => new JsonObject
            {
                ["type"] = "local",
                ["command"] = "aicode-mcp",
                ["args"] = new JsonArray("mcp-serve", "--server", server),
                ["tools"] = new JsonArray("*"),
            });
            EditSkills(Path.Combine(copilot, "skills"), bundled, remove, output);
            output.WriteLine($"aicode-mcp: {verb} copilot");
        }
        else
        {
            output.WriteLine($"aicode-mcp: skipped copilot, {copilot} not found");
        }

        var pi = Path.Combine(home, ".pi", "agent");
        if (Directory.Exists(pi))
        {
            EditServers(Path.Combine(pi, "mcp.json"), remove, server => new JsonObject
            {
                ["command"] = "aicode-mcp",
                ["args"] = new JsonArray("mcp-serve", "--server", server),
                ["description"] = $"aicode-mcp {server} tools for .NET and React projects",
            });
            EditSkills(Path.Combine(pi, "skills"), bundled, remove, output);
            output.WriteLine($"aicode-mcp: {verb} pi");
        }
        else
        {
            output.WriteLine($"aicode-mcp: skipped pi, {pi} not found");
        }

        return 0;
    }

    /// <summary>Upserts or deletes the aicode entries under mcpServers, leaving every other server untouched.</summary>
    private static void EditServers(string file, bool remove, Func<string, JsonObject> entry)
    {
        if (remove && !File.Exists(file))
        {
            return;
        }

        var root = Load(file);
        if (root["mcpServers"] is not JsonObject servers)
        {
            servers = new JsonObject();
            root["mcpServers"] = servers;
        }
        foreach (var server in Servers)
        {
            servers.Remove($"{server}-mcp");
            if (!remove)
            {
                servers[$"{server}-mcp"] = entry(server);
            }
        }
        Save(file, root);
    }

    /// <summary>Replaces the aicode-mcp hook entries in Claude Code settings, recognised by their command.</summary>
    private static void EditHooks(string file, bool remove)
    {
        if (remove && !File.Exists(file))
        {
            return;
        }

        var root = Load(file);
        if (root["hooks"] is not JsonObject hooks)
        {
            hooks = new JsonObject();
            root["hooks"] = hooks;
        }
        var wanted = new Dictionary<string, (string Matcher, string Type)>
        {
            ["PreToolUse"] = ("Write", "scaffold"),
            ["PostToolUse"] = ("Edit|Write|MultiEdit", "architect"),
        };
        foreach (var (eventName, hook) in wanted)
        {
            var kept = new JsonArray();
            if (hooks[eventName] is JsonArray existing)
            {
                foreach (var item in existing.Where(item => !IsOurs(item)))
                {
                    kept.Add(item?.DeepClone());
                }
            }
            if (!remove)
            {
                kept.Add(new JsonObject
                {
                    ["matcher"] = hook.Matcher,
                    ["hooks"] = new JsonArray(new JsonObject { ["type"] = "command", ["command"] = $"{HookMarker} --type {hook.Type}" }),
                });
            }
            hooks[eventName] = kept;
        }
        Save(file, root);
    }

    private static bool IsOurs(JsonNode? entry) =>
        entry?["hooks"] is JsonArray list
        && list.Any(hook => hook?["command"] is JsonValue command
            && command.TryGetValue(out string? text)
            && text.Contains(HookMarker));

    /// <summary>Copies each bundled skill into the harness skills folder, replacing an older copy. Symlinked skills are left alone.</summary>
    private static void EditSkills(string skillsFolder, string bundled, bool remove, TextWriter output)
    {
        var source = Path.Combine(bundled, "skills");
        if (!Directory.Exists(source))
        {
            return;
        }

        foreach (var skill in Directory.EnumerateDirectories(source))
        {
            var name = Path.GetFileName(skill);
            var target = Path.Combine(skillsFolder, name);
            if (Directory.Exists(target) && new DirectoryInfo(target).LinkTarget is not null)
            {
                output.WriteLine($"Kept linked skill {target}");
                continue;
            }
            if (Directory.Exists(target))
            {
                Directory.Delete(target, recursive: true);
            }
            if (remove)
            {
                continue;
            }

            CopyDirectory(skill, target);
            // repo-template reads the bundled templates as format references.
            if (name == RepoTemplateSkill && Directory.Exists(Path.Combine(bundled, "templates")))
            {
                CopyDirectory(Path.Combine(bundled, "templates"), Path.Combine(target, "reference"));
            }
        }
    }

    private static void CopyDirectory(string source, string target)
    {
        foreach (var file in Directory.EnumerateFiles(source, "*", SearchOption.AllDirectories))
        {
            var destination = Path.Combine(target, Path.GetRelativePath(source, file));
            Directory.CreateDirectory(Path.GetDirectoryName(destination) ?? target);
            File.Copy(file, destination, overwrite: true);
        }
    }

    private static JsonObject Load(string file)
    {
        var text = File.Exists(file) ? File.ReadAllText(file) : "";
        if (string.IsNullOrWhiteSpace(text))
        {
            return new JsonObject();
        }
        // Never overwrite a config we cannot read as an object, that would lose the user's settings.
        return JsonNode.Parse(text) as JsonObject ?? throw new InvalidDataException($"{file} is not a JSON object, left it unchanged");
    }

    private static void Save(string file, JsonObject root)
    {
        Directory.CreateDirectory(Path.GetDirectoryName(file) ?? ".");
        File.WriteAllText(file, root.ToJsonString(Indented) + Environment.NewLine);
    }

    private static bool RunClaude(string[] args)
    {
        var start = new ProcessStartInfo("claude") { RedirectStandardOutput = true, RedirectStandardError = true };
        foreach (var arg in args)
        {
            start.ArgumentList.Add(arg);
        }

        try
        {
            using var process = Process.Start(start);
            if (process is null)
            {
                return false;
            }
            process.BeginOutputReadLine();
            process.BeginErrorReadLine();
            process.WaitForExit();
            return process.ExitCode == 0;
        }
        catch (Win32Exception)
        {
            return false;
        }
    }
}
