using System.Text.Json;
using Aicode.Mcp.Cli;

namespace Aicode.Mcp.Tests;

public class CliCommandTests
{
    [Fact]
    public void Option_ReadsTheValueAfterTheFlag()
    {
        var args = new[] { "mcp-serve", "--server", "architect", "--gitlab-url", "https://gl.example" };

        Assert.Equal("architect", ArgumentReader.Option(args, "--server"));
        Assert.Equal("https://gl.example", ArgumentReader.Option(args, "--gitlab-url"));
        Assert.Null(ArgumentReader.Option(args, "--missing"));
    }

    [Fact]
    public void Option_IgnoresAFlagWithNoValue()
    {
        Assert.Null(ArgumentReader.Option(new[] { "hook", "--type" }, "--type"));
    }

    [Fact]
    public void Init_CreatesSettingsAndMcpConfigAndPrintsTheHooks()
    {
        using var workspace = new TestWorkspace(sourceTemplate: null);
        var directory = workspace.PathOf("project");
        var output = new StringWriter();

        var exitCode = InitCommand.Run(directory, output);

        Assert.Equal(0, exitCode);
        Assert.Contains("sourceTemplate:", File.ReadAllText(Path.Combine(directory, ".toolkit", "settings.yaml")));
        using var mcp = JsonDocument.Parse(File.ReadAllText(Path.Combine(directory, ".mcp.json")));
        var servers = mcp.RootElement.GetProperty("mcpServers");
        Assert.Equal("scaffold", servers.GetProperty("scaffold-mcp").GetProperty("args")[2].GetString());
        Assert.Equal("architect", servers.GetProperty("architect-mcp").GetProperty("args")[2].GetString());
        Assert.Equal("reviewer", servers.GetProperty("reviewer-mcp").GetProperty("args")[2].GetString());
        Assert.Contains("aicode-mcp hook --type scaffold", output.ToString());
        Assert.Contains("aicode-mcp hook --type architect", output.ToString());
    }

    [Fact]
    public void Init_PrintsHooksThatAreValidJson()
    {
        using var workspace = new TestWorkspace(sourceTemplate: null);
        var output = new StringWriter();

        InitCommand.Run(workspace.PathOf("project"), output);

        var text = output.ToString();
        var json = text[text.IndexOf('{')..];
        using var doc = JsonDocument.Parse(json);
        Assert.Equal("Write", doc.RootElement.GetProperty("hooks").GetProperty("PreToolUse")[0].GetProperty("matcher").GetString());
    }

    [Fact]
    public void Init_KeepsExistingFiles()
    {
        using var workspace = new TestWorkspace(sourceTemplate: null);
        var directory = workspace.PathOf("project");
        workspace.Write("project/.mcp.json", "{ \"mine\": true }");

        InitCommand.Run(directory, new StringWriter());

        Assert.Equal("{ \"mine\": true }", File.ReadAllText(Path.Combine(directory, ".mcp.json")));
        Assert.True(File.Exists(Path.Combine(directory, ".toolkit", "settings.yaml")));
    }

    [Fact]
    public async Task McpServe_RejectsAnUnknownServer()
    {
        Assert.Equal(1, await McpServeCommand.RunAsync(new[] { "mcp-serve", "--server", "nope" }));
    }

    [Fact]
    public async Task McpServe_ReviewerNeedsTheTokenEnvOption()
    {
        Assert.Equal(1, await McpServeCommand.RunAsync(new[] { "mcp-serve", "--server", "reviewer" }));
    }

    [Fact]
    public async Task Run_ReturnsOneForAnUnknownCommand()
    {
        Assert.Equal(1, await CliApp.RunAsync(new[] { "bogus" }));
    }
}
