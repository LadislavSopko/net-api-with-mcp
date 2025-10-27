§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[FOCUS]
⚡filterPipeline::DesignMCPToolFilterChain{auth+validation+logging}
@pattern::PreFilters→AIFunction→PostFilters
@challenge::MaintainProperDIScoping{like-normal-API}

[RECENT]
>fixed::DNSIssue{API:localhost+Keycloak:127.0.0.1→401}✓
>updated::launchSettings{should-use:127.0.0.1:5001}?
>analyzed::ScopingMechanism{HttpContext.RequestServices}
>discovered::SDKScopingBehavior{stateless:ScopeRequests:false}
©User>requested::FilterPipelineDesign{pre+post+auth}

[CURRENT_PROBLEM]
!design::FilterPipeline{
  goal:pre-filter+auth-check+post-filter
  challenge:hide-inside-AIFunction
  avoid:overcomplicated-wrappers
}

[SCOPING_ANALYSIS]
@flow::HTTPRequest→AspNetCore→CreateScope→HttpContext.RequestServices
@mcpFlow::context.RequestServices→McpServer→RequestServiceProvider→args.Services
@concern::StatelessMode{ScopeRequests:false}!
@location::StreamableHttpHandler.cs:228{ScopeRequests=false:stateless}
@location::McpServerImpl.cs:667{conditional-scoping}
@currentBehavior::{
  stateful:ScopeRequests:true→CreateAsyncScope{line:677}
  stateless:ScopeRequests:false→UseServicesDirectly{line:669}
}
@question::DoesCurrentScopingWorkForEFCore?{needs-verification}!

[FILTER_PIPELINE_DISCUSSION]
@interceptionPoints::{
  1.TargetFactory{line:75:args→CreateControllerInstance}→PRE-FILTER
  2.MarshalResult{line:109:UnwrapActionResult}→POST-FILTER
}
@rejected::ComplexWrappers{proxy+dynamic-methods+expression-trees}
@preferred::SimpleEnhancement{existing-interception-points}

[AUTHENTICATION_WORKING]
@provider::Keycloak§8080{OAuth2+OIDC}✓
@endpoint::/mcp{RequireAuthorization}✓
@token::JWT{Bearer:Authorization-header}✓
@validation::JWTBearer{ValidateAudience:false:azp¬aud}✓
@config::ALL{127.0.0.1:not-localhost}!

[DNS_PERFORMANCE_FIX]
!problem::DNSLookup{NSPLookupServiceBegin:slow}✓
!problem2::MixedHosts{API:localhost+Keycloak:127.0.0.1→401}✓
@fix::Consistent127.0.0.1{everywhere}✓
@files::{
  KeycloakTokenHelper:http://127.0.0.1:8080✓
  mcppoc-realm.json:urls→127.0.0.1✓
  appsettings.json:Authority→127.0.0.1✓
  launchSettings.json:applicationUrl→localhost{should-fix}?
}

[NEXT_SESSION_TASKS]
!verify::DIScopingWorks{test:EFCore+scoped-services}!
!implement::FilterPipeline{
  pre:authorization-check
  post:result-transformation+logging
}
!check::launchSettings{use:127.0.0.1:5001}
!design::AuthorizationFilter{
  check:[Authorize]-attribute
  verify:User.Identity.IsAuthenticated
  validate:Roles{if-specified}
  access:HttpContext{via:IHttpContextAccessor}
}
!consider::ScopeRequests{stateful-vs-stateless:implications}
