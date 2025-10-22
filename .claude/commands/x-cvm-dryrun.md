@include shared/constants.yml#Process_Symbols

@include shared/command-templates.yml#Command_Header

CVM program dry-run verification for completeness, references, and branching logic.

@include shared/command-templates.yml#Universal_Flags
@see shared/thinking-modes.yml ∀ thinking flags

Examples:
- `/cvm-dryrun program.ts` - Basic dry-run verification
- `/cvm-dryrun acl-refactor-program.ts --detailed` - Detailed analysis
- `/cvm-dryrun /path/to/program.ts --think` - With deep analysis

## Usage Pattern:
```
/cvm-dryrun <program-file> [options]
```

## Verification Checklist:

### 1. Prompt Completeness ✓
- **Context prompts:** Verify all CC() calls have complete context
- **Variable references:** Check all prompt variables are defined
- **Instruction clarity:** Ensure prompts have clear success criteria
- **Error guidance:** Verify prompts include error handling instructions
- **Submit criteria:** Check all prompts end with clear submit instructions

### 2. Plan References 📍
- **Line numbers:** Verify all planReference include specific line numbers
- **File paths:** Check plan file paths are correct and accessible
- **Code snippets:** Ensure codeSnippets reference correct line ranges
- **Back-references:** Verify each TDDAB maps to plan sections
- **Completeness:** Check all plan TDDABs are represented

### 3. Program Branching 🔀
- **Test loops:** Verify while loops have exit conditions
- **Max attempts:** Check infinite loop prevention (maxAttempts)
- **Error branches:** Ensure failed test handling exists
- **Debug modes:** Verify debug prompt triggers on max failures
- **Completion paths:** Check all paths lead to completion

## Verification Process:

1. **Load program in CVM** using mcp__cvm__loadFile
2. **Start execution** with mcp__cvm__start
3. **Execute dry-run**:
   - For each CC() call, use mcp__cvm__getTask
   - Analyze the prompt (completeness, references)
   - Respond strategically:
     - Implementation tasks: "done"
     - Test tasks (first time): "failed" (to test retry logic)
     - Test tasks (retry): "passed" (to continue)
     - Commit tasks: "done"
   - Track execution path and iterations
4. **Monitor program flow** through all branches
5. **Generate comprehensive report**

## Dry-Run Execution Strategy:

The dry-run simulates real execution by providing reasonable responses:
- **No actual file operations** - just respond to prompts
- **Test branching paths** - fail first, then pass to see retry logic
- **Track all iterations** - count how many times loops execute
- **Verify completion** - ensure program reaches the end
- **Detect infinite loops** - check if maxAttempts prevents runaway

Example flow:
1. TDDAB-1: CC("Implement...") → respond "done"
2. TDDAB-2: CC("Test...") → respond "failed" (attempt 1)
3. TDDAB-2: CC("Fix and test...") → respond "passed" (attempt 2)
4. TDDAB-2: CC("Commit...") → respond "done"
5. Continue through all TDDABs...

## Report Format:

```
=== CVM Program Dry-Run Report ===

Program: <filename>
Total CC calls: <count>
Plan references: <count>
Branching points: <count>

✓ Prompt Completeness:
  - Context variables: <status>
  - Submit instructions: <status>
  - Error handling: <status>

✓ Plan References:
  - Line numbers: <found/missing>
  - File paths: <valid/invalid>
  - Coverage: <percentage>

✓ Branching Logic:
  - Test loops: <safe/risky>
  - Max attempts: <defined/missing>
  - Debug fallback: <exists/missing>

⚠️ Issues Found:
  - <list of issues>

✅ Ready for execution: <yes/no>
```

## Options:

**--detailed:** Show full analysis of each CC prompt
**--fix:** Suggest fixes for identified issues
**--compare:** Compare with plan to ensure alignment
**--simulate:** Show execution flow without running

@include shared/command-templates.yml#Research_Requirements

Output: Dry-run verification report with actionable findings