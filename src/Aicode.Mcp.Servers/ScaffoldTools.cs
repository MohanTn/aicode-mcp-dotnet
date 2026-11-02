using System.ComponentModel;
using Aicode.Mcp.Core.Infrastructure;
using Aicode.Mcp.Core.Models;
using Aicode.Mcp.Core.Projects;
using Aicode.Mcp.Core.Scaffold;
using ModelContextProtocol;
using ModelContextProtocol.Server;

namespace Aicode.Mcp.Servers;

[McpServerToolType]
public class ScaffoldTools(ScaffoldService scaffold)
{
    [McpServerTool(Name = "list-boilerplates")]
    [Description("List the project boilerplates available in the templates folder, with the variables each one needs.")]
    public string ListBoilerplates(
        [Description("A directory inside the workspace. Defaults to the current directory.")] string? workspacePath = null)
    {
        var context = ProjectContext.Resolve(workspacePath ?? ".", Directory.GetCurrentDirectory());
        var boilerplates = scaffold.ListTemplates(context.TemplatesPath)
            .SelectMany(template => (scaffold.LoadConfig(context.TemplatesPath, template)?.Boilerplate ?? new List<ScaffoldEntry>())
                .Select(entry => Summarize(template, entry)))
            .ToList();
        return JsonOutput.Serialize(new { boilerplates });
    }

    [McpServerTool(Name = "use-boilerplate")]
    [Description("Create a new project from a boilerplate. Existing files are never overwritten.")]
    public async Task<string> UseBoilerplate(
        [Description("Name from list-boilerplates")] string boilerplateName,
        [Description("Folder to create the project in")] string targetFolder,
        [Description("JSON object with the boilerplate variables, e.g. {\"projectName\":\"orders-api\"}")] string? variablesJson = null,
        [Description("Template name, only needed when two templates share a boilerplate name")] string? templateName = null,
        [Description("A directory inside the workspace. Defaults to the current directory.")] string? workspacePath = null,
        CancellationToken cancellationToken = default)
    {
        var context = ProjectContext.Resolve(workspacePath ?? ".", Directory.GetCurrentDirectory());
        var matches = scaffold.ListTemplates(context.TemplatesPath)
            .Where(t => templateName is null || t == templateName)
            .SelectMany(t => (scaffold.LoadConfig(context.TemplatesPath, t)?.Boilerplate ?? new List<ScaffoldEntry>())
                .Where(e => e.Name == boilerplateName)
                .Select(e => (Template: t, Entry: e)))
            .ToList();

        if (matches.Count == 0)
        {
            throw new McpException($"Boilerplate '{boilerplateName}' not found. Call list-boilerplates.");
        }

        if (matches.Count > 1)
        {
            throw new McpException($"Boilerplate '{boilerplateName}' exists in several templates. Pass templateName.");
        }

        var (template, entry) = matches[0];
        var result = await Generate(context.TemplatesPath, template, entry, targetFolder, variablesJson, cancellationToken);
        return JsonOutput.Serialize(new { template, boilerplate = entry.Name, result.CreatedFiles, result.SkippedFiles });
    }

    [McpServerTool(Name = "list-scaffolding-methods")]
    [Description("List the scaffolding methods (features) available for an existing project, with the variables each one needs.")]
    public string ListScaffoldingMethods(
        [Description("Project directory or any file inside it")] string projectPath)
    {
        var (context, template) = ResolveProject(projectPath);
        var methods = (scaffold.LoadConfig(context.TemplatesPath, template)?.Features ?? new List<ScaffoldEntry>())
            .Select(entry => Summarize(template, entry))
            .ToList();
        return JsonOutput.Serialize(new { project = context.Project!.Name, source_template = template, methods });
    }

    [McpServerTool(Name = "use-scaffold-method")]
    [Description("Add a feature to an existing project from a scaffolding method. Existing files are never overwritten.")]
    public async Task<string> UseScaffoldMethod(
        [Description("Project directory or any file inside it")] string projectPath,
        [Description("Method name from list-scaffolding-methods")] string methodName,
        [Description("JSON object with the method variables, e.g. {\"feature\":\"Orders\",\"useCase\":\"CreateOrder\"}")] string? variablesJson = null,
        CancellationToken cancellationToken = default)
    {
        var (context, template) = ResolveProject(projectPath);
        var entry = scaffold.LoadConfig(context.TemplatesPath, template)?.Features.FirstOrDefault(f => f.Name == methodName)
            ?? throw new McpException($"Method '{methodName}' not found for template '{template}'. Call list-scaffolding-methods.");

        var result = await Generate(context.TemplatesPath, template, entry, context.Project!.Root, variablesJson, cancellationToken);
        return JsonOutput.Serialize(new
        {
            template,
            method = entry.Name,
            result.CreatedFiles,
            result.SkippedFiles,
            next_step = "Open the created files and replace the placeholder bodies with the real implementation.",
        });
    }

    private async Task<ScaffoldResult> Generate(
        string templatesPath,
        string template,
        ScaffoldEntry entry,
        string targetFolder,
        string? variablesJson,
        CancellationToken cancellationToken)
    {
        try
        {
            var variables = JsonValues.ParseObject(variablesJson);
            var target = Path.GetFullPath(targetFolder, Directory.GetCurrentDirectory());
            return await scaffold.GenerateAsync(templatesPath, template, entry, target, variables, cancellationToken);
        }
        catch (Exception ex) when (ex is ArgumentException or FileNotFoundException or InvalidOperationException or System.Text.Json.JsonException)
        {
            throw new McpException(ex.Message);
        }
    }

    private static (ProjectContext Context, string Template) ResolveProject(string projectPath)
    {
        var context = ProjectContext.Resolve(projectPath, Directory.GetCurrentDirectory());
        return context.Project?.SourceTemplate is { } template
            ? (context, template)
            : throw new McpException($"No sourceTemplate found for {projectPath}. Add it to project.json or .toolkit/settings.yaml.");
    }

    private static object Summarize(string template, ScaffoldEntry entry) => new
    {
        template,
        name = entry.Name,
        description = entry.Description,
        variables_schema = JsonValues.Normalize(entry.VariablesSchema),
    };
}
