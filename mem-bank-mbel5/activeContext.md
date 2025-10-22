§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[FOCUS]
⚡security::HybridMCPAuthorization{implemented}✓
@pattern::EndpointAuth+PerToolMetadata
>completed::AuthenticationIntegration{Keycloak+JWT}

[RECENT]
>added::HybridAuthorization{/mcp:.RequireAuthorization()}✓
>refactored::TestFixture{on-demand-auth+caching}✓
>fixed::DNSPerformance{localhost→127.0.0.1:~39ms}✓
>updated::KeycloakConfig{accept:127.0.0.1}✓
©User>decided::HybridApproach{endpoint+metadata}

[SECURITY_MODEL]
@layer1::EndpointAuth{MapMcp("/mcp").RequireAuthorization()}!
@layer2::PerToolMetadata{[Authorize]:collected-by-SDK}
@flow::Request→JWTValidation→HttpContext.User→Available
@ready::FineGrainedChecks{HttpContext+User.Identity}?

[AUTHENTICATION]
@provider::Keycloak§8080{OAuth2+OIDC}
@flow::ClientCredentials||PasswordGrant
@token::JWT{Bearer:Authorization-header}
@validation::JWTBearer{ValidateAudience:false:azp¬aud}!

[TEST_FIXTURE]
@pattern::OnDemandAuth{lazy-load+cache}
@methods::{
  GetAuthenticatedClientAsync():client-credentials
  GetAuthenticatedClientAsync(user,pass):password-grant
  GetUnauthenticatedClient():401-tests
}
@cache::Dictionary<string,string>{per-user-tokens}
@benefit::NoConstructorDelay+RoleBasedTesting

[PERFORMANCE_FIX]
!problem::DNSLookup{NSPLookupServiceBegin:slow}
@fix::localhost→127.0.0.1{token-request:~39ms}✓
@changes::{
  KeycloakTokenHelper:default→http://127.0.0.1:8080
  mcppoc-realm.json:urls→127.0.0.1
  appsettings.json:Authority→127.0.0.1
}

[NEXT]
?RoleBasedAuth::PerToolAuthorization{admin|user}
?CustomPolicies::AuthorizationPolicies{Claims+Roles}
?AuditLogging::SecurityEvents{who+what+when}
