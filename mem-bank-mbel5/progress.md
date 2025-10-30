§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[STATUS]
✅phase5::COMPLETE{library-extraction:Zero.Mcp.Extensions-v1.9.0}✅!
⚡phase6::Planning{tool-filtering:metadata-based}⚡
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

[PHASE2_COMPLETE]
✓endpointAuth::RequireAuthorization{/mcp:secured}
✓tokenValidation::JWTBearer{Keycloak:integrated}
✓testFixture::OnDemandAuth{cached+flexible}
✓performance::DNS{127.0.0.1:fast}
✓metadata::SDK{[Authorize]:auto-collected}

[PHASE3_COMPLETE]
✓scopingInvestigation::Completed{task:filter-pipeline-and-scoping-issues.md}
✓scopingTests::Created{DIScopingTests:3-tests}✓!
✓scopingProof::Verified{each-call:new-scope+different-RequestId}✓!
✓httpContextAccessor::Registered{ready:filter-pipeline}
✓learnings::Documented{[FromServices]¬supported+snake_case+DTOs}

[PHASE4_COMPLETE]
✅policyAuth::100%{http+mcp:ALL-WORKING}✅!
✓infrastructure::{
  PolicyNames+MinimumRoleRequirement+Handler+Extensions
}✓
✓preFilter::Implemented{CreateControllerWithPreFilter}✓
✓httpTests::3/3{Create+Update+Block}✓!
✓mcpTests::7/7{Create+Update+Promote+Blocks}✓!
✓roles::Alice{Member}+Bob{Manager}+Carol{Admin}✓
✓scoping::Singleton→Scoped{test-isolation-fixed}✓!
✓parameterBinding::Nested-DTO-wrapper{request:{name+email}}✓!
✓assertions::IsError-null-check{.NotBe(true):success}✓!

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

[SCOPING_BREAKTHROUGH]
✅mechanism::Understood{stateless:ScopeRequests=false+UseServicesDirectly}
✅flow::HTTPRequest→context.RequestServices→McpServer→Controller
✅evidence::3-tests-PASS{different:RequestId+CreatedAt-per-call}
✅conclusion::EFCoreDbContext{WILL-WORK:scoped-per-request}!
✅tools::#4{get_by_id+get_all+create+get_scope_id}

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

[READY_FOR]
✓efCore::AddDbContext{confidence:100%:scoping-proven}!
✅phase5::COMPLETE{library+viewer+tests:44/44}✅!
⚡phase6::ToolFiltering{metadata-based:TDDAB-4-blocks}⚡!
?phase7::AdvancedAuth{custom-requirements+conditional-policies}
?production::Deploy{security:verified+scoping:verified+4-role-auth:proven}
