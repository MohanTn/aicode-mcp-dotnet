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

        var mcp = builder.Services
            .AddMcpServer(options => options.ServerInstructions = Instructions(server))
            .WithStdioServerTransport();
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

    /// <summary>Workflow sent to the client at initialize, so agents use the tools without a hook having to teach them.</summary>
    public static string Instructions(string server) => server switch
    {
        "scaffold" =>
            "Generate code from the repo's templates instead of hand-writing boilerplate. " +
            "A project follows the template named by sourceTemplate in project.json or .toolkit/settings.yaml. " +
            "New project: list-boilerplates, then use-boilerplate. " +
            "New file or feature in an existing project: list-scaffolding-methods with the project path, then use-scaffold-method, " +
            "then replace the placeholder bodies in the created files. Write a file by hand only when no method fits.",
        "architect" =>
            "Before editing a file call get-file-design-pattern to get its design patterns and rules. " +
            "After editing call review-code-change and fix every must_do and must_not_do violation it lists.",
        _ =>
            "Check out the merge request branch, then call review-merge-request. " +
            "Review each diff against the rules returned for it, then call post-review-comments with your findings.",
    };
}
