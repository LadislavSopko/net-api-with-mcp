§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[STATUS]
✅shipped::Zero.Mcp.Extensions§3.0.0{nuget.org:2026-09-17:MCP-SDK-2.2.0:SDK-native-authorization}✅
✅phase8::UpgradeToSdk2.2.0{COMPLETE:9/9-blocks:merged→main:4c7f09e:details→history.md}✅
✅phase5::LibraryExtraction{v2.0.0}✅ | ✅phase7::ToolNaming+McpContext{v2.1.0}✅ | ✅upgrade::.NET10✅
✗phase6::RoleBasedToolFiltering{SUPERSEDED←SDK-AddAuthorizationFilters}
@tests::158/158{100%}✅{99-unit+59-E2E}
@lint-gate::STRICT{TreatWarningsAsErrors:true:0-warnings-4-projects:ast-grep-absent}✅
@infra::.ai-agent-submodule+git-crypt(.00-secrets/)+j-settings.md+cvm-mcp+4-role-poc-servers✅

[METRICS]
@tests::158/158{100%:ALL-PASSING}✅!
@coverage::{
  99×Zero.Mcp.Extensions.Tests✓
  59×McpPoc.Api.Tests✓
}
@library::Zero.Mcp.Extensions{v3.0.0:.NET10}✅
@openapi::Scalar§2.17.3{modern-UI}✅
@framework::.NET§10.0.100{LTS:3-years}✅

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

[CRITICAL_DISCOVERIES]
!sdk-httpcontext::FLOWS-into-tools{SDK-2.2.0-stateless:RequestServices+request-ExecutionContext:IsMcpCall=true:verified-E2E:supersedes-the-old-NotFlowed-claim}✅
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

[READY_FOR]
✓efCore::AddDbContext{confidence:100%:scoping-proven}
✗phase6::ToolFiltering{SUPERSEDED:SDK-AddAuthorizationFilters-ships-in-3.0.0}
?phase8::AdvancedAuth{custom-requirements+conditional-policies}
?production::Deploy{security:verified+scoping:verified}

[TASK_FILES]
@phase8::tasks/01-upgrade-mcp-sdk-2/{plan.md+notes.md:COMPLETE:all-checkboxes-ticked}✅
@phase6::tasks/old/tddab-tool-filtering-by-permissions.md{superseded-by-SDK-filters}
