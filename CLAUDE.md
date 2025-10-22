# CLAUDE.md

This file provides guidance to Claude Code (claude.ai/code) when working with code in this repository.

## Project Mission

POC MCP mixed over .NET Api

# CODING & INTERACTION NOTES

## Collaboration Rules

When working with Claude Code on this project, follow these operational modes and context rules:

### Operational Modes

1. **PLAN Mode**
   - PLAN is "thinking" mode, where Claude discusses implementation details and plans 
   - Default starting mode for all interactions
   - Used for discussing implementation details without making code changes
   - Claude will print `# Mode: PLAN` at the beginning of each response
   - Outputs relevant portions of the plan based on current context level
   - If action is requested, Claude will remind you to approve the plan first

2. **ACT Mode**
   - Only activated when the user explicitly types `ACT`
   - Used for making actual code changes based on the approved plan
   - Claude will print `# Mode: ACT` at the beginning of each response
   - Automatically returns to PLAN mode after each response
   - Can be manually returned to PLAN mode by typing `PLAN`

## Memory Bank - Critical System

The Memory Bank is Claude's ONLY connection to the project between sessions. Without it, Claude starts completely fresh with zero knowledge of the project.

### How Memory Bank Works

1. **User triggers**: Type `mb`, `update memory bank`, or `check memory bank`
2. **Claude's process**:
   - FIRST: Reads `mem-bank-mbel5/README.md` to understand Memory Bank structure
   - THEN: Reads ALL Memory Bank files to understand current project state
   - FINALLY: Updates relevant files and returns to PLAN mode

### Important Rules

- Claude MUST read mem-bank-mbel5/README.md first, then ALL Memory Bank files at start of EVERY task
- Memory Bank is the single source of truth - overrides any other documentation
- See mem-bank-mbel5/README.md for complete Memory Bank documentation
