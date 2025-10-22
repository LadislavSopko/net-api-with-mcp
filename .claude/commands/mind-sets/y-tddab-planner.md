# Agent-Optimized TDDAB Planner Mindset

## 🎯 AGENT-OPTIMIZED TDDAB PRINCIPLES - ABSOLUTE RULES

### What is Agent-Optimized TDDAB?
**Test Driven Development Atomic Block with Context-Efficient Execution** - Each block is:
- **Test-First**: Write FAILING tests before implementation
- **Atomic**: Complete, self-contained, independently deployable
- **Block**: Cohesive unit of functionality
- **Context-Optimized**: Uses agents for 90-95% context savings

### The Three Sacred Phases (Agent-Enhanced)
```
1. RED Phase    → Write tests that FAIL
2. GREEN Phase  → Write code to make tests PASS
3. VERIFY Phase → Use agents for compressed verification feedback
```

---

## 🚨 AGENT USAGE - ABSOLUTE REQUIREMENTS

### VERIFICATION Section - ALWAYS Use Agents:
```bash
# ✅ CORRECT - Context-optimized verification
Use build-agent to build [ProjectName]
→ Returns: ✅ CLEAN or compressed error list

Use test-agent to run tests for [TestClass]
→ Returns: ✅ ALL PASS or compressed failure list with actionable fixes

# ❌ WRONG - Direct tool usage (context killers!)
mcp__vs-mcp__ExecuteCommand       // NEVER!
mcp__vs-mcp__ExecuteAsyncTest     // NEVER!
```

### Agent Selection for TDDAB:
| TDDAB Phase | Agent | Output |
|-------------|-------|---------|
| RED (Test) | *None needed* | Write failing tests |
| GREEN (Code) | *None needed* | Write implementation |
| VERIFY (Build) | build-agent | ✅ CLEAN or errors |
| VERIFY (Test) | test-agent | ✅ PASS or failures |

---

## 🚫 WHAT AN AGENT-OPTIMIZED TDDAB PLAN IS NOT

### ❌ NEVER Include:
- Options or alternatives ("Should we A or B?")
- Decisions to be made later
- Discussion or analysis
- "Investigation needed" sections
- "Consider using..." phrases
- Multiple approaches
- **Direct vs-mcp build/test tool calls** (context killers!)

---

## ✅ WHAT AN AGENT-OPTIMIZED TDDAB PLAN MUST BE

### Every TDDAB Block Contains:

#### 1. TEST FIRST Section
```csharp
// ALWAYS start with tests that will FAIL
[Fact]
public void Should_DoExpectedBehavior_WhenCondition()
{
    // Arrange
    var input = CreateTestData();

    // Act
    var result = SystemUnderTest.Execute(input);

    // Assert
    result.Should().BeExpected();
}
```

#### 2. IMPLEMENTATION Section
```csharp
// Then show EXACT code to make tests pass
public class Implementation
{
    // Precise, executable code
    // No pseudo-code
    // No "..." or "TODO"
}
```

#### 3. VERIFICATION Section (Agent-Optimized)
```bash
# Context-optimized verification with agents
Use build-agent to build [ProjectName] and verify compilation
Use test-agent to run tests for [TestClass] and verify implementation

→ Expected Results:
  Build: ✅ CLEAN (0 errors, 0 warnings)
  Tests: ✅ ALL PASS (X tests passed)
```

---

## 📋 AGENT-OPTIMIZED TDDAB Structure Template

```markdown
## TDDAB-N: [Feature Name]

### N.1 Tests First (These will FAIL initially)

**Create/Update:** `/full/absolute/path/to/test/file.cs`
```csharp
using All.Required.Namespaces;  // COMPLETE imports
using Xunit;
using FluentAssertions;

namespace Full.Namespace.Path
{
    public class CompleteTestClass  // COMPLETE class
    {
        [Fact]
        public void Complete_Test_Method()  // COMPLETE method
        {
            // Full test implementation
            // No "..." or "rest of code"
        }
    }
}
```

### N.2 Implementation (Make tests PASS)

**Step 1 - [Specific action]:**
`/full/absolute/path/to/implementation/file.cs`
```csharp
using All.Required.Namespaces;  // COMPLETE imports

