using System.Text.Json.Nodes;
using Aicode.Mcp.Cli;

namespace Aicode.Mcp.Tests;

public class SetupCommandTests
{
    private static (string Home, string Bundled, List<string> ClaudeCalls) Arrange(TestWorkspace workspace)
    {
        var home = workspace.PathOf("home");
        Directory.CreateDirectory(Path.Combine(home, ".claude"));
        Directory.CreateDirectory(Path.Combine(home, ".copilot"));
        Directory.CreateDirectory(Path.Combine(home, ".pi", "agent"));
        workspace.Write("bundled/skills/aicode-init/SKILL.md", "init");
        workspace.Write("bundled/skills/repo-template/SKILL.md", "template");
        workspace.Write("bundled/templates/dotnet-api/RULES.yaml", "rules");
        return (home, workspace.PathOf("bundled"), new List<string>());
    }

    private static JsonNode Json(string home, params string[] path) =>
        JsonNode.Parse(File.ReadAllText(Path.Combine([home, .. path]))) ?? throw new InvalidDataException();

    [Fact]
    public void Setup_RegistersServersHooksAndSkillsInEveryHarness()
    {
        using var workspace = new TestWorkspace();
        var (home, bundled, calls) = Arrange(workspace);

        var exitCode = SetupCommand.Run(home, new StringWriter(), bundled: bundled, runClaude: args => { calls.Add(string.Join(' ', args)); return true; });

        Assert.Equal(0, exitCode);
        Assert.Contains("mcp add --scope user scaffold-mcp -- aicode-mcp mcp-serve --server scaffold", calls);
        Assert.Contains("mcp add --scope user architect-mcp -- aicode-mcp mcp-serve --server architect", calls);
        var hooks = Json(home, ".claude", "settings.json")["hooks"];
        Assert.Equal("aicode-mcp hook --type scaffold", hooks?["PreToolUse"]?[0]?["hooks"]?[0]?["command"]?.GetValue<string>());
        Assert.Equal("Edit|Write|MultiEdit", hooks?["PostToolUse"]?[0]?["matcher"]?.GetValue<string>());
        Assert.Equal("local", Json(home, ".copilot", "mcp-config.json")["mcpServers"]?["scaffold-mcp"]?["type"]?.GetValue<string>());
        Assert.Equal("architect", Json(home, ".pi", "agent", "mcp.json")["mcpServers"]?["architect-mcp"]?["args"]?[2]?.GetValue<string>());
        foreach (var skills in new[] { ".claude/skills", ".copilot/skills", ".pi/agent/skills" })
        {
            Assert.True(File.Exists(Path.Combine(home, skills, "aicode-init", "SKILL.md")));
            Assert.True(File.Exists(Path.Combine(home, skills, "repo-template", "reference", "dotnet-api", "RULES.yaml")));
        }
    }

    [Fact]
    public void Setup_RunTwiceKeepsOneEntryAndOtherSettings()
    {
        using var workspace = new TestWorkspace();
        var (home, bundled, _) = Arrange(workspace);
        workspace.Write("home/.claude/settings.json", """
            { "model": "opus", "hooks": { "PreToolUse": [ { "matcher": "Bash", "hooks": [ { "type": "command", "command": "guard.sh" } ] } ] } }
            """);
        workspace.Write("home/.copilot/mcp-config.json", """{ "mcpServers": { "other": { "command": "x" } } }""");

        SetupCommand.Run(home, new StringWriter(), bundled: bundled, runClaude: _ => true);
        SetupCommand.Run(home, new StringWriter(), bundled: bundled, runClaude: _ => true);

        var settings = Json(home, ".claude", "settings.json");
        Assert.Equal("opus", settings["model"]?.GetValue<string>());
        var preToolUse = settings["hooks"]?["PreToolUse"]?.AsArray();
        Assert.Equal(2, preToolUse?.Count);
        Assert.Equal("Bash", preToolUse?[0]?["matcher"]?.GetValue<string>());
        Assert.Single(settings["hooks"]?["PostToolUse"]?.AsArray() ?? new JsonArray());
        Assert.NotNull(Json(home, ".copilot", "mcp-config.json")["mcpServers"]?["other"]);
    }

    [Fact]
    public void Setup_SkipsHarnessesThatAreNotInstalled()
    {
        using var workspace = new TestWorkspace();
        var home = workspace.PathOf("home");
        Directory.CreateDirectory(Path.Combine(home, ".pi", "agent"));
        var calls = new List<string>();
        var output = new StringWriter();

        SetupCommand.Run(home, output, bundled: workspace.PathOf("bundled"), runClaude: args => { calls.Add(string.Join(' ', args)); return true; });

        Assert.Empty(calls);
        Assert.False(Directory.Exists(Path.Combine(home, ".claude")));
        Assert.False(Directory.Exists(Path.Combine(home, ".copilot")));
        Assert.True(File.Exists(Path.Combine(home, ".pi", "agent", "mcp.json")));
        Assert.Contains("skipped claude", output.ToString());
    }

    [Fact]
    public void Setup_PrintsTheManualCommandWhenClaudeFails()
    {
        using var workspace = new TestWorkspace();
        var (home, bundled, _) = Arrange(workspace);
        var output = new StringWriter();

        SetupCommand.Run(home, output, bundled: bundled, runClaude: _ => false);

        Assert.Contains("run: claude mcp add --scope user scaffold-mcp -- aicode-mcp mcp-serve --server scaffold", output.ToString());
    }

    [Fact]
    public void Setup_LeavesAConfigThatIsNotAJsonObjectUnchanged()
    {
        using var workspace = new TestWorkspace();
        var (home, bundled, _) = Arrange(workspace);
        workspace.Write("home/.claude/settings.json", "[1]");

        Assert.Throws<InvalidDataException>(() => SetupCommand.Run(home, new StringWriter(), bundled: bundled, runClaude: _ => true));
        Assert.Equal("[1]", File.ReadAllText(Path.Combine(home, ".claude", "settings.json")));
    }

    [Fact]
    public void Remove_TakesOutOnlyWhatSetupAdded()
    {
        using var workspace = new TestWorkspace();
        var (home, bundled, calls) = Arrange(workspace);
        workspace.Write("home/.pi/agent/mcp.json", """{ "mcpServers": { "other": { "command": "x" } } }""");
        workspace.Write("home/.claude/skills/mine/SKILL.md", "mine");
        SetupCommand.Run(home, new StringWriter(), bundled: bundled, runClaude: _ => true);

        SetupCommand.Run(home, new StringWriter(), remove: true, bundled: bundled, runClaude: args => { calls.Add(string.Join(' ', args)); return true; });

        Assert.Contains("mcp remove --scope user scaffold-mcp", calls);
        Assert.DoesNotContain(calls, call => call.StartsWith("mcp add"));
        Assert.Empty(Json(home, ".claude", "settings.json")["hooks"]?["PreToolUse"]?.AsArray() ?? new JsonArray(new JsonObject()));
        var piServers = Json(home, ".pi", "agent", "mcp.json")["mcpServers"]?.AsObject();
        Assert.NotNull(piServers?["other"]);
        Assert.False(piServers?.ContainsKey("scaffold-mcp"));
        Assert.False(Directory.Exists(Path.Combine(home, ".claude", "skills", "aicode-init")));
        Assert.True(File.Exists(Path.Combine(home, ".claude", "skills", "mine", "SKILL.md")));
    }
}
