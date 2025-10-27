§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[FOCUS]
⚡policyAuth::ImplementFilterPipeline+PolicyBasedAuthorization!
@plan::tasks/tddab-policy-based-authorization-implementation.md
@approach::PreFilter+MinimumRoleRequirement+IAuthorizationHandler
@pattern::User's-existing-authorization-code

[RECENT]
✓scoping::VERIFIED{3-tests:ALL-PASS}!
✓scopingProof::EachCall→NewScope{different:RequestId}
✓infrastructure::IHttpContextAccessor{registered}✓
✓keycloak::Running{docker-compose:up}✓
✓tests::22/22{100%}✓
©User>requested::PolicyBased+RoleHierarchy{like-existing-projects}
>created::TDDAB-Plan{tasks/tddab-policy-based-authorization-implementation.md}

[CURRENT_TASK]
@phase::ImplementPolicyBasedAuthorization
@plan::6-TDDABBlocks{infrastructure→preFilter→keycloak→tools→tests→verify}
@goal::29/29-tests{22-existing+7-new}
@pattern::FollowUser's-MinimumRoleRequirement+Handler

[PLAN_OVERVIEW]
@block1::AuthInfrastructure{
  PolicyNames+MinimumRoleRequirement+Handler+Extensions
  files:5-new{Authorization/*.cs}
}
@block2::PreFilterImplementation{
  CreateControllerWithPreFilter{check:[Authorize]-BEFORE-execution}
  location:McpServerBuilderExtensions.cs{enhance-TargetFactory}
}
@block3::KeycloakUsers{
  member@test.com:member123{role:Member}
  manager@test.com:manager123{role:Manager}
  admin@test.com:admin123{role:Admin}
}
@block4::PolicyProtectedTools{
  create:[Authorize(Policy=RequireMember)]
  update:[Authorize(Policy=RequireManager)]
  promote_to_manager:[Authorize(Policy=RequireAdmin)]
}
@block5::PolicyAuthorizationTests{
  7-tests{1-member-allow+2-manager+4-denials}
}
@block6::VerifyAllPass{
  expected:29/29{22-existing+7-new}
}

[PREREQUISITES_COMPLETE]
✓diScoping::Verified{HttpContext.RequestServices:works}
✓httpContextAccessor::Registered{Program.cs:110}
✓endpointAuth::Working{MapMcp().RequireAuthorization()}
✓keycloak::Running{127.0.0.1:8080}
✓tests::22/22{baseline:established}

[AUTHORIZATION_PATTERN]
@user'sPattern::{
  MinimumRoleRequirement:IAuthorizationRequirement
  MinimumRoleRequirementHandler:AuthorizationHandler<T>
  PolicyNames:constants{RequireMember+RequireManager+RequireAdmin}
  AuthorizationServiceExtensions:registration
}
@poc::ExactCopy{follow-existing-pattern:¬creative}

[PRE_FILTER_ARCHITECTURE]
```
MCP-Request
→TargetFactory{line:75}
  →CreateControllerWithPreFilter
    →Get:HttpContext{via:IHttpContextAccessor}
    →Check:[Authorize]{method||class}
    →If-Policy:IAuthorizationService.AuthorizeAsync()
      →MinimumRoleRequirementHandler
        →Get:User{email-claim}
        →Check:user.Role>=requirement.MinimumRole
        →Success:context.Succeed()||Failure:return
    →If-Success:CreateController||Throw:UnauthorizedAccessException
→Controller-Method
```

[ROLE_HIERARCHY]
@enum::UserRole{Member:1+Manager:2+Admin:3}
@hierarchy::Admin>Manager>Member
@tools::{
  get_by_id:no-policy{endpoint-auth-only}
  get_all:no-policy{endpoint-auth-only}
  get_scope_id:no-policy{endpoint-auth-only}
  create:RequireMember{Member+}
  update:RequireManager{Manager+}
  promote_to_manager:RequireAdmin{Admin-only}
}

[TEST_STRATEGY]
@newTests::PolicyAuthorizationTests{7-tests}
@scenarios::{
  ✓member→create:ALLOW{RequireMember}
  ✓manager→update:ALLOW{RequireManager}
  ✗member→update:DENY{RequireManager}
  ✓admin→promote:ALLOW{RequireAdmin}
  ✗manager→promote:DENY{RequireAdmin}
  ✗member→promote:DENY{RequireAdmin}
}
@verification::RoleHierarchy{Manager-can-create:inherits-Member}

[FILES_TO_CREATE]
1.src/McpPoc.Api/Authorization/PolicyNames.cs
2.src/McpPoc.Api/Authorization/MinimumRoleRequirement.cs
3.src/McpPoc.Api/Authorization/MinimumRoleRequirementHandler.cs
4.src/McpPoc.Api/Authorization/AuthorizationServiceExtensions.cs
5.tests/McpPoc.Api.Tests/PolicyAuthorizationTests.cs

[FILES_TO_MODIFY]
1.src/McpPoc.Api/Models/User.cs{+UserRole-enum+Role-property}
2.src/McpPoc.Api/Services/UserService.cs{update:users-with-roles}
3.src/McpPoc.Api/Program.cs{+AddMcpPocAuthorization()}
4.src/McpPoc.Api/Extensions/McpServerBuilderExtensions.cs{+CreateControllerWithPreFilter}
5.src/McpPoc.Api/Controllers/UsersController.cs{+policy-attributes+new-tools}
6.docker/keycloak/mcppoc-realm.json{+test-users:member+manager+admin}
7.tests/McpPoc.Api.Tests/McpToolDiscoveryTests.cs{expect:6-tools}

[EXPECTED_RESULTS]
@before::Tests{22/22:100%}
@after::Tests{29/29:100%}
@new::PolicyAuthorizationTests{7/7:100%}
@tools::Count{4→6:+create+update+promote_to_manager}

[CRITICAL_IMPLEMENTATION_NOTES]
!syncAuth::TargetFactory{synchronous:use-GetAwaiter().GetResult()}
!userLookup::EmailClaim{Keycloak→email→UserService}
!errorPropagation::UnauthorizedAccessException{SDK→MCP-error-response}
!logging::Comprehensive{Trace+Information+Warning}
!policyEval::IAuthorizationService{standard-AspNetCore}

[NEXT_STEPS]
1.await::User-says-ACT
2.implement::TDDAB-Block-1{AuthInfrastructure}
3.verify::build-agent{CLEAN}
4.implement::TDDAB-Block-2{PreFilter}
5.verify::build-agent{CLEAN}
6.implement::TDDAB-Block-3{Keycloak-users}
7.implement::TDDAB-Block-4{Policy-tools}
8.verify::build-agent{CLEAN}
9.implement::TDDAB-Block-5{Tests}
10.verify::test-agent{7/7-PASS}
11.implement::TDDAB-Block-6{Verify-all}
12.verify::test-agent{29/29-PASS}
13.update::MemoryBank{completion}

[SCOPING_LEARNINGS]
✓mechanism::HttpContext.RequestServices{already-scoped:by-AspNetCore}
✓evidence::DIScopingTests{3/3-PASS:different-RequestIds}
✓conclusion::EFCore{WILL-WORK:scoped-per-request}
!aiFunction::[FromServices]¬Supported{use:constructor-injection}
!serialization::SnakeCaseLower{MCP:snake_case¬camelCase}
!dtoRequired::ProperRecord{¬anonymous-objects}
