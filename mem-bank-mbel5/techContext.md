§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[STACK]
@runtime::.NET§9.0.300
@framework::AspNetCore§9.0
@protocol::MCP§0.4.0-preview.3
@auth::Keycloak§27.0.0{OAuth2+OIDC}
@logging::Serilog
@testing::xUnit+FluentAssertions+WebApplicationFactory

[KEY_FILES]
src/McpPoc.Api/
├─Extensions/McpServerBuilderExtensions.cs{solution:170lines}
├─Program.cs{.WithToolsFromAssemblyUnwrappingActionResult()+RequireAuthorization()}
├─Controllers/UsersController.cs{[McpServerToolType]+[Authorize]}
├─Services/{IUserService+UserService}
└─appsettings.json{Keycloak:127.0.0.1:8080}!

tests/McpPoc.Api.Tests/
├─McpApiFixture.cs{on-demand-auth+cache}!
├─KeycloakTokenHelper.cs{client-credentials+password-grant:127.0.0.1}!
├─McpToolDiscoveryTests.cs{5/5✓}
├─McpToolInvocationTests.cs{5/5✓}
├─HttpCoexistenceTests.cs{3/3✓}
├─ActionResultSerializationTest.cs{2/2✓}
└─AuthenticationTests.cs{4/4✓:new}

docker/
├─docker-compose.yml{keycloak+postgres}
└─keycloak/mcppoc-realm.json{127.0.0.1-support}!

[SOLUTION]
WithToolsFromAssemblyUnwrappingActionResult::{
  scan::[McpServerToolType]
  create::AIFunction{AIFunctionFactoryOptions}
  MarshalResult::UnwrapActionResult
  register::McpServerTool
}

UnwrapActionResult::{
  ActionResult<T>→Result→IActionResult→Value
  ValueTask.FromResult(unwrapped)
}

[AUTHENTICATION_SETUP]
@keycloak::{
  realm::mcppoc-realm
  client::mcppoc-api{secret:mcppoc-api-secret}
  users::{admin:admin123,user:user123}
  flows::client_credentials+password
  url::http://127.0.0.1:8080!
}

@jwt::{
  Authority::http://127.0.0.1:8080/realms/mcppoc-realm
  Audience::account{ValidateAudience:false:azp¬aud}!
  RequireHttpsMetadata::false{dev-only}
  ValidateIssuer::true
  ValidateLifetime::true
}

[TEST_FIXTURE_PATTERN]
@fixture::McpApiFixture{WebApplicationFactory<Program>}
@methods::{
  GetAuthenticatedClientAsync()→client-credentials-token+cache
  GetAuthenticatedClientAsync(user,pass)→password-token+cache
  GetUnauthenticatedClient()→no-auth
}
@helper::KeycloakTokenHelper{
  GetClientCredentialsTokenAsync()
  GetPasswordTokenAsync(user,pass)
  url::http://127.0.0.1:8080!
}

[TOOLS_LIVE]
✓get_all::mcp__poc__get_all()+Bearer-required
✓get_by_id::mcp__poc__get_by_id(id)+Bearer-required
✓create::mcp__poc__create(name,email)+Bearer-required

[CRITICAL_PATTERNS]
!new-ValueTask(result)→null{broken}
!ValueTask.FromResult(result)→value{works}
!ValidateAudience:true→fail{Keycloak:azp¬aud}
!ValidateAudience:false→works{accept:azp-claim}
!localhost→slow{NSPLookupServiceBegin}
!127.0.0.1→fast{39ms:skip-DNS}

[DEV_COMMANDS]
@docker::{
  up::docker-compose-up-d{keycloak:8080+postgres:5432}
  logs::docker-compose-logs-f-keycloak
  restart::docker-compose-restart-keycloak
  clean::docker-compose-down-v{delete-volumes}
}
@test::{
  all::dotnet-test
  watch::dotnet-watch-test
  filter::dotnet-test--filter-FullyQualifiedName~Auth
}
@run::{
  api::dotnet-run--project-src/McpPoc.Api
  swagger::http://127.0.0.1:5001/swagger
  mcp::http://127.0.0.1:5001/mcp
}
