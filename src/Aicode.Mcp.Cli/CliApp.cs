namespace Aicode.Mcp.Cli;

public static class CliApp
{
    private const string Usage = """
        aicode-mcp <command>

        Commands:
          mcp-serve --server scaffold|architect|reviewer
              Run one MCP server over stdio.
              reviewer also needs: --gitlab-token-env <ENV_VAR_NAME> [--gitlab-url https://gitlab.com]
          hook --type scaffold|architect
              Claude Code hook. Reads the hook JSON from stdin and writes the result to stdout.
          init
              Create .toolkit/settings.yaml and .mcp.json, and print the hooks to add to .claude/settings.json.
        """;

    public static async Task<int> RunAsync(string[] args)
    {
        switch (args.FirstOrDefault())
        {
            case "mcp-serve":
                return await McpServeCommand.RunAsync(args);
            case "hook":
                return await HookCommand.RunAsync(args);
            case "init":
                return InitCommand.Run(Directory.GetCurrentDirectory(), Console.Out);
            default:
                Console.Error.WriteLine(Usage);
                return args.Length == 0 || args[0] is "-h" or "--help" or "help" ? 0 : 1;
        }
    }
}
