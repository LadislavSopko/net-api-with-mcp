# AGENTS.md

This file provides guidance to OpenAI coding agents working in this repository via the Codex CLI.

## Project Mission

POC MCP mixed over .NET Api


## Codex CLI Operating Guidelines

- Run shell commands through the CLI harness: wrap commands with `bash -lc`, always set the `workdir`, and avoid `cd` in commands when possible.
- Default to ASCII when editing files; keep existing encodings intact.
- Prefer `rg`/`rg --files` for searches. If unavailable, fall back to other tools.
- Respect any existing uncommitted changes made by the user; never revert work you did not author.
- Follow the editing philosophy: add comments only where they clarify complex logic.

## Planning Discipline

- Use the plan tool when tasks are non-trivial; avoid plans for the simplest ~25% of requests.
- Plans must have two or more steps, with at most one step marked `in_progress` at any time.
- After completing a planned step, update the plan before moving to the next action.
- Skip the plan tool for straightforward single-file edits or quick reads.

## Execution & Safety

- Workspace is Linux (WSL) with Windows-specific binaries/projects; some builds or tests may not run. Document any skipped validations.
- Network access is restricted; do not attempt outbound connections.
- Treat destructive operations (`rm`, `git reset`, etc.) as last resort and only when explicitly requested.
- NEVER run `git` commands (repository resides on Windows; git locks the repo).
- When running tests or scripts that may be long-running or Windows-only, note limitations and suggest alternatives instead of forcing execution.

## Collaboration With Claude Code

- Claude operates with explicit PLAN/ACT modes. Wait for a PLAN to be approved before acting when collaborating in shared sessions.
- Share relevant context from your work with Claude via the memory bank or conversation notes so both agents remain aligned.
- When Claude is in ACT mode, avoid simultaneous edits to the same files unless coordinated.

## Memory Bank Usage

The `mem-bank-mbel5/README.md` directory stores the canonical project context. Always refresh your understanding before significant work and update files when progress or decisions change.

## Communication Style

- Be concise, collaborative, and factual. Summaries should lead with outcomes, followed by supporting detail.
- Reference files with clickable paths (e.g., `src/module/File.cs:42`). Avoid large dumps of file contents in responses.
- Suggest logical next steps (tests, builds, commits) when appropriate, especially after code changes.

## Mindset Commands

- When the user issues `x-csharp-tddab`, treat it as a mindset setup command.
- Perform the required context reads from `.claude/commands/x-csharp-tddab.md`, `.claude/commands/mind-sets/csharp-senior.md`, `.claude/commands/mind-sets/tddab-planner.md`, and the Memory Bank (skip `progress.md` and `docs/` unless explicitly asked) before proceeding.
- Adopt the rules in those mindset files: prefer VS MCP tooling for .NET work, enforce TDDAB planning discipline, and follow the user's .NET coding standards.
- Confirm the mindset activation in your reply so the user knows you are operating under the requested constraints.
- Mindsets apply for the current session only; wait for the user to reissue commands if a reset occurs.

Following these guidelines ensures smooth collaboration between Codex, Claude, and project maintainers while preserving repository integrity.
