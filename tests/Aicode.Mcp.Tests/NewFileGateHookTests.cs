using Aicode.Mcp.Core.Hooks;
using Aicode.Mcp.Core.Scaffold;

namespace Aicode.Mcp.Tests;

public class NewFileGateHookTests
{
    private static HookInput WriteInput(TestWorkspace workspace, string relative, string session = "s1") =>
        new("PreToolUse", "Write", workspace.PathOf(relative), workspace.Root, session);

    [Fact]
    public void PreToolUse_DeniesFirstWriteThenLetsTheRepeatThrough()
    {
        using var workspace = new TestWorkspace();
        var hook = new NewFileGateHook(new ScaffoldService(), workspace.LogDirectory);
        var input = WriteInput(workspace, "Features/Orders/NewThing.cs");

        var first = hook.PreToolUse(input);
        var second = hook.PreToolUse(input);

        Assert.Equal(HookResponse.Deny, first.Decision);
        Assert.Contains("add-use-case", first.Message);
        Assert.Equal(HookResponse.Skip, second.Decision);
    }

    [Fact]
    public void PreToolUse_SkipsExistingFiles()
    {
        using var workspace = new TestWorkspace();
        workspace.Write("Features/Orders/Old.cs", "// old");
        var hook = new NewFileGateHook(new ScaffoldService(), workspace.LogDirectory);

        Assert.Equal(HookResponse.Skip, hook.PreToolUse(WriteInput(workspace, "Features/Orders/Old.cs")).Decision);
    }

    [Fact]
    public void PreToolUse_SkipsToolsOtherThanWrite()
    {
        using var workspace = new TestWorkspace();
        var hook = new NewFileGateHook(new ScaffoldService(), workspace.LogDirectory);
        var input = new HookInput("PreToolUse", "Edit", workspace.PathOf("a.cs"), workspace.Root, "s1");

        Assert.Equal(HookResponse.Skip, hook.PreToolUse(input).Decision);
    }

    [Fact]
    public void PreToolUse_AllowsFilesMatchingExcludeGlobs()
    {
        using var workspace = new TestWorkspace(extraSettings: "scaffold-mcp:\n  hook:\n    excludeGlobs:\n      - \"docs/**/*.md\"\n");
        var hook = new NewFileGateHook(new ScaffoldService(), workspace.LogDirectory);

        var response = hook.PreToolUse(WriteInput(workspace, "docs/guide.md"));

        Assert.Equal(HookResponse.Allow, response.Decision);
    }

    [Fact]
    public void PreToolUse_AllowsFilesMatchingTemplateExclude()
    {
        using var workspace = new TestWorkspace();
        var hook = new NewFileGateHook(new ScaffoldService(), workspace.LogDirectory);

        Assert.Equal(HookResponse.Allow, hook.PreToolUse(WriteInput(workspace, "Api.csproj")).Decision);
    }

    [Fact]
    public void PreToolUse_FailsOpenOnBadTemplateConfig()
    {
        using var workspace = new TestWorkspace();
        workspace.Write("templates/dotnet-api/scaffold.yaml", "features: [this is: not valid");
        var hook = new NewFileGateHook(new ScaffoldService(), workspace.LogDirectory);

        var response = hook.PreToolUse(WriteInput(workspace, "Features/Orders/NewThing.cs"));

        Assert.Equal(HookResponse.Skip, response.Decision);
        Assert.StartsWith("Hook error:", response.Message);
    }
}
