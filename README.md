# aicode-mcp

**aicode-mcp** helps AI coding assistants write code that follows your team's standards for .NET and React projects.

It adds two helpers (called "MCP servers") to your AI assistant:

- **scaffold-mcp** creates new files from ready-made templates, so new code starts in the right shape.
- **architect-mcp** tells the assistant which coding rules apply to the file it is working on.

It works with **Claude Code**, **GitHub Copilot CLI**, **GitHub Copilot Chat in VS Code**, and **Pi**.

---

## Before you start

You need two free programs. Install them once.

1. **Git**: download from <https://git-scm.com/downloads> and install with the default options.
2. **.NET 10 SDK**: download from <https://dotnet.microsoft.com/download> and pick **.NET 10 SDK** for your computer.

To check they are installed, open a terminal and type each line, then press Enter:

```bash
git --version
dotnet --version
```

Each one should print a version number. If you see "command not found", restart your computer and try again.

> **How to open a terminal**
> - **Windows:** press the Start key, type `PowerShell`, press Enter.
> - **Mac:** press Cmd + Space, type `Terminal`, press Enter.
> - **Linux:** press Ctrl + Alt + T.

You also need at least one AI assistant installed (Claude Code, Copilot CLI, VS Code with Copilot Chat, or Pi). Open it at least once so it creates its settings folder.

---

## Step 1: Install aicode-mcp

Copy these lines into your terminal, one at a time, pressing Enter after each:

```bash
git clone https://github.com/MohanTn/aicode-mcp-dotnet
cd aicode-mcp-dotnet
dotnet pack src/Aicode.Mcp.Cli -o nupkg
dotnet tool install --global --add-source nupkg Aicode.Mcp.Cli
```

Now check it worked:

```bash
aicode-mcp --help
```

You should see a list of commands.

**If you see "command not found":** close the terminal and open a new one. If it still fails, add the .NET tools folder to your PATH:

- **Mac or Linux:** run `echo 'export PATH="$PATH:$HOME/.dotnet/tools"' >> ~/.zshrc` (or `~/.bashrc` if you use bash), then open a new terminal.
- **Windows:** the folder is `%USERPROFILE%\.dotnet\tools`. It is normally added for you. Sign out and back in if the command is still missing.

---

## Step 2: Connect it to your AI assistant

Pick the section for the assistant you use. You can do more than one.

### Claude Code, Copilot CLI and Pi (automatic)

Run this one command:

```bash
aicode-mcp setup
```

It finds each assistant installed on your computer and connects aicode-mcp to it. It prints a line for each one, for example:

```
aicode-mcp: set up for claude
aicode-mcp: set up for copilot
aicode-mcp: skipped pi, /home/you/.pi/agent not found
```

"Skipped" just means that assistant is not installed, or you have not opened it yet. Open it once, then run `aicode-mcp setup` again.

**Pi only:** Pi needs an extra add-on that understands MCP, for example `pi-mcp-adapter`. Install that first.

Running `setup` again is always safe. It never duplicates anything and never touches your other settings.

### GitHub Copilot Chat in VS Code (manual)

`setup` does not change VS Code, so add the servers by hand once:

1. Open VS Code.
2. Press **Ctrl + Shift + P** (Mac: **Cmd + Shift + P**) and choose **MCP: Open User Configuration**.
3. Replace the file's contents with the text below (or, if it already has a `"servers"` section, add the two entries inside it):

   ```json
   {
     "servers": {
       "scaffold-mcp": {
         "type": "stdio",
         "command": "aicode-mcp",
         "args": ["mcp-serve", "--server", "scaffold"]
       },
       "architect-mcp": {
         "type": "stdio",
         "command": "aicode-mcp",
         "args": ["mcp-serve", "--server", "architect"]
       }
     }
   }
   ```

4. Save the file.
5. Open Copilot Chat, switch to **Agent** mode, and click the tools icon. You should see `scaffold-mcp` and `architect-mcp` in the list.

---

## Step 3: Set up your project

Do this once for each project (repository) you want to use it in.

1. Open your project in your AI assistant.
2. Start the setup:

   | Assistant | What to type |
   | --- | --- |
   | Claude Code | `/aicode-init` |
   | Copilot CLI | `use the aicode-init skill` |
   | Pi | `/skill:aicode-init` |
   | VS Code Copilot Chat | open the VS Code terminal in your project and run `aicode-mcp init --quiet --template dotnet-api` (or `react-web` for a React project) |

3. **Close the assistant and open it again.** It only loads new helpers when it starts.

The assistant picks the right template for your project:

- **dotnet-api** for C# and .NET projects
- **react-web** for React projects

**If your project already has a lot of code**, the assistant will suggest building a template from your own code instead of the generic one. That is usually the better choice, so the rules match how your team already works. Say yes and it uses the `repo-template` skill (`/repo-template` in Claude Code, `/skill:repo-template` in Pi).

---

## Step 4: Check it works

Ask your assistant:

> What scaffolding methods are available?

It should list options such as creating a new controller, service or component. If it says it has no such tool, close and reopen the assistant, then ask again.

From now on, just ask for things normally, for example "add an endpoint for orders". The assistant uses the templates and rules on its own.

---

## Updating

