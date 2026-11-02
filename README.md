# aicode-mcp-dotnet

Three MCP servers and a Claude Code hook runner for .NET and React projects, shipped as one `dotnet tool` named `aicode-mcp`. It ports the scaffold and architect ideas from aicode-toolkit to C# and adds a GitLab merge request reviewer.

| Server | Tools |
| --- | --- |
| `scaffold` | `list-boilerplates`, `use-boilerplate`, `list-scaffolding-methods`, `use-scaffold-method` |
| `architect` | `get-file-design-pattern`, `review-code-change` |
| `reviewer` | `get-merge-request`, `review-merge-request`, `post-review-comments` |

## Layout

- `src/Aicode.Mcp.Core`: YAML models, glob matching, project and rule lookup, scaffold rendering (Liquid via Fluid), hooks, GitLab client
- `src/Aicode.Mcp.Servers`: MCP tool classes
- `src/Aicode.Mcp.Cli`: the `aicode-mcp` tool (`mcp-serve`, `hook`, `init`)
- `templates/dotnet-api`, `templates/react-web`: `scaffold.yaml`, `architect.yaml`, `RULES.yaml` and template files
- `tests/Aicode.Mcp.Tests`: xUnit tests. They copy the real `templates/` folder into a temp workspace

## Build and test

Needs the .NET 10 SDK.

```bash
dotnet test aicode-mcp-dotnet.slnx
```

`McpProtocolTests` starts the real `aicode-mcp` process for each server and calls all 9 tools over stdio. The reviewer talks to a fake GitLab served from the test. `ToolCatalogTests` fails if a tool is added or renamed without updating the suite.

## Use it

```bash
dotnet pack src/Aicode.Mcp.Cli -o nupkg
dotnet tool install --global --add-source nupkg Aicode.Mcp.Cli
cd your-project && aicode-mcp init
```

`init` writes `.toolkit/settings.yaml` and `.mcp.json`, then prints the hooks to add to `.claude/settings.json`. Copy the template folders you need into your project's `templates/` folder.

A project is tied to a template by `sourceTemplate` in its `project.json`, or in `.toolkit/settings.yaml` for single-project repos.

## Reviewer

Set an environment variable to a GitLab access token with the `api` scope. Pass its name to the server with `--gitlab-token-env <NAME>`, and optionally `--gitlab-url` for self-hosted GitLab. Check out the merge request branch, then ask the agent to call `review-merge-request`. It returns each diff with the rules for its template. The agent reviews, then calls `post-review-comments`.

## Hooks

- `aicode-mcp hook --type scaffold` (PreToolUse on Write): the first Write of a new file is denied once with the scaffold methods that fit. Repeating the Write goes through.
- `aicode-mcp hook --type architect` (PostToolUse on Edit, Write, MultiEdit): adds the matching RULES.yaml rules as context. It never blocks.

Both hooks fail open and always exit 0.

## Not built yet

stdio is the only transport. There is no LLM-backed review mode, no `write-to-file` tool, no scaffold markers, and GitHub support.
