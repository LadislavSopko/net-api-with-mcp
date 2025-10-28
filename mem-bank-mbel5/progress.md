§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[STATUS]
✅phase4::COMPLETE{policy-authorization:100%}✅!
✓httpAuth::Complete{3/3-tests}✓!
✓mcpAuth::Complete{7/7-tests}✓!
@tests::32/32{100%}✅
✓infrastructure::Complete{4-files+policies}✓
✓scoping::Fixed{Singleton→Scoped:test-isolation}✓

[METRICS]
@tests::32/32{100%:ALL-PASSING}✅!
@coverage::{
  6×ToolDiscovery✓
  5×ToolInvocation✓
  3×HttpCoexistence✓
  2×ActionResultSerialization✓
  4×Authentication✓
  3×DIScopingTests✓
  3×HttpAuthorizationTests✓
  7×PolicyAuthorizationTests✓!
}
@files::~40{+7-auth-files}
@loc::~1450{+350}
@time::~15h{+5h-phase4}

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

[READY_FOR]
✓efCore::AddDbContext{confidence:100%:scoping-proven}!
✓phase4::COMPLETE{pre-filter-auth+policy-based:32/32-tests}✅!
?phase5::ProductionFeatures{real-CRUD+EFCore+DbContext}
?phase6::AdvancedAuth{claims+custom-requirements+conditional-policies}
?production::Deploy{security:verified+scoping:verified+authorization:proven}
