§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[STATUS]
✅upgrade::.NET10{9.0→10.0.100:complete+verified}✅!
✅phase5::COMPLETE{library-extraction:Zero.Mcp.Extensions-v2.0.0}✅
⚡phase7::ToolNaming+McpContext{TDDAB:ready}⚡!
⏸phase6::RoleBasedToolFiltering{paused:phase7-priority}
@tests::97/97{100%}✅
@next-version::2.1.0{after-phase7}

[METRICS]
@tests::97/97{100%:ALL-PASSING}✅!
@coverage::{
  49×Zero.Mcp.Extensions.Tests✓
  48×McpPoc.Api.Tests✓
}
@library::Zero.Mcp.Extensions{v2.0.0:.NET10→v2.1.0:planned}
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
⚠️requirement::CustomMarshaller{ActionResult:needs-unwrapping:SDK-still-missing}
✅solution::WithToolsFromAssemblyUnwrappingActionResult{exists}

[PHASE7_PLAN]
⚡status::Ready-to-implement{TDDAB:9-blocks}⚡
@problem1::GenericControllers{duplicate-tool-names}
@problem2::HeaderAccess{need-mcp-call-detection}
@solution1::ToolNamingConvention{ControllerPrefix}
@solution2::IMcpRequestContext{+x-mcp-call-auto}

[PHASE7_TDDAB]
@plan::tasks/tddab-tool-naming-and-mcp-context.md
@blocks::9
@new-tests::31
@target::128-tests{97+31}
@new-loc::~290
@version::2.0.0→2.1.0

@block1::Options{enum+defaults:4-tests}⏳
@block2::ToolNameGenerator{core-logic:6-tests}⏳
@block3::BuilderIntegration{3-tests}⏳
@block4::IMcpRequestContext{interface+impl:5-tests}⏳
@block5::Middleware{x-mcp-call:3-tests}⏳
@block6::Registration{2-tests}⏳
@block7::E2E-ToolNaming{4-tests}⏳
@block8::E2E-McpContext{4-tests}⏳
@block9::VersionBump{2.1.0+pack}⏳

[.NET10_UPGRADE_COMPLETE]
✅framework::.NET{9.0→10.0.100}✅
✅packages::All{→10.x-versions}✅
✅openapi::Swashbuckle→Scalar.AspNetCore✅
✅breaking-changes::Fixed{FluentAssertions+Scalar}✅
✅tests::97/97{verified}✅

[SDK_HACK_STATUS]
@checked::2026-01-20
@result::Still-needed{MarshalResult:ActionResult<T>-unwrapping}
@sdk-version::0.6.0-preview.1
@sdk-default::MarshalResult{static(result)→new-ValueTask<object?>(result)}
@no-unwrap::ActionResult<T>+Task<T>+ObjectResult
@our-solution::MarshalResult.UnwrapAsync{complete-chain}✅

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
!genericControllers::DuplicateToolNames{need:ControllerPrefix}!
!headerAccess::Need-IMcpRequestContext{+x-mcp-call-auto}!

[READY_FOR]
✓efCore::AddDbContext{confidence:100%:scoping-proven}
✅phase5::COMPLETE{library+viewer+tests:97/97}✅
✅.NET10::COMPLETE{upgraded+verified}✅
⚡phase7::ToolNaming+McpContext{TDDAB:ready}⚡!
⏸phase6::ToolFiltering{paused}
?phase8::AdvancedAuth{custom-requirements+conditional-policies}
?production::Deploy{security:verified+scoping:verified}

[TASK_FILES]
@phase6::tasks/tddab-tool-filtering-by-permissions.md{paused}
@phase7::tasks/tddab-tool-naming-and-mcp-context.md{active}!
