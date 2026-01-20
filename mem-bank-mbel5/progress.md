§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[STATUS]
✅upgrade::.NET10{9.0→10.0.100:complete+verified}✅!
✅phase5::COMPLETE{library-extraction:Zero.Mcp.Extensions-v2.0.0}✅
✅phase7::COMPLETE{ToolNaming+McpContext:v2.1.0}✅!
⏸phase6::RoleBasedToolFiltering{paused:ready-to-resume}
@tests::131/131{100%}✅
@version::2.1.0✅

[METRICS]
@tests::131/131{100%:ALL-PASSING}✅!
@coverage::{
  75×Zero.Mcp.Extensions.Tests✓
  56×McpPoc.Api.Tests✓
}
@library::Zero.Mcp.Extensions{v2.1.0:.NET10}✅
@framework::.NET§10.0.100{LTS:3-years}✅
@openapi::Scalar§2.12.11{modern-UI}✅

[POC_VERDICT]
✅hypothesis::PROVEN{controllers-can-be-mcp-tools}
✅attributes::[McpServerToolType]+[McpServerTool]{work}
✅di::IUserService+IScopedRequestTracker{injected-properly}
✅coexistence::HTTP+MCP{parallel}
✅authentication::Keycloak{OAuth2+JWT}
✅authorization::Hybrid{endpoint+metadata}
✅scoping::VERIFIED{each-request:new-scope}✓
✅.NET10::UPGRADED{all-packages+tests-passing}✅
✅toolNaming::IMPLEMENTED{ControllerPrefix+MethodOnly}✅
✅mcpContext::IMPLEMENTED{IMcpRequestContext+x-mcp-call}✅
⚠️requirement::CustomMarshaller{ActionResult:needs-unwrapping:SDK-still-missing}
✅solution::WithToolsFromAssemblyUnwrappingActionResult{exists}

[PHASE7_COMPLETE]
✅status::COMPLETE{TDDAB:9-blocks:131-tests}✅!
@problem1::GenericControllers{duplicate-tool-names}→SOLVED✅
@problem2::HeaderAccess{need-mcp-call-detection}→SOLVED✅
@solution1::ToolNamingConvention{ControllerPrefix:works}✅
@solution2::IMcpRequestContext{+path-based-fallback}✅

@block1::Options{enum+defaults:4-tests}✅
@block2::ToolNameGenerator{core-logic:7-tests}✅
@block3::BuilderIntegration{7-tests}✅
@block4::IMcpRequestContext{interface+impl:7-tests}✅
@block5::Middleware{x-mcp-call:3-tests}✅
@block6::Registration{2-tests}✅
@block7::E2E-ToolNaming{4-tests}✅
@block8::E2E-McpContext{4-tests}✅
@block9::VersionBump{2.1.0+pack}✅

[CRITICAL_DISCOVERIES]
!marshallerBug::new-ValueTask{loses-value}→ValueTask.FromResult{preserves}
!audienceValidation::Keycloak{azp¬aud}→ValidateAudience:false
!dnsPerformance::localhost{slow:NSPLookup}→127.0.0.1{fast:39ms}
!sdkFeature::MetadataCollection{automatic:[Authorize]}
!aiFunction::[FromServices]¬Supported{use:constructor-injection}
!serialization::SnakeCaseLower{MCP:snake_case¬camelCase}
!scoping::HttpContext.RequestServices{already-scoped:works}
!mcpParameterBinding::DTO-needs-nesting{args:{"request":{inner-props}}}
!mcpIsError::Null-for-success{use:.NotBe(true):¬.BeFalse()}
!clientCredentials::No-user-context{use:alice@example.com:for-auth-tests}
!sdk-filter::AddListToolsFilter{use-SDK-hooks:¬fight-framework}
!fluentAssertions8::BeGreaterOrEqualTo→BeGreaterThanOrEqualTo{breaking}
!swashbuckle10::Microsoft.OpenApi.Models{removed}→use-Scalar
!genericControllers::DuplicateToolNames→SOLVED{ControllerPrefix}✅
!headerAccess::Need-IMcpRequestContext→SOLVED{+path-fallback}✅
!sdk-httpcontext::NotFlowed-to-tool-scopes{use:path-based-detection}!

[READY_FOR]
✓efCore::AddDbContext{confidence:100%:scoping-proven}
✅phase5::COMPLETE{library+viewer+tests}✅
✅.NET10::COMPLETE{upgraded+verified}✅
✅phase7::COMPLETE{ToolNaming+McpContext:v2.1.0}✅!
⏸phase6::ToolFiltering{ready-to-resume}
?phase8::AdvancedAuth{custom-requirements+conditional-policies}
?production::Deploy{security:verified+scoping:verified}

[TASK_FILES]
@phase6::tasks/tddab-tool-filtering-by-permissions.md{paused}
@phase7::tasks/tddab-tool-naming-and-mcp-context.md{COMPLETE}✅

[V2.1.0_RELEASE_NOTES]
@features::{
  ToolNamingConvention::ControllerPrefix{generic-controllers}
  IMcpRequestContext::header-access+mcp-detection
  UseZeroMcpMarking::middleware{x-mcp-call-injection}
  McpServerTool.Name::explicit-tool-naming
}
@package::Zero.Mcp.Extensions.2.1.0.nupkg✅