namespace Full.Namespace.Path
{
    public class CompleteImplementation  // COMPLETE class
    {
        // Complete implementation
        // No snippets or fragments
    }
}
```

### N.3 Verification (Agent-Optimized)
```bash
# Context-efficient verification using agents
Use build-agent to build [ExactProjectName]
→ Expected: ✅ CLEAN (0 errors, 0 warnings)

Use test-agent to run tests for [ExactTestClassName]
→ Expected: ✅ ALL PASS ([X] tests passed)
```
```

---

## 🏗️ AGENT-OPTIMIZED TDDAB Planning Rules

### 1. Information Self-Sufficiency (CRITICAL!)
**Each TDDAB must be executable with ZERO context:**
- Include COMPLETE code, not snippets
- Show FULL file paths, not relative references
- Include ALL necessary imports/usings
- Specify EXACT package versions
- Include COMPLETE configuration examples
- Never reference "previous discussion" or "as we decided"
- Never use "..." or "rest remains the same"

### 2. Agent-First Verification
**MANDATORY: All verification must use agents:**
- Build verification → build-agent ONLY
- Test verification → test-agent ONLY
- NEVER use direct vs-mcp tools in plans
- NEVER include raw tool output in plans

### 3. Context Efficiency Targets
**Each TDDAB block must achieve:**
- Build feedback: 5-15 lines (vs 200-500 raw)
- Test feedback: 8-25 lines (vs 500-2000 raw)
- Total verification: <40 lines (vs 1000-3000 raw)
- Context savings: 90-95% minimum

---

## 🎯 AGENT-OPTIMIZED WORKFLOWS

### Traditional TDDAB Problems:
- Build output: 200-500 lines of MSBuild noise
- Test output: 500-2000 lines with stack traces
- Context consumption: 30-60% per verification
- Slow feedback: Parse noise manually

### Agent-Optimized TDDAB Benefits:
- Build feedback: ✅ CLEAN or compressed error list
- Test feedback: ✅ ALL PASS or compressed failure list
- Context consumption: <2% per verification
- Fast feedback: Actionable intelligence only

### Verification Workflow:
```
Traditional: Write code → Run build → Parse 500 lines → Find 2 errors
Agent-Optimized: Write code → Use build-agent → Get "2 errors: File.cs:45, File.cs:67"

Traditional: Run tests → Parse 2000 lines → Find 1 failing assertion
Agent-Optimized: Use test-agent → Get "1 failure: TestClass.Method - Expected X, got Y"
```

---

## 📊 TDDAB + Agent Success Metrics

### Context Efficiency:
- **Build operations**: 90% token reduction
- **Test operations**: 95% token reduction
- **Overall TDDAB cycle**: 90-95% faster feedback

### Development Speed:
- **Feedback loops**: 5-10x faster
- **Error identification**: Instant vs manual parsing
- **Development flow**: Uninterrupted by noise

### Quality Maintenance:
- **All C# senior standards**: Preserved
- **TDDAB methodology**: Enhanced with efficiency
- **Professional patterns**: Maintained and improved

---

## 💎 AGENT-OPTIMIZED GOLDEN RULES

1. **Agent-First: All build/test operations must use agents**
2. **Context is Sacred: Every token saved enables better development**
3. **If you write direct vs-mcp calls in a plan - STOP, use agents**
4. **If verification isn't compressed - STOP, use agents**
5. **If it's not atomic - STOP, split or merge blocks**
6. **Signal over Noise: Agents provide signal, direct tools provide noise**

---

## 🚀 ACTIVATION TRIGGER

When user requests Agent-Optimized TDDAB planning:
1. Ensure all decisions are made first
2. Create only executable, atomic blocks
3. Tests ALWAYS come first
4. **Verification ALWAYS uses agents**
5. No options, no discussions, no investigations
6. Complete, deployable code only
7. **90-95% context efficiency minimum**

**Remember: An Agent-Optimized TDDAB plan is a RECIPE with COMPRESSED FEEDBACK, not a DISCUSSION with NOISE!**