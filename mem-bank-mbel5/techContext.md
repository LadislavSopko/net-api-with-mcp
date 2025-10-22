§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[STACK]
@runtime::.NET§9.0.300
@framework::AspNetCore§9.0
@protocol::MCP§0.4.0-preview.3
@logging::Serilog

[KEY_FILES]
src/McpPoc.Api/
├─Extensions/McpServerBuilderExtensions.cs{solution:170lines}
├─Program.cs{.WithToolsFromAssemblyUnwrappingActionResult()}
├─Controllers/UsersController.cs{[McpServerToolType]}
└─Services/{IUserService+UserService}

tests/McpPoc.Api.Tests/
├─McpToolDiscoveryTests.cs{5/5✓}
├─McpToolInvocationTests.cs{5/5✓}
├─HttpCoexistenceTests.cs{3/3✓}
└─ActionResultSerializationTest.cs{2/2✓}

[SOLUTION]
WithToolsFromAssemblyUnwrappingActionResult::{
  scan::[McpServerToolType]
  create::AIFunction{AIFunctionFactoryOptions}
  MarshalResult::UnwrapActionResult
  register::McpServerTool
}

UnwrapActionResult::{
  ActionResult<T>→Result→IActionResult→Value
  ValueTask.FromResult(unwrapped)
}

[TOOLS_LIVE]
✓get_all::mcp__poc__get_all()
✓get_by_id::mcp__poc__get_by_id(id)
✓create::mcp__poc__create(name,email)

[CRITICAL_PATTERN]
!new-ValueTask(result)→null{broken}
!ValueTask.FromResult(result)→value{works}
