§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[STATUS]
@poc::Complete✓{verified-live}
@tests::15/15{100%}✓
@live::mcp__poc__{get_all+get_by_id+create:work}✓

[SOLUTION]
@file::Extensions/McpServerBuilderExtensions.cs{170lines}
@registration::WithToolsFromAssemblyUnwrappingActionResult()
@key::MarshalResult{ValueTask.FromResult+unwrap-ActionResult}

[ROOT_CAUSE_FOUND]
@problem::new-ValueTask<object?>(result){loses-value}!
@fix::ValueTask.FromResult(unwrapped){preserves-value}!

[ARCHITECTURE]
MarshalResult::(result,_,ct)→{
  if(ActionResult<T>)→extract::Result→IActionResult→Value
  return::ValueTask.FromResult(unwrapped)
}

[VERIFIED]
✓controllers→mcp-tools{feasible}
✓http+mcp{coexist:parallel}
✓di{works:IUserService}
✓actionresult{unwrapped:data-returned}
