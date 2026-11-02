using System.Net;
using System.Net.Sockets;
using System.Text;
using System.Text.Json;
using ModelContextProtocol.Client;
using ModelContextProtocol.Protocol;

namespace Aicode.Mcp.Tests;

/// <summary>
/// Starts the real aicode-mcp process for each server and calls every tool over the MCP stdio protocol.
/// This proves tool registration, dependency injection, argument binding and error mapping, not just the C# methods.
/// </summary>
public class McpProtocolTests
{
    private const string ChangesJson = """
        {
          "title": "Add orders", "description": "desc", "source_branch": "feature/orders", "target_branch": "main",
          "web_url": "http://gitlab.test/g/p/-/merge_requests/7",
          "changes": [
            { "old_path": "Features/Orders/OrdersController.cs", "new_path": "Features/Orders/OrdersController.cs",
              "new_file": false, "deleted_file": false, "renamed_file": false, "diff": "+x" }
          ]
        }
        """;

    private static async Task<McpClient> StartAsync(
        TestWorkspace workspace,
        string server,
        IEnumerable<string>? extraArgs = null,
        IDictionary<string, string?>? environment = null)
    {
        var dll = Path.Combine(AppContext.BaseDirectory, "aicode-mcp.dll");
        var arguments = new List<string> { dll, "mcp-serve", "--server", server };
        arguments.AddRange(extraArgs ?? Array.Empty<string>());

        var transport = new StdioClientTransport(new StdioClientTransportOptions
        {
            Name = server,
            Command = "dotnet",
            Arguments = arguments,
            WorkingDirectory = workspace.Root,
            EnvironmentVariables = environment ?? new Dictionary<string, string?>(),
        });
        return await McpClient.CreateAsync(transport);
    }

    private static string TextOf(CallToolResult result) =>
        string.Concat(result.Content.OfType<TextContentBlock>().Select(b => b.Text));

    private static async Task<JsonDocument> CallOkAsync(McpClient client, string tool, Dictionary<string, object?> arguments)
    {
        var result = await client.CallToolAsync(tool, arguments);
        Assert.True(result.IsError != true, $"{tool} failed: {TextOf(result)}");
        return JsonDocument.Parse(TextOf(result));
    }

    private static async Task<string> CallErrorAsync(McpClient client, string tool, Dictionary<string, object?> arguments)
    {
        var result = await client.CallToolAsync(tool, arguments);
        Assert.True(result.IsError == true, $"{tool} should have failed but returned: {TextOf(result)}");
        return TextOf(result);
    }

