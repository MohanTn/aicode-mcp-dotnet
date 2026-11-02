using System.ComponentModel;
using System.Reflection;
using Aicode.Mcp.Servers;
using ModelContextProtocol.Server;

namespace Aicode.Mcp.Tests;

/// <summary>Fails when a tool is added, renamed or removed without the test suite (and README) being updated.</summary>
public class ToolCatalogTests
{
    private static readonly string[] ExpectedTools =
    {
        "get-file-design-pattern",
        "get-merge-request",
        "list-boilerplates",
        "list-scaffolding-methods",
        "post-review-comments",
        "review-code-change",
        "review-merge-request",
        "use-boilerplate",
        "use-scaffold-method",
    };

    private static IEnumerable<(Type Type, MethodInfo Method, McpServerToolAttribute Tool)> AllTools() =>
        typeof(ArchitectTools).Assembly.GetTypes()
            .Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() is not null)
            .SelectMany(t => t.GetMethods(BindingFlags.Public | BindingFlags.Instance | BindingFlags.DeclaredOnly)
                .Select(m => (Type: t, Method: m, Tool: m.GetCustomAttribute<McpServerToolAttribute>()))
                .Where(x => x.Tool is not null)
                .Select(x => (x.Type, x.Method, x.Tool!)));

    [Fact]
    public void Servers_ExposeExactlyTheDocumentedTools()
    {
        var names = AllTools().Select(t => t.Tool.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();

        Assert.Equal(ExpectedTools, names);
    }

    [Fact]
    public void EveryTool_HasADescriptionOnTheMethodAndOnEachParameter()
    {
        foreach (var (type, method, _) in AllTools())
        {
            Assert.True(method.GetCustomAttribute<DescriptionAttribute>() is not null, $"{type.Name}.{method.Name} has no description");
            foreach (var parameter in method.GetParameters().Where(p => p.ParameterType != typeof(CancellationToken)))
            {
                Assert.True(parameter.GetCustomAttribute<DescriptionAttribute>() is not null, $"{type.Name}.{method.Name}({parameter.Name}) has no description");
            }
        }
    }

    [Fact]
    public void EveryTool_ReturnsAJsonString()
    {
        foreach (var (type, method, _) in AllTools())
        {
            var returnType = method.ReturnType;
            Assert.True(
                returnType == typeof(string) || returnType == typeof(Task<string>),
                $"{type.Name}.{method.Name} returns {returnType.Name}");
        }
    }

    [Fact]
    public void EveryServer_ExposesAtLeastOneToolAndNoToolNameRepeats()
    {
        var tools = AllTools().ToList();

        Assert.Equal(tools.Count, tools.Select(t => t.Tool.Name).Distinct().Count());
        Assert.Equal(3, tools.Select(t => t.Type).Distinct().Count());
    }
}
