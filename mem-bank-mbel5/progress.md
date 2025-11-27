§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[STATUS]
✅phase5::COMPLETE{library-extraction:Zero.Mcp.Extensions-v1.9.0}✅!
⚡phase6::Ready{tool-filtering:TDDAB-v2:simplified}⚡
✓httpAuth::Complete{3+4-viewer-tests:7/7}✓!
✓mcpAuth::Complete{7+4-viewer-tests:11/11}✓!
@tests::44/44{100%}✅!
✓roles::4-tier{Viewer:0+Member:1+Manager:2+Admin:3}✓!
✓infrastructure::Complete{library+policies+viewer-role}✓

[METRICS]
@tests::44/44{100%:ALL-PASSING}✅!
@coverage::{
  6×ToolDiscovery✓
  5×ToolInvocation✓
  3×HttpCoexistence✓
  2×ActionResultSerialization✓
  4×Authentication✓
  3×DIScopingTests✓
  7×HttpAuthorizationTests{3-original+4-viewer}✓!
  11×PolicyAuthorizationTests{7-original+4-viewer}✓!
}
@library::Zero.Mcp.Extensions{v1.9.0:production-ready}✅
@files::~50{+library-project+viewer-role}
@loc::~2000{+550}
@time::~20h{+5h-phase5}
@users::6{viewer+alice+bob+carol+admin+user}
@roles::4{hierarchy:Viewer<Member<Manager<Admin}

[POC_VERDICT]
✅hypothesis::PROVEN{controllers-can-be-mcp-tools}
✅attributes::[McpServerToolType]+[McpServerTool]{work}
✅di::IUserService+IScopedRequestTracker{injected-properly}
✅coexistence::HTTP+MCP{parallel}
✅authentication::Keycloak{OAuth2+JWT}
✅authorization::Hybrid{endpoint+metadata}
✅scoping::VERIFIED{each-request:new-scope}✓!
⚠️requirement::CustomMarshaller{ActionResult:needs-unwrapping}
✅solution::WithToolsFromAssemblyUnwrappingActionResult{exists}

[PHASE5_COMPLETE]
✅library::Zero.Mcp.Extensions{v1.9.0:production-ready}✅!
✓extraction::Complete{single-file→library-project}✓
✓tests::44/44{36-core+8-viewer}✅!
✓config::ZeroMcpOptions{RequireAuth+UseAuth+Path+Assembly}✓
✓viewer-role::Added{UserRole.Viewer:0:read-only}✓!
✓viewer-user::Created{ID:102:viewer:viewer123}✓
✓viewer-tests::8-comprehensive{4-MCP+4-HTTP}✓!
✓keycloak::Recreated{fresh-DB-with-viewer-realm}✓
✓scripts::do-login-poc.sh{auto-login+mcp-update}✓
✓password-flow::Fixed{¬client-credentials:user-context-needed}✓!

[PHASE6_PLANNING]
⚡status::Ready-to-implement{TDDAB-v2}⚡
@problem::UX{viewer-sees-5-tools→tries-create→403}
@solution::Filter-tools/list{by-user-role}
@approach::SDK-AddListToolsFilter{¬decorator-pattern}✅

[PHASE6_TDDAB_V2]
@version::v2{simplified:refactored-from-v1}
@blocks::3{reduced-from-4}
@new-code::~110-lines{reduced-from-~400}
@new-tests::15{5+6+4}
@target::59-tests{44+15}

@block1::ToolAuthorizationMetadata{
  ToolAuthorizationMetadata::record{toolName+minRole}
  ToolAuthorizationStore::dictionary{IToolAuthorizationStore}
  FromMethod::extractor{[Authorize]→minRole}
  tests::5
}

@block2::ToolListFilter{
  FilterByRole::static{allTools+userRole+store→filtered}
  GetUserRole::static{ClaimsPrincipal→int?}
  SDK-integration::AddListToolsFilter{request.User+request.Services}
  tests::6
}

@block3::IntegrationTests{
  Viewer→2-tools{get_by_id+get_all}
  Member→3-tools{+create}
  Manager→4-tools{+update}
  Admin→5-tools{+promote_to_manager}
  tests::4
}

[KEY_DISCOVERY_PHASE6]
!sdk-has-filter::AddListToolsFilter{built-in:designed-for-this}⭐⭐⭐
!request-context::{
  request.User::ClaimsPrincipal{role-claims-available}
  request.Services::IServiceProvider{DI-available}
}
!no-decorator-needed::IMcpServer-wrapping{overkill:SDK-provides-hook}
!simple-wins::~110-lines>~400-lines{same-functionality}⭐

[CRITICAL_DISCOVERIES]
!marshallerBug::new-ValueTask{loses-value}→ValueTask.FromResult{preserves}
!audienceValidation::Keycloak{azp¬aud}→ValidateAudience:false
!dnsPerformance::localhost{slow:NSPLookup}→127.0.0.1{fast:39ms}
!sdkFeature::MetadataCollection{automatic:[Authorize]}
!aiFunction::[FromServices]¬Supported{use:constructor-injection}!
!serialization::SnakeCaseLower{MCP:snake_case¬camelCase}!
!scoping::HttpContext.RequestServices{already-scoped:works}✓!
!mcpParameterBinding::DTO-needs-nesting{args:{"request":{inner-props}}}!
!mcpIsError::Null-for-success{use:.NotBe(true):¬.BeFalse()}!
!clientCredentials::No-user-context{use:alice@example.com:for-auth-tests}!
!sdk-filter::AddListToolsFilter{use-SDK-hooks:¬fight-framework}⭐⭐⭐

[READY_FOR]
✓efCore::AddDbContext{confidence:100%:scoping-proven}!
✅phase5::COMPLETE{library+viewer+tests:44/44}✅!
⚡phase6::ToolFiltering{TDDAB-v2:3-blocks:ready}⚡!
?phase7::AdvancedAuth{custom-requirements+conditional-policies}
?production::Deploy{security:verified+scoping:verified+4-role-auth:proven}

[TASK_FILE]
@location::tasks/tddab-tool-filtering-by-permissions.md
@version::v2{simplified}
@status::Ready-for-ACT
