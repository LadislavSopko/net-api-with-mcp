§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[FOCUS]
@task::POCComplete!
@phase::Finalized{implementation✓→testing✓→solution✓}
⚡achievement::ActionResultUnwrapping{custom-marshaller}

[RECENT]
>debugged::MarshallerPipeline{ReflectionAIFunction→ReturnParameterMarshaller}
>discovered::RootCause{ValueTask<object?>:loses-value}
>created::CustomExtension{WithToolsFromAssemblyUnwrappingActionResult}
>implemented::ActionResultUnwrapper{MarshalResult:before-serialization}
>achieved::AllTestsPass{15/15:100%}✓

[NEXT]
?cleanup::ObsoleteFilter{Filters/ActionResultUnwrapperFilter.cs}
?document::Solution{MB+commit}
?optional::PullRequest{if-desired}

[DECISIONS]
@approach::CustomMarshalResult{¬filter:too-late}
@interceptPoint::AIFunctionFactory{MarshalResult:before-serialize}
@naming::snake_case{explicit:ConvertToSnakeCase}
@pattern::ManualToolRegistration{¬WithToolsFromAssembly}

[LEARNINGS]
@critical::MarshallerProblem{ValueTask-construction:loses-value}!
@critical::ValueTaskFromResult{proper-construction:preserves-value}!
@critical::FiltersUseless{post-marshalling→too-late}!
@sdk::AIFunctionFactoryOptions{MarshalResult:custom-delegate}
@sdk::ValueTaskConstruction{FromResult:¬new-ValueTask}!
@pattern::ExtensionMethod{replicates:WithToolsFromAssembly}
@naming::ToolNames{Name:property¬SerializerOptions}
@combo::MarshallerFix+Unwrapping{two-problems:one-solution}

[SOLUTION_ARCHITECTURE]
WithToolsFromAssemblyUnwrappingActionResult::
  scan::Assembly{[McpServerToolType]}
  → foreach::Method{[McpServerTool]}
    → create::AIFunction{AIFunctionFactoryOptions}
      → MarshalResult::UnwrapActionResult{fix-marshaller+unwrap}
      → Name::ConvertToSnakeCase
      → SerializerOptions::SnakeCaseLower
    → register::McpServerTool

UnwrapActionResult{two-fixes-in-one}::
  1.fix::ValueTask{FromResult:¬new-ValueTask}→preserves-value
  2.unwrap::ActionResult<T>→Result→IActionResult→Value
  return::ValueTask.FromResult(unwrapped-value)

[BLOCKERS]
¬none::AllClear

[CONFIDENCE]
@solution::100%{proven:15/15-tests}
@approach::100%{correct-intercept-point}
@poc::100%{hypothesis-fully-proven}
