using Aicode.Mcp.Core.Architect;
using Aicode.Mcp.Core.Hooks;
using Aicode.Mcp.Core.Scaffold;

namespace Aicode.Mcp.Cli;

public static class HookCommand
{
    /// <summary>Always exits 0 and prints valid JSON, so a hook bug can never block the agent.</summary>
    public static async Task<int> RunAsync(string[] args)
    {
        var type = ArgumentReader.Option(args, "--type");
        var eventName = "";
        HookResponse response;
        try
        {
            var input = HookInput.Parse(await Console.In.ReadToEndAsync());
            eventName = input.EventName;
            response = type switch
            {
                "scaffold" => new NewFileGateHook(new ScaffoldService()).PreToolUse(input),
                "architect" => new ReviewHook(new RuleFinder(), new DesignPatternFinder()).PostToolUse(input),
                _ => HookResponse.Skipped("Unknown hook type, expected scaffold or architect"),
            };
        }
        catch (Exception ex)
        {
            response = HookResponse.Skipped($"Hook error: {ex.Message}");
        }

        Console.Out.Write(ClaudeHookFormatter.Format(eventName, response));
        return 0;
    }
}
