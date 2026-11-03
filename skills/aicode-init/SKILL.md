---
name: aicode-init
description: Set up the current repo for aicode-mcp so scaffold-mcp, architect-mcp and the Claude Code hooks apply the templated coding standard (dotnet-api or react-web). Picks the template from the repo's stack, runs `aicode-mcp init`, and hands brownfield repos with existing code to the repo-template skill instead. Use when the user asks to set up, initialise, or enable aicode-mcp, scaffold-mcp, architect-mcp, or the coding standard templates in a repo.
---

# aicode-init

Ties the session's repo to an aicode-mcp template. The target is the git root of the session cwd unless the user names another folder.

## 1. Check the tool

Run `aicode-mcp --help`. If the command is missing, stop and point the user to the install steps in the aicode-mcp-dotnet README (`dotnet tool install`, then `aicode-mcp setup`).

## 2. Pick the template

- `.toolkit/settings.yaml` or a `project.json` with `sourceTemplate` already exists: the repo is set up. Skip to step 4 without `--template`.
- The repo tracks a `.csproj`, `.sln` or `.slnx`: `dotnet-api`.
- The root `package.json` depends on `react`: `react-web`.
- Both, or neither: ask the user which one, or whether to stop.

## 3. Greenfield or brownfield

Count tracked source files: `git ls-files '*.cs' '*.ts' '*.tsx' '*.js' '*.jsx' | wc -l`.

More than 10 means the repo already has its own architecture. The generic template would fight it. Tell the user and offer two choices:
- build a template from the repo's own code with the `repo-template` skill (recommended), then stop here
- use the generic template anyway, then continue

## 4. Run init

From the repo root:
- `aicode-mcp init --quiet --template <name>` when the servers are registered user-wide (the user ran `aicode-mcp setup`, or the mohan-dotfiles installed them). `--quiet` skips `.mcp.json`.
- `aicode-mcp init --template <name>` otherwise. It also writes `.mcp.json` and prints the hooks to merge into `.claude/settings.json`. Merge them for the user if they agree.

Drop `--template` when step 2 found an existing template. init never overwrites existing files.

## 5. Verify

Call the `list-scaffolding-methods` tool. It must list the template's methods. MCP servers load at session start, so if the tools are missing, tell the user to restart the session and check again. Report what init created and the scaffold methods now available.
