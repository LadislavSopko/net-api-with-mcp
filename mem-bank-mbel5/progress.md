§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[STATUS]
✓poc::Complete{15/15-tests+live-verified}
✓solution::Implemented{custom-marshaller}
✓verification::Live{mcp__poc__*:all-work}

[METRICS]
@tests::15/15{100%}✓
@files::~25{src+tests+extensions}
@loc::~750
@time::~4h

[POC_VERDICT]
✅hypothesis::PROVEN{controllers-can-be-mcp-tools}
✅attributes::[McpServerToolType]+[McpServerTool]{work}
✅di::IUserService{injected-properly}
✅coexistence::HTTP+MCP{parallel}
⚠️requirement::CustomMarshaller{ActionResult:needs-unwrapping}
✅solution::WithToolsFromAssemblyUnwrappingActionResult{exists}

[CRITICAL_DISCOVERY]
!rootCause::MarshallerBroken{new-ValueTask:null}
!solution::ValueTask.FromResult{preserves-value}
!bonus::ActionResultUnwrapping{extract-data}
