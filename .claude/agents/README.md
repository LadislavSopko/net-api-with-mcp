# Claude Code Sub-Agents for C# Enterprise Development

This directory contains specialized sub-agents for comprehensive C# code analysis, documentation, and testing.

## Available Sub-Agents

### 1. code-reviewer-strict
Performs rigorous code review focusing on:
- Security vulnerabilities (SQL injection, XSS, etc.)
- Performance issues (N+1 queries, memory leaks)
- Architecture violations (SOLID, DDD)
- Missing tests and error handling

**Usage**: `"Review the OrderService class for security and performance issues"`

### 2. er-diagram-expert  
Generates comprehensive Entity Framework database diagrams:
- Complete Mermaid ER diagrams with all relationships
- Detailed entity specifications with constraints
- Relationship matrices and data dictionaries
- Both technical and business-friendly views

**Usage**: `"Generate ER diagram for the entire database schema"`

### 3. usecase-architect
Extracts and documents use cases from existing code:
- Analyzes controllers, services, and workflows
- Documents all flows (main, alternative, exception)
- Extracts business rules from validation logic
- Maps code to functional requirements

**Usage**: `"Document all use cases for the Order processing system"`

### 4. e2e-test-builder
Creates comprehensive E2E tests for documented use cases:
- Generates executable test code
- Covers all scenarios (happy path, edge cases, errors)
- Includes test data builders and verification helpers
- Handles concurrency and performance testing

**Usage**: `"Create E2E tests for UC-002 Order Approval"`

### 5. workflow-orchestrator
Orchestrates the complete analysis workflow:
1. Code review → 2. ER diagrams → 3. Use cases → 4. E2E tests
- Generates executive dashboard with findings
- Creates organized documentation structure
- Ensures proper sequencing of analysis phases

**Usage**: `"Perform complete analysis of the Orders module"`

## Installation

### Option 1: User-level (available in all projects)
Copy these files to: `~/.claude/agents/`

### Option 2: Project-level (this project only)  
Copy these files to: `.claude/agents/`

Note: Project-level agents take precedence over user-level agents with the same name.

## Workflow Example

For a complete system analysis:

```bash
# 1. Start with code review
"Use code-reviewer-strict to analyze the Services folder"

# 2. Generate database documentation
"Use er-diagram-expert to document all entities"

# 3. Extract use cases
"Use usecase-architect to document Order subsystem use cases"

# 4. Create tests
"Use e2e-test-builder to generate tests for all documented use cases"

# Or simply:
"Use workflow-orchestrator to analyze the entire Orders module"
```

## Output Structure

```
documentation/
├── analysis-dashboard.md      # Executive summary
├── code-review-report.md      # All issues found
├── database-schema.md         # ER diagrams and details
├── use-cases/                 # Individual use case docs
│   ├── UC-001-create-order.md
│   ├── UC-002-approve-order.md
│   └── ...
└── diagrams/                  # Visual diagrams
    ├── er-diagram.mermaid
    └── use-case-diagram.mermaid

tests/E2E/                     # Generated test files
├── UC_001_CreateOrder_E2E.cs
├── UC_002_ApproveOrder_E2E.cs
└── ...
```

## Customization

Each agent can be customized by editing the markdown files:
- Modify the system prompt to match your coding standards
- Add specific rules or patterns for your project
- Adjust output formats to your preferences
- Add or remove tools based on your setup

## Best Practices

1. **Run code review first** - Fix critical issues before documenting
2. **Keep agents focused** - Each agent has a specific purpose
3. **Use the orchestrator** - For complete analysis workflows
4. **Version control agents** - Track changes to your customizations
5. **Share with team** - Put in project repo for consistency

## Troubleshooting

If agents aren't being recognized:
1. Check file location (`.claude/agents/` or `~/.claude/agents/`)
2. Verify YAML frontmatter format is correct
3. Ensure unique agent names (no conflicts)
4. Use `/agents` command to see all available agents

## MCP Integration

These agents work with your VS MCP server tools:
- `mcp__vs-mcp__GetDocumentOutline` - For code structure analysis
- `mcp__vs-mcp__FindSymbolUsages` - For tracking dependencies
- `mcp__vs-mcp__ExecuteTest` - For running generated tests
- And more...

Make sure your MCP server is running for full functionality.



USABLE :
tools: Task, Bash, Glob, Grep, LS, ExitPlanMode, Read, Edit, MultiEdit, Write, NotebookRead, NotebookEdit, WebFetch, TodoWrite, WebSearch, mcp__zen__chat, mcp__zen__thinkdeep, mcp__zen__planner, mcp__zen__consensus, mcp__zen__codereview, mcp__zen__precommit, mcp__zen__debug, mcp__zen__secaudit, mcp__zen__docgen, mcp__zen__analyze, mcp__zen__refactor, mcp__zen__tracer, mcp__zen__testgen, mcp__zen__challenge, mcp__zen__listmodels, mcp__zen__version, mcp__brave-search__brave_web_search, mcp__brave-search__brave_local_search, mcp__sequential-thinking__sequentialthinking, mcp__context7__resolve-library-id, mcp__context7__get-library-docs, mcp__browser__puppeteer_navigate, mcp__browser__puppeteer_screenshot, mcp__browser__puppeteer_click, mcp__browser__puppeteer_fill, mcp__browser__puppeteer_select, mcp__browser__puppeteer_hover, mcp__browser__puppeteer_evaluate, ListMcpResourcesTool, ReadMcpResourceTool, mcp__playwright__browser_close, mcp__playwright__browser_resize, mcp__playwright__browser_console_messages, mcp__playwright__browser_handle_dialog, mcp__playwright__browser_evaluate, mcp__playwright__browser_file_upload, mcp__playwright__browser_install, mcp__playwright__browser_press_key, mcp__playwright__browser_type, mcp__playwright__browser_navigate, mcp__playwright__browser_navigate_back, mcp__playwright__browser_navigate_forward, mcp__playwright__browser_network_requests, mcp__playwright__browser_take_screenshot, mcp__playwright__browser_snapshot, mcp__playwright__browser_click, mcp__playwright__browser_drag, mcp__playwright__browser_hover, mcp__playwright__browser_select_option, mcp__playwright__browser_tab_list, mcp__playwright__browser_tab_new, mcp__playwright__browser_tab_select, mcp__playwright__browser_tab_close, mcp__playwright__browser_wait_for, mcp__vs-mcp__GetDocumentOutline, mcp__vs-mcp__FindSymbols, mcp__vs-mcp__GetSymbolAtLocation, mcp__vs-mcp__ExecuteTest, mcp__vs-mcp__FindSymbolDefinition, mcp__vs-mcp__ExecuteCommand, mcp__vs-mcp__GetProjectReferences, mcp__vs-mcp__GetMethodCalls, mcp__vs-mcp__CheckSelection, mcp__vs-mcp__FindSymbolUsages, mcp__vs-mcp__GetActiveFile, mcp__vs-mcp__ExecuteAsyncTest, mcp__vs-mcp__GetSelection, mcp__vs-mcp__GetSolutionTree, mcp__vs-mcp__GetInheritance, mcp__vs-mcp__TranslatePath, mcp__vs-mcp__GetMethodCallers, mcp__lmt__analyzeImage, mcp__cvm__load, mcp__cvm__loadFile, mcp__cvm__start, mcp__cvm__getTask, mcp__cvm__submitTask, mcp__cvm__status, mcp__cvm__list_executions, mcp__cvm__get_execution, mcp__cvm__set_current, mcp__cvm__delete_execution, mcp__cvm__list_programs, mcp__cvm__delete_program, mcp__cvm__restart