using Aicode.Mcp.Core.Architect;
using Aicode.Mcp.Core.Reviewer;
using Aicode.Mcp.Core.Scaffold;
using Microsoft.Extensions.DependencyInjection;

namespace Aicode.Mcp.Servers;

public static class ServiceCollectionExtensions
{
    /// <summary>Registers the finders and scaffold service shared by every server.</summary>
    public static IServiceCollection AddAicodeCore(this IServiceCollection services) => services
        .AddSingleton<RuleFinder>()
        .AddSingleton<DesignPatternFinder>()
        .AddSingleton<ScaffoldService>();

    /// <summary>Registers the GitLab merge request provider with the given connection settings.</summary>
    public static IServiceCollection AddGitLabReviewer(this IServiceCollection services, GitLabOptions options) => services
        .AddSingleton(options)
        .AddSingleton(new HttpClient())
        .AddSingleton<IMergeRequestProvider>(sp =>
            new GitLabProvider(sp.GetRequiredService<HttpClient>(), sp.GetRequiredService<GitLabOptions>()))
        .AddSingleton<MergeRequestReviewer>();
}