    [Fact]
    public async Task ArchitectServer_ListsAndCallsBothTools()
    {
        using var workspace = new TestWorkspace();
        await using var client = await StartAsync(workspace, "architect");

        var names = (await client.ListToolsAsync()).Select(t => t.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();
        Assert.Equal(new[] { "get-file-design-pattern", "review-code-change" }, names);

        var file = workspace.PathOf("Features/Orders/OrdersController.cs");
        using (var pattern = await CallOkAsync(client, "get-file-design-pattern", new() { ["filePath"] = file }))
        {
            Assert.Equal("controller", pattern.RootElement.GetProperty("rules").GetProperty("pattern").GetString());
            Assert.Equal("Controller", pattern.RootElement.GetProperty("design_patterns")[0].GetProperty("name").GetString());
        }

        using (var review = await CallOkAsync(client, "review-code-change", new() { ["filePath"] = file }))
        {
            Assert.Contains("must_do", review.RootElement.GetProperty("feedback").GetString());
        }

        var error = await CallErrorAsync(client, "get-file-design-pattern", new() { ["filePath"] = Path.Combine(Path.GetTempPath(), "nowhere", "a.cs") });
        Assert.Contains("sourceTemplate", error);
    }

    [Fact]
    public async Task ScaffoldServer_ListsAndCallsAllFourTools()
    {
        using var workspace = new TestWorkspace();
        await using var client = await StartAsync(workspace, "scaffold");

        var names = (await client.ListToolsAsync()).Select(t => t.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();
        Assert.Equal(
            new[] { "list-boilerplates", "list-scaffolding-methods", "use-boilerplate", "use-scaffold-method" },
            names);

        using (var boilerplates = await CallOkAsync(client, "list-boilerplates", new() { ["workspacePath"] = workspace.Root }))
        {
            Assert.Equal(2, boilerplates.RootElement.GetProperty("boilerplates").GetArrayLength());
        }

        var target = workspace.PathOf("out/web");
        using (var created = await CallOkAsync(client, "use-boilerplate", new()
        {
            ["boilerplateName"] = "react-web",
            ["targetFolder"] = target,
            ["variablesJson"] = """{ "projectName": "e2e-web" }""",
            ["templateName"] = "react-web",
            ["workspacePath"] = workspace.Root,
        }))
        {
            Assert.True(created.RootElement.GetProperty("created_files").GetArrayLength() > 0);
        }

        Assert.Contains("e2e-web", await File.ReadAllTextAsync(Path.Combine(target, "package.json")));

        using (var methods = await CallOkAsync(client, "list-scaffolding-methods", new() { ["projectPath"] = workspace.Root }))
        {
            Assert.Equal("add-use-case", methods.RootElement.GetProperty("methods")[0].GetProperty("name").GetString());
        }

        using (var method = await CallOkAsync(client, "use-scaffold-method", new()
        {
            ["projectPath"] = workspace.Root,
            ["methodName"] = "add-use-case",
            ["variablesJson"] = """{ "rootNamespace": "Orders.Api", "feature": "Orders", "useCase": "CreateOrder" }""",
        }))
        {
            Assert.Equal("add-use-case", method.RootElement.GetProperty("method").GetString());
        }

        Assert.True(File.Exists(workspace.PathOf("Features/Orders/CreateOrder/CreateOrderHandler.cs")));

        var error = await CallErrorAsync(client, "use-scaffold-method", new()
        {
            ["projectPath"] = workspace.Root,
            ["methodName"] = "add-use-case",
            ["variablesJson"] = """{ "feature": "Orders" }""",
        });
        Assert.Contains("useCase", error);
    }

    [Fact]
    public async Task ReviewerServer_ListsAndCallsAllThreeToolsAgainstAFakeGitLab()
    {
        using var workspace = new TestWorkspace();
        using var gitlab = FakeGitLab.Start(ChangesJson);
        await using var client = await StartAsync(
            workspace,
            "reviewer",
            new[] { "--gitlab-url", gitlab.BaseUrl, "--gitlab-token-env", "E2E_GL_CRED" },
            new Dictionary<string, string?> { ["E2E_GL_CRED"] = "e2e-value" });

        var names = (await client.ListToolsAsync()).Select(t => t.Name).OrderBy(n => n, StringComparer.Ordinal).ToList();
        Assert.Equal(new[] { "get-merge-request", "post-review-comments", "review-merge-request" }, names);

        using (var mr = await CallOkAsync(client, "get-merge-request", new() { ["project"] = "g/p", ["mergeRequestIid"] = 7 }))
        {
            Assert.Equal("Add orders", mr.RootElement.GetProperty("title").GetString());
        }

        using (var review = await CallOkAsync(client, "review-merge-request", new()
        {
            ["project"] = "g/p",
            ["mergeRequestIid"] = 7,
            ["workspacePath"] = workspace.Root,
        }))
        {
            Assert.Equal("controller", review.RootElement.GetProperty("files")[0].GetProperty("rules").GetProperty("pattern").GetString());
        }

        using (var posted = await CallOkAsync(client, "post-review-comments", new()
        {
            ["project"] = "g/p",
            ["mergeRequestIid"] = 7,
            ["body"] = "Looks good",
        }))
        {
            Assert.True(posted.RootElement.GetProperty("posted").GetBoolean());
        }

        Assert.Contains("Looks good", Assert.Single(gitlab.PostedBodies));
        Assert.All(gitlab.CredentialHeaders, h => Assert.Equal("e2e-value", h));

        await CallErrorAsync(client, "post-review-comments", new() { ["project"] = "g/p", ["mergeRequestIid"] = 7, ["body"] = " " });
    }

    [Fact]
    public async Task ReviewerServer_ReportsGitLabFailuresAsToolErrors()
    {
        using var workspace = new TestWorkspace();
        using var gitlab = FakeGitLab.Start(ChangesJson, statusCode: 404);
        await using var client = await StartAsync(
            workspace,
            "reviewer",
            new[] { "--gitlab-url", gitlab.BaseUrl, "--gitlab-token-env", "E2E_GL_CRED" },
            new Dictionary<string, string?> { ["E2E_GL_CRED"] = "e2e-value" });

        var error = await CallErrorAsync(client, "get-merge-request", new() { ["project"] = "g/p", ["mergeRequestIid"] = 7 });

        Assert.Contains("404", error);
    }

    private sealed class FakeGitLab : IDisposable
    {
        private readonly HttpListener _listener = new();
        private readonly int _statusCode;
        private readonly string _changesJson;

        private FakeGitLab(string changesJson, int statusCode)
        {
            _changesJson = changesJson;
            _statusCode = statusCode;
        }

        public string BaseUrl { get; private set; } = "";

        public List<string> PostedBodies { get; } = new();

        public List<string> CredentialHeaders { get; } = new();

        public static FakeGitLab Start(string changesJson, int statusCode = 200)
        {
            var probe = new TcpListener(IPAddress.Loopback, 0);
            probe.Start();
            var port = ((IPEndPoint)probe.LocalEndpoint).Port;
            probe.Stop();

            var server = new FakeGitLab(changesJson, statusCode) { BaseUrl = $"http://127.0.0.1:{port}" };
            server._listener.Prefixes.Add(server.BaseUrl + "/");
            server._listener.Start();
            _ = Task.Run(server.ServeAsync);
            return server;
        }

        private async Task ServeAsync()
        {
            while (_listener.IsListening)
            {
                HttpListenerContext context;
                try
                {
                    context = await _listener.GetContextAsync();
                }
                catch (Exception ex) when (ex is HttpListenerException or ObjectDisposedException)
                {
                    return;
                }

                var path = context.Request.Url!.AbsolutePath;
                CredentialHeaders.Add(context.Request.Headers["PRIVATE-TOKEN"] ?? "");
                string body;
                if (_statusCode != 200)
                {
                    body = "{\"message\":\"not found\"}";
                    context.Response.StatusCode = _statusCode;
                }
                else if (path.EndsWith("/changes", StringComparison.Ordinal))
                {
                    body = _changesJson;
                }
                else
                {
                    using var reader = new StreamReader(context.Request.InputStream, Encoding.UTF8);
                    PostedBodies.Add(await reader.ReadToEndAsync());
                    context.Response.StatusCode = 201;
                    body = "{}";
                }

                var bytes = Encoding.UTF8.GetBytes(body);
                context.Response.ContentType = "application/json";
                await context.Response.OutputStream.WriteAsync(bytes);
                context.Response.Close();
            }
        }

        public void Dispose() => _listener.Close();
    }
}
