§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[STACK]
@runtime::.NET§10.0.100
@framework::AspNetCore§10.0
@protocol::MCP§0.6.0-preview.1
@auth::Keycloak§27.0.0{OAuth2+OIDC}
@logging::Serilog§10.0.0
@testing::xUnit+FluentAssertions§8.8.0+WebApplicationFactory
@openapi::Scalar.AspNetCore§2.12.11{replaced:Swashbuckle}!

[KEY_FILES]
src/McpPoc.Api/
├─Extensions/McpServerBuilderExtensions.cs{solution:170lines}
├─Program.cs{.WithToolsFromAssemblyUnwrappingActionResult()+RequireAuthorization()+Scalar}
├─Controllers/UsersController.cs{[McpServerToolType]+[Authorize]}
├─Services/{IUserService+UserService}
└─appsettings.json{Keycloak:127.0.0.1:8080}!

tests/McpPoc.Api.Tests/
├─McpApiFixture.cs{on-demand-auth+cache}!
├─KeycloakTokenHelper.cs{client-credentials+password-grant:127.0.0.1}!
├─McpToolDiscoveryTests.cs{6/6✓}
├─McpToolInvocationTests.cs{5/5✓}
├─HttpCoexistenceTests.cs{3/3✓}
├─ActionResultSerializationTest.cs{2/2✓}
└─AuthenticationTests.cs{4/4✓}

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
  users::{admin:admin123,user:user123,viewer:viewer123}
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
  scalar::http://127.0.0.1:5001/scalar{replaced:swagger}!
  mcp::http://127.0.0.1:5001/mcp
}

[PACKAGE_VERSIONS]
@aspnetcore::{
  Microsoft.AspNetCore.OpenApi::10.0.2
  Microsoft.AspNetCore.Authentication.JwtBearer::10.0.2
  Microsoft.AspNetCore.Authorization::10.0.2
  Microsoft.AspNetCore.Mvc.Testing::10.0.2
}
@extensions::{
  Microsoft.Extensions.Logging.Abstractions::10.0.2
  Microsoft.Extensions.DependencyInjection.Abstractions::10.0.2
}
@logging::{
  Serilog.AspNetCore::10.0.0
  Serilog.Sinks.File::7.0.0
}
@testing::{
  Microsoft.NET.Test.Sdk::18.0.1
  xunit::2.9.3
  xunit.runner.visualstudio::3.1.5
  FluentAssertions::8.8.0
  Moq::4.20.72
}
@openapi::{
  Scalar.AspNetCore::2.12.11
}
