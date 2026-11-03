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
- `src/Aicode.Mcp.Cli`: the `aicode-mcp` tool (`mcp-serve`, `hook`, `init`, `setup`)
- `templates/dotnet-api`, `templates/react-web`: `scaffold.yaml`, `architect.yaml`, `RULES.yaml` and template files
- `skills/aicode-init`, `skills/repo-template`: agent skills that `setup` installs for Claude Code, Copilot CLI and Pi
- `tests/Aicode.Mcp.Tests`: xUnit tests. They copy the real `templates/` folder into a temp workspace

## Build and test

Needs the .NET 10 SDK.

```bash
dotnet test aicode-mcp-dotnet.slnx
```

`McpProtocolTests` starts the real `aicode-mcp` process for each server and calls all 9 tools over stdio. The reviewer talks to a fake GitLab served from the test. `ToolCatalogTests` fails if a tool is added or renamed without updating the suite.

## Install

There are two ways in. Pick one, not both.

### Option A: with mohan-dotfiles (everything automatic)

If you use [mohan-dotfiles](https://github.com/MohanTn/mohan-dotfiles), answer yes to "9b. Set up the aicode-mcp MCP servers" in `./setup-packages.sh`, then run `./setup.sh`. It clones this repo to `~/.local/share/aicode-mcp-dotnet`, builds it, rebuilds on every pull, registers the servers and hooks for Claude Code, Copilot CLI and Pi, and initialises .NET and React repos at session start. Nothing below is needed.

### Option B: standalone

Needs the .NET 10 SDK and git. Pi also needs an MCP extension that reads `~/.pi/agent/mcp.json` (for example `pi-mcp-adapter`).

```bash
git clone https://github.com/MohanTn/aicode-mcp-dotnet && cd aicode-mcp-dotnet
dotnet pack src/Aicode.Mcp.Cli -o nupkg
dotnet tool install --global --add-source nupkg Aicode.Mcp.Cli
aicode-mcp setup
```

Make sure `~/.dotnet/tools` is on your `PATH`. `aicode-mcp setup` configures every harness whose folder exists (`~/.claude`, `~/.copilot`, `~/.pi/agent`) and reports the ones it skipped:

| | Claude Code | Copilot CLI | Pi |
| --- | --- | --- | --- |
| `scaffold-mcp`, `architect-mcp` | `claude mcp add --scope user` | `~/.copilot/mcp-config.json` | `~/.pi/agent/mcp.json` |
| scaffold and architect hooks | `~/.claude/settings.json` | | |
| `aicode-init`, `repo-template` skills | `~/.claude/skills` | `~/.copilot/skills` | `~/.pi/agent/skills` |

Re-running `setup` refreshes everything without duplicates. Other servers, hooks and skills are left alone. `aicode-mcp setup --remove` takes out only what setup added.

To update: `git pull`, run `dotnet pack` again, then `dotnet tool uninstall --global Aicode.Mcp.Cli` and install it again, then `aicode-mcp setup`.

## Set up a repo

Open the repo in your agent and run the `aicode-init` command:

| Claude Code | Copilot CLI | Pi |
| --- | --- | --- |
| `/aicode-init` | ask it to use the `aicode-init` skill | `/skill:aicode-init` |

It picks `dotnet-api` or `react-web` from the stack, runs `aicode-mcp init`, and checks the scaffold tools respond. A repo that already has more than 10 source files is brownfield. The generic template would fight its architecture, so it offers `repo-template` instead (`/repo-template`, `/skill:repo-template`), which builds `templates/<name>/` from the patterns the repo already follows.

Restart the agent session after the first setup so it loads the MCP servers.

### By hand, without setup

`aicode-mcp init` in a repo writes `.toolkit/settings.yaml` and `.mcp.json`, copies the bundled templates into `templates/`, then prints the hooks to add to `.claude/settings.json`. Existing files are kept. This is enough for Claude Code in that one repo.

- `--template react-web` picks the template when the repo has none yet. The default is `dotnet-api`.
- `--quiet` is for when the servers are registered user-wide (setup or the dotfiles). It skips `.mcp.json` and the hook printout.

Each server sends MCP instructions at startup that describe its workflow, so agents know the tool order without extra prompting.

A project is tied to a template by `sourceTemplate` in its `project.json`, or in `.toolkit/settings.yaml` for single-project repos.

## Reviewer

Set an environment variable to a GitLab access token with the `api` scope. Pass its name to the server with `--gitlab-token-env <NAME>`, and optionally `--gitlab-url` for self-hosted GitLab. Check out the merge request branch, then ask the agent to call `review-merge-request`. It returns each diff with the rules for its template. The agent reviews, then calls `post-review-comments`.

## Hooks

- `aicode-mcp hook --type scaffold` (PreToolUse on Write): the first Write of a new file is denied once with the scaffold methods that fit. Repeating the Write goes through.
- `aicode-mcp hook --type architect` (PostToolUse on Edit, Write, MultiEdit): adds the matching RULES.yaml rules as context. It never blocks.

Both hooks fail open and always exit 0.

## Not built yet

stdio is the only transport. There is no LLM-backed review mode, no `write-to-file` tool, no scaffold markers, and GitHub support.
