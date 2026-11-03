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
          init [--quiet] [--template dotnet-api|react-web]
              Create .toolkit/settings.yaml, .mcp.json and a templates/ copy.
              --template picks the sourceTemplate when none is configured yet (default dotnet-api).
              --quiet is for a SessionStart hook where the servers are registered user-wide:
              it skips .mcp.json and does not print the hooks to add to .claude/settings.json.
          setup [--remove]
              Register scaffold-mcp and architect-mcp user-wide in Claude Code, Copilot CLI and Pi,
              add the Claude Code hooks and install the aicode-init and repo-template skills.
              --remove takes them out again.
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
                var quiet = args.Contains("--quiet");
                return InitCommand.Run(
                    Directory.GetCurrentDirectory(),
                    Console.Out,
                    printHooks: !quiet,
                    template: ArgumentReader.Option(args, "--template"),
                    writeMcpJson: !quiet);
            case "setup":
                return SetupCommand.Run(
                    Environment.GetFolderPath(Environment.SpecialFolder.UserProfile),
                    Console.Out,
                    remove: args.Contains("--remove"));
            default:
                Console.Error.WriteLine(Usage);
                return args.Length == 0 || args[0] is "-h" or "--help" or "help" ? 0 : 1;
        }
    }
}
