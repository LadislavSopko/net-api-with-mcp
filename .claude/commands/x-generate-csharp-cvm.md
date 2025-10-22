Generate CVM (Claude Virtual Machine) program from TDDAB plan following strict patterns.

Examples:
- `/generate-cvm /tasks/plan.md` - Generate CVM program from plan
- `/generate-cvm /tasks/plan.md --pattern /tasks/example_program.ts` - Use specific pattern

## MANDATORY RULES - NO EXCEPTIONS

### 1. Pattern Following - THIS IS NON-NEGOTIABLE
- **ONLY follow example program structure (if not present --pattern argument)** from <root>/tasks/example_program.ts
- **FOLLOW the pattern exactly** - naturally with correct context from plan
- **NEVER invent new structures** - use what works
- Always include:
  - Same console.log format
  - Same contextPrompt structure
  - Same fileOpsBase + submitDone pattern
  - Same test loop with no escape
  - Submit info must be one word as result done|failed|success|....

### 2. Plan Back-References - SOURCE OF TRUTH
- **Every TDDAB MUST reference source plan** with exact line numbers
- Format: `planReference: "/path/to/plan.md lines X-Y"`
- **NO EXCEPTIONS** - every task needs traceability
- Example:
  ```typescript
  {
      name: "TDDAB-1: Fix Authorization",
      planReference: "/tasks/critical-fixes-plan.md lines 45-89",
      description: "Add RequireSystemAdmin attribute"
  }
  ```

### 3. Self-Sufficient Prompts - FRESH CONTEXT READY
- **Each CC() call must work independently** - no memory between calls
- Include full context in every prompt:
  - Project details
  - Technology stack
  - Specific task
  - Tool instructions
  - Expected result format

### 4. Simple Result Words - AUTOMATION READY
- **ALWAYS specify exact words to submit**:
  - "done" - task completed
  - "passed" - tests pass
  - "failed" - tests fail
  - "fixed" - issue resolved
  - "analyzed" - dry-run analysis
- Add to every prompt: `" Submit 'done' when complete."`
- **CRITICAL**: Include in contextPrompt: `"When submitting use only one word done|failed|passed|..."`

### 5. Atomic Commits - CLEAN HISTORY
- **After each successful TDDAB**: git add and commit
- **Technical commit messages only** - no emojis, no attributions
- Format: `"feat(area): description"` or `"fix(area): description"`

## C# Project Rules - CRITICAL

### NEVER This:
```bash
dotnet build  # WRONG!
dotnet test   # WRONG!
npm test      # WRONG!
```

### ALWAYS This in Generated Prompts:
```typescript
"Use mcp__vs-mcp__ExecuteCommand command='build' outputFormat='compact'"
"Use mcp__vs-mcp__ExecuteAsyncTest operation='start' projectName='X'"
"NEVER use sync test execution - ONLY ExecuteAsyncTest"
```

## Program Structure Template

When generating, follow this exact pattern:

```typescript
function main() {
    console.log("=== [Project] [Feature] Implementation ===");
    
    var contextPrompt = "[Extract project context from plan] " +
        "When submitting use only one word done|failed|passed|fixed|analyzed.";
    
    var fileOpsBase = contextPrompt + " Use Read, Write, Edit tools. ";
    var submitDone = " Submit 'done' when complete.";
    var submitTest = " Submit 'passed' if tests pass, 'failed' if tests fail.";
    
    var tddBlocks = [
        // Extract from plan with MANDATORY planReference
    ];
    
    var blockIndex = 0;
    while (blockIndex < tddBlocks.length) {
        var block = tddBlocks[blockIndex];
        
        // Implementation
        CC(fileOpsBase + "Implement " + block.name + 
           " as specified in " + block.planReference + submitDone);
        
        // Test loop - NO ESCAPE
        var testResult = CC(fileOpsBase + "Test " + block.name + submitTest);
        while (testResult === "failed") {
            CC(fileOpsBase + "Fix failing tests" + submitDone);
            testResult = CC(fileOpsBase + "Test again" + submitTest);
        }
        
        // Commit
        CC(fileOpsBase + "Commit changes" + submitDone);
        
        blockIndex = blockIndex + 1;
    }
}
```


## Active Mode Behaviors

When this command is invoked, I will:
- **READ** the specified plan file completely
- **EXTRACT** all TDDAB blocks with line numbers
- **FOLLOW** the example program pattern exactly
- **GENERATE** complete CVM program with all 5 rules
- **SAVE** as `[plan-basename]-cvm-program.ts`
- **VERIFY** all planReference fields are complete

**MY GOLDEN RULE: Copy the example_program.ts pattern exactly - don't innovate!**