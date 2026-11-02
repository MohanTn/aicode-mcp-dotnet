using Aicode.Mcp.Core.Reviewer;
using Aicode.Mcp.Servers;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

namespace Aicode.Mcp.Cli;

public static class McpServeCommand
{
    public static async Task<int> RunAsync(string[] args)
    {
        var server = ArgumentReader.Option(args, "--server");
        if (server is not ("scaffold" or "architect" or "reviewer"))
        {
            Console.Error.WriteLine("mcp-serve needs --server scaffold|architect|reviewer");
            return 1;
        }

        var builder = Host.CreateApplicationBuilder();

        // stdout carries the MCP protocol, so every log line must go to stderr.
        builder.Logging.ClearProviders();
        builder.Logging.AddConsole(options => options.LogToStandardErrorThreshold = LogLevel.Trace);
        builder.Services.AddAicodeCore();

        var mcp = builder.Services.AddMcpServer().WithStdioServerTransport();
        switch (server)
        {
            case "scaffold":
                mcp.WithTools<ScaffoldTools>();
                break;
            case "architect":
                mcp.WithTools<ArchitectTools>();
                break;
            default:
                var tokenEnv = ArgumentReader.Option(args, "--gitlab-token-env");
                if (tokenEnv is null)
                {
                    Console.Error.WriteLine("reviewer needs --gitlab-token-env <name of the env var that holds the GitLab access token>");
                    return 1;
                }

                var url = ArgumentReader.Option(args, "--gitlab-url") ?? "https://gitlab.com";
                builder.Services.AddGitLabReviewer(new GitLabOptions(url, Environment.GetEnvironmentVariable(tokenEnv)));
                mcp.WithTools<ReviewerTools>();
                break;
        }

        await builder.Build().RunAsync();
        return 0;
    }
}