Open a terminal in the `aicode-mcp-dotnet` folder and run:

```bash
git pull
dotnet pack src/Aicode.Mcp.Cli -o nupkg
dotnet tool uninstall --global Aicode.Mcp.Cli
dotnet tool install --global --add-source nupkg Aicode.Mcp.Cli
aicode-mcp setup
```

## Uninstalling

```bash
aicode-mcp setup --remove
dotnet tool uninstall --global Aicode.Mcp.Cli
```

`setup --remove` takes out only what aicode-mcp added. For VS Code, delete the two entries you added in Step 2.

## Common problems

| Problem | Fix |
| --- | --- |
| `aicode-mcp: command not found` | Open a new terminal. See the PATH note in Step 1. |
| `dotnet: command not found` | Install the .NET 10 SDK, then restart your computer. |
| Assistant can't see the tools | Close and reopen the assistant. Check Step 2 printed "set up for" your assistant. |
| `setup` says "skipped" | Open that assistant once so it creates its settings folder, then run `aicode-mcp setup` again. |
| `claude CLI failed or not found` | Run the `claude mcp add ...` command that `setup` printed. |

## Using mohan-dotfiles?

If you use [mohan-dotfiles](https://github.com/MohanTn/mohan-dotfiles), you can skip Steps 1 and 2. Answer yes to "9b. Set up the aicode-mcp MCP servers" in `./setup-packages.sh`, then run `./setup.sh`. It installs, updates and connects everything for Claude Code, Copilot CLI and Pi, and sets up .NET and React projects automatically.

---

## For developers

<details>
<summary>Tools, layout, tests, hooks and the GitLab reviewer</summary>

### Servers and tools

| Server | Tools |
| --- | --- |
| `scaffold` | `list-boilerplates`, `use-boilerplate`, `list-scaffolding-methods`, `use-scaffold-method` |
| `architect` | `get-file-design-pattern`, `review-code-change` |
| `reviewer` | `get-merge-request`, `review-merge-request`, `post-review-comments` |

This project ports the scaffold and architect ideas from aicode-toolkit to C# and adds a GitLab merge request reviewer.

### Layout

- `src/Aicode.Mcp.Core`: YAML models, glob matching, project and rule lookup, scaffold rendering (Liquid via Fluid), hooks, GitLab client
- `src/Aicode.Mcp.Servers`: MCP tool classes
- `src/Aicode.Mcp.Cli`: the `aicode-mcp` tool (`mcp-serve`, `hook`, `init`, `setup`)
- `templates/dotnet-api`, `templates/react-web`: `scaffold.yaml`, `architect.yaml`, `RULES.yaml` and template files
- `skills/aicode-init`, `skills/repo-template`: agent skills that `setup` installs for Claude Code, Copilot CLI and Pi
- `tests/Aicode.Mcp.Tests`: xUnit tests. They copy the real `templates/` folder into a temp workspace

### Build and test

```bash
dotnet test aicode-mcp-dotnet.slnx
```

`McpProtocolTests` starts the real `aicode-mcp` process for each server and calls all 9 tools over stdio. The reviewer talks to a fake GitLab served from the test. `ToolCatalogTests` fails if a tool is added or renamed without updating the suite.

### What setup writes

| | Claude Code | Copilot CLI | Pi |
| --- | --- | --- | --- |
| `scaffold-mcp`, `architect-mcp` | `claude mcp add --scope user` | `~/.copilot/mcp-config.json` | `~/.pi/agent/mcp.json` |
| scaffold and architect hooks | `~/.claude/settings.json` | | |
| `aicode-init`, `repo-template` skills | `~/.claude/skills` | `~/.copilot/skills` | `~/.pi/agent/skills` |

### init by hand

`aicode-mcp init` in a repo writes `.toolkit/settings.yaml` and `.mcp.json`, copies the bundled templates into `templates/`, then prints the hooks to add to `.claude/settings.json`. Existing files are kept. This is enough for Claude Code in that one repo.

- `--template react-web` picks the template when the repo has none yet. The default is `dotnet-api`.
- `--quiet` is for when the servers are registered user-wide. It skips `.mcp.json` and the hook printout.

A project is tied to a template by `sourceTemplate` in its `project.json`, or in `.toolkit/settings.yaml` for single-project repos. Each server sends MCP instructions at startup that describe its workflow.

### Reviewer

Set an environment variable to a GitLab access token with the `api` scope. Pass its name to the server with `--gitlab-token-env <NAME>`, and optionally `--gitlab-url` for self-hosted GitLab. Check out the merge request branch, then ask the agent to call `review-merge-request`. It returns each diff with the rules for its template. The agent reviews, then calls `post-review-comments`.

### Hooks

- `aicode-mcp hook --type scaffold` (PreToolUse on Write): the first Write of a new file is denied once with the scaffold methods that fit. Repeating the Write goes through.
- `aicode-mcp hook --type architect` (PostToolUse on Edit, Write, MultiEdit): adds the matching RULES.yaml rules as context. It never blocks.

Both hooks fail open and always exit 0.

### Not built yet

stdio is the only transport. There is no LLM-backed review mode, no `write-to-file` tool, no scaffold markers, and no GitHub support.

</details>
