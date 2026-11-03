using Aicode.Mcp.Core.Architect;
using Aicode.Mcp.Core.Infrastructure;
using Aicode.Mcp.Core.Reviewer;
using Aicode.Mcp.Core.Scaffold;

namespace Aicode.Mcp.Cli;

/// <summary>
/// Prints, as JSON, the template rules, design patterns and feature template for each changed file,
/// so an external reviewer such as pipeline-worker can check a diff against the repo's own templates.
/// </summary>
public static class ReviewContextCommand
{
    public static int Run(string directory, IReadOnlyList<string> files, TextWriter output)
    {
        var builder = new ReviewContextBuilder(new RuleFinder(), new DesignPatternFinder(), new ScaffoldService());
        output.WriteLine(JsonOutput.Serialize(new { files = builder.Build(files, directory) }));
        return 0;
    }
}
