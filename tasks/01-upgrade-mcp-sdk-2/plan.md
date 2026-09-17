# TDDAB Plan: Upgrade Zero.Mcp.Extensions to MCP C# SDK 2.2.0 (v3.0.0)
**Date:** 2026-09-14

<mission>
## Project
Repo `D:\Projekty\AI_Works\net-api-with-mcp` (solution `net-api-with-mcp.slnx`, .NET 10, C# latest, central package management in `Directory.Packages.props`, shared props in `Directory.Build.props`, version in `Version.props` via `MainVersion`).
Two shipped projects and two test projects:
- `src/Zero.Mcp.Extensions/` — NuGet library. Scans an assembly for ASP.NET Core controllers marked with an `McpServerToolType` attribute and exposes methods marked with an `McpServerTool` attribute as MCP tools, unwrapping `ActionResult<T>` return values (`MarshalResult.cs`), generating tool names (`ToolNameGenerator.cs`, `ToolNamingConvention.cs` MethodOnly|ControllerPrefix), exposing `IMcpRequestContext`/`McpRequestContext` (detects MCP calls via `HttpContext.Items` marker set by `UseZeroMcpMarking` middleware, fallback path prefix `/mcp`), and configured through `ZeroMcpOptions` (`RequireAuthentication`, `UseAuthorization`, `McpEndpointPath`, `ToolAssembly`, `SerializerOptions`, `NamingConvention`, `ToolNameSeparator`). Entry points: `services.AddZeroMcpExtensions(options => ...)` and `app.MapZeroMcp()` in `McpServerBuilderExtensions.cs`.
- `src/McpPoc.Api/` — demo API: `Controllers/UsersController.cs` (class-level `[Authorize]`, 9 tools: GetById with explicit `Name = "UserGetById"`, GetAll, Create [RequireMember], Update [RequireManager], PromoteToManager [RequireAdmin], GetScopeId, GetPublicInfo [AllowAnonymous], GetMcpContext, EchoHeaders [AllowAnonymous]), Keycloak JWT bearer auth (`Program.cs`), policies in `Authorization/AuthorizationServiceExtensions.cs` + `PolicyNames.cs` (RequireMember/RequireManager/RequireAdmin built on realm roles), Serilog, Scalar. Config flag `Auth:Enabled` (default true) switches auth off entirely.
  Real MCP tool names (MethodOnly convention, explicit attribute name always wins — `ToolNameGenerator.cs`): `UserGetById`, `get_all`, `create`, `update`, `promote_to_manager`, `get_scope_id`, `get_public_info`, `get_mcp_context`, `echo_headers`. Visibility per role: viewer = 6 base tools (`UserGetById`, `get_all`, `get_scope_id`, `get_public_info`, `get_mcp_context`, `echo_headers`), member +`create` (7), manager +`update` (8), admin all 9. The E2E files written before v2.1.0 still assert `get_by_id` and omit `echo_headers`; block 01 corrects them. Measured baseline on 2026-09-14 before any block: E2E 45/56 green, the 11 red ones are exactly ToolVisibilityTests (4), ToolNamingTests (2), McpToolInvocationTests (2), McpToolDiscoveryTests (2), PolicyAuthorizationTests.Should_AllowRead_WhenUserIsViewer — all caused by `get_by_id` / missing `echo_headers`.
- Docker: `docker compose -f docker/docker-compose.yml up -d` (Keycloak on 8080). If host port 5432 is taken by another Postgres, start with `POSTGRES_PORT=15432 docker compose -f docker/docker-compose.yml up -d` — Keycloak reaches Postgres over the compose network, the host port is irrelevant for the tests.
- `tests/Zero.Mcp.Extensions.Tests/` — unit tests for the library.
- `tests/McpPoc.Api.Tests/` — E2E tests using `WebApplicationFactory<Program>` + `McpClientHelper.cs` + `KeycloakTokenHelper.cs` (needs docker Keycloak on http://127.0.0.1:8080, realm mcppoc-realm, users admin/admin123 etc.; start with `docker compose -f docker/docker-compose.yml up -d`). Regression net: `PolicyAuthorizationTests`, `ToolVisibilityTests`, `McpToolInvocationTests`, `McpToolDiscoveryTests`, `McpRequestContextE2ETests`, `ToolNamingTests`, `DIScopingTests`, `AuthenticationTests`, `HttpAuthorizationTests`, `ActionResultSerializationTest`.
- `3rdp/csharp-sdk/` — vendored git checkout of the official MCP C# SDK, included in the .slnx for reading only (no ProjectReference from our projects). Currently at v0.6.0-preview.1; will be moved to tag v2.2.0.

## Target state (decisions already made, do not reopen)
- `ModelContextProtocol`, `ModelContextProtocol.Core`, `ModelContextProtocol.AspNetCore` = **2.2.0**.
- Library uses the SDK attributes `ModelContextProtocol.Server.McpServerToolTypeAttribute` and `ModelContextProtocol.Server.McpServerToolAttribute` (own copies deleted). SDK attribute members: `Name`, `Title`, `Destructive`, `Idempotent`, `OpenWorld`, `ReadOnly`, `UseStructuredContent`, `OutputSchemaType`, `IconSource`.
- Authorization is delegated to the SDK: `builder.AddAuthorizationFilters()` (namespace `ModelContextProtocol.AspNetCore`) evaluates `[Authorize]`/`[AllowAnonymous]` found in `McpServerTool.Metadata` through `IAuthorizationPolicyProvider`/`IAuthorizationService`, filters `tools/list` per user and rejects `tools/call` with `McpProtocolException("Access forbidden...")`. The SDK throws `InvalidOperationException` at list time if tools carry authorization metadata but the filter was never registered.
- Tools are created via `Microsoft.Extensions.AI.AIFunctionFactory.Create(method, createTarget, new AIFunctionFactoryOptions { Name, MarshalResult, SerializerOptions })` then `McpServerTool.Create(aiFunction, new McpServerToolCreateOptions { Services, Metadata, Title, Destructive, Idempotent, OpenWorld, ReadOnly, UseStructuredContent, OutputSchema, Icons, SerializerOptions })`. The AIFunction overload does NOT read attributes, so the library builds `Metadata` and the options itself. Metadata layout mirrors the SDK: `[MethodInfo, ...class attributes, ...method attributes]`.
- SDK filter API: `builder.WithRequestFilters(f => f.AddListToolsFilter(next => async (context, ct) => { var r = await next(context, ct); ...; return r; }))`. Filter context type `RequestContext<ListToolsRequestParams>` exposes `User` (ClaimsPrincipal?), `Services`, `Items`, `Params`. `ListToolsResult` has `Tools`, `TimeToLive` (TimeSpan?), `CacheScope` (enum `ModelContextProtocol.Protocol.CacheScope { Public, Private }`).
- HTTP transport: `WithHttpTransport(o => o.SessionMode = HttpServerSessionMode.Stateless|Stateful|StatefulForInitializeClients)`; default Stateless. `MapMcp(pattern)` unchanged.
- Verified in block 07: the SDK emits `outputSchema` ONLY when `UseStructuredContent = true` (AIFunctionMcpServerTool.CreateOutputSchema), so a tool must declare `[McpServerTool(UseStructuredContent = true, OutputSchemaType = typeof(T))]`; the demo `GetById` does. Stateless mode DOES flow the HTTP request context into tools (IsMcpCall is true inside tools); demo `Program.cs` reads `Mcp:SessionMode` config to switch modes (used by TransportModeTests).
- Removed public types: `IAuthForMcpSupplier`, `McpAuthorizationPreFilter`, `ToolAuthorizationMetadata`, `IToolAuthorizationStore`, `ToolAuthorizationStore`, `ToolListFilter`, `IUserRoleResolver`, `ZeroMcpOptions.FilterToolsByPermissions`, own `McpServerToolTypeAttribute`/`McpServerToolAttribute`. Kept: `ZeroMcpOptions.UseAuthorization` (false = authorization metadata is NOT attached to tools and `AddAuthorizationFilters` is NOT registered, so the demo `Auth:Enabled=false` mode still works).
- New: `ZeroMcpOptions.ToolsListTimeToLive` (TimeSpan?, default null) → when set, `tools/list` responses carry `TimeToLive` and `CacheScope.Private` when `UseAuthorization` is true, `CacheScope.Public` otherwise.
- Test stack: `xunit.v3` 4.0.1 + `xunit.runner.visualstudio` 4.0.0 + `Microsoft.NET.Test.Sdk` 18.10.0 (kept for IDE), `AwesomeAssertions` 9.6.0 (namespace is `AwesomeAssertions` since 9.x — every test file uses `using AwesomeAssertions;`, the `.Should()` API is unchanged), `NSubstitute` 6.2.0 (Moq removed). xunit.v3 differences that apply to ALL test code: `IAsyncLifetime` members return `ValueTask` (`public async ValueTask InitializeAsync()` / `DisposeAsync()`), `ITestOutputHelper` lives in `Xunit` (no `Xunit.Abstractions`), and the runner is selected in `global.json` (`"test": { "runner": "Microsoft.Testing.Platform" }`). Run tests with `dotnet test --project <csproj-dir>`; never pass `--nologo` to `dotnet test` because it is forwarded to the test host which rejects it (exit code 5). NSubstitute: when one argument is a matcher, every argument of the call must be a matcher (`Arg.Any<object?>()` instead of a raw `null`).
- Dependency versions: Microsoft.AspNetCore.* / Microsoft.Extensions.* 10.0.12, `Scalar.AspNetCore` 2.17.3, `Microsoft.SourceLink.GitHub` 10.0.401, `Serilog.AspNetCore` 10.0.0 and `Serilog.Sinks.File` 7.0.0 unchanged. `global.json`: `"version": "10.0.100", "rollForward": "latestFeature"`.
- Version bump to **3.0.0** in `Version.props`.
- Lint gate (j-settings.md): `Directory.Build.props` currently has `TreatWarningsAsErrors=false` with a `TODO(lint-debt)` comment and `WarningsNotAsErrors` for NU190x; `.editorconfig` has a `TODO(lint-debt)` block downgrading CA2007 and IDE0005. Both temporary blocks are removed in the last block once code is clean.

## Conventions
- Tests: xUnit `[Fact]`/`[Theory]`, names `Should_<Outcome>_When<Condition>`, AwesomeAssertions `.Should()`, NSubstitute `Substitute.For<T>()`, `async Task` never `async void`.
- Nullable enabled; records for DTOs; primary constructors allowed; `ConfigureAwait(false)` in library code (CA2007); logging via `LoggerMessage` source-generated methods (CA1848); culture-invariant string comparisons (CA1310).
- Commands (from j-settings.md): build `dotnet build`, tests `dotnet test`, lint `dotnet format --verify-no-changes` + `ast-grep scan` via `.claude/support/lint/lint-run.sh` (ast-grep absent → skip with warning). Run E2E tests with Keycloak up.
- Commit after each block on branch `feature/laco/upgrade`, message `feat|refactor|test|chore(scope): ...`, ending with the Co-Authored-By/Claude-Session trailers used in this repo.
</mission>

<block id="01-migrate-test-stack-and-deps">
## TDDAB-1: Migrate test stack to xunit.v3 + AwesomeAssertions + NSubstitute and bump platform dependencies

<intro>
Replaces the test toolchain and bumps every non-MCP dependency while keeping the MCP SDK at 0.6.0-preview.1 (so the library still compiles; the SDK moves in block 03). Also moves the vendored `3rdp/csharp-sdk` checkout to tag v2.2.0 so the solution reflects the SDK we are targeting.
Files: `Directory.Packages.props`, `global.json`, `tests/Zero.Mcp.Extensions.Tests/Zero.Mcp.Extensions.Tests.csproj`, `tests/McpPoc.Api.Tests/McpPoc.Api.Tests.csproj`, `tests/McpPoc.Api.Tests/Usings.cs`, and the 5 Moq-based test files: `tests/McpPoc.Api.Tests/KeycloakAuthSupplierTests.cs`, `tests/Zero.Mcp.Extensions.Tests/McpAuthorizationPreFilterTests.cs`, `McpMiddlewareTests.cs`, `McpRequestContextTests.cs`, `McpServerBuilderExtensionsTests.cs`.
The RED tests are the Moq → NSubstitute rewrites: they must compile against NSubstitute and fail to compile while Moq is still referenced (package swap is the GREEN).
</intro>

<red>
- test: every existing test in tests/Zero.Mcp.Extensions.Tests that used `new Mock<T>()` / `.Setup()` / `.Verify()` is rewritten with `Substitute.For<T>()`, `.Returns()`, `.Received()` and keeps its original assertions
- test: `KeycloakAuthSupplierTests` rewritten with NSubstitute for `IHttpContextAccessor` and `IAuthorizationService`, same assertions
- test: `McpRequestContextTests.IsMcpCall_ReturnsFalse_WhenHttpContextIsNull` uses `Substitute.For<IHttpContextAccessor>()` returning null `HttpContext`
- test: a new `tests/Zero.Mcp.Extensions.Tests/TestStackSmokeTests.cs` with `Should_RunUnderXunitV3_WhenExecuted` asserting `typeof(FactAttribute).Assembly.GetName().Name == "xunit.v3.core"` and `Should_UseAwesomeAssertions_WhenAsserting` asserting `typeof(AwesomeAssertions.AssertionExtensions).Assembly.GetName().Name == "AwesomeAssertions"`
- test: E2E expectations corrected to the real tool names of the mission: `"get_by_id"` → `"UserGetById"` in `tests/McpPoc.Api.Tests/McpToolDiscoveryTests.cs`, `McpToolInvocationTests.cs`, `PolicyAuthorizationTests.cs`, `ToolNamingTests.cs`, `ToolVisibilityTests.cs`; `"echo_headers"` added to `ToolVisibilityTests.BaseTools` (viewer 6, member 7, manager 8, admin 9) and `McpToolDiscoveryTests` member count becomes 7
- test: full suite (unit + E2E with Keycloak up) passes under `dotnet test` with the xunit.v3 runner — every previously existing test green after the expectation fixes, plus the 2 smoke tests
</red>

### Implementation
`Directory.Packages.props` (full replacement of the version list):
```xml
<PackageVersion Include="ModelContextProtocol" Version="0.6.0-preview.1" />           <!-- moves to 2.2.0 in block 03 -->
<PackageVersion Include="ModelContextProtocol.Core" Version="0.6.0-preview.1" />
<PackageVersion Include="ModelContextProtocol.AspNetCore" Version="0.6.0-preview.1" />
<PackageVersion Include="Microsoft.AspNetCore.OpenApi" Version="10.0.12" />
<PackageVersion Include="Scalar.AspNetCore" Version="2.17.3" />
<PackageVersion Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="10.0.12" />
<PackageVersion Include="Microsoft.AspNetCore.Authorization" Version="10.0.12" />
<PackageVersion Include="Microsoft.Extensions.Logging.Abstractions" Version="10.0.12" />
<PackageVersion Include="Microsoft.Extensions.DependencyInjection.Abstractions" Version="10.0.12" />
<PackageVersion Include="Serilog.AspNetCore" Version="10.0.0" />
<PackageVersion Include="Serilog.Sinks.File" Version="7.0.0" />
<PackageVersion Include="Microsoft.SourceLink.GitHub" Version="10.0.401" />
<PackageVersion Include="Microsoft.NET.Test.Sdk" Version="18.10.0" />
<PackageVersion Include="xunit.v3" Version="4.0.1" />
<PackageVersion Include="xunit.runner.visualstudio" Version="4.0.0" />
<PackageVersion Include="AwesomeAssertions" Version="9.6.0" />
<PackageVersion Include="NSubstitute" Version="6.2.0" />
<PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="10.0.12" />
<PackageVersion Include="Microsoft.AspNetCore.TestHost" Version="10.0.12" />
```
Both test csproj: replace `xunit` → `xunit.v3`, `FluentAssertions` → `AwesomeAssertions`, `Moq` → `NSubstitute`; keep `Microsoft.NET.Test.Sdk` and `xunit.runner.visualstudio`. Add `<OutputType>Exe</OutputType>` (required by xunit.v3), `<UseMicrosoftTestingPlatformRunner>true</UseMicrosoftTestingPlatformRunner>` and `<GenerateProgramFile>false</GenerateProgramFile>` (xunit.v3 supplies the entry point; Microsoft.NET.Test.Sdk must not generate a second `Program`, otherwise CS0017).
E2E expectation fixes (stale since v2.1.0 gave `GetById` the explicit name and added `EchoHeaders`): replace the string `"get_by_id"` with `"UserGetById"` in the five files listed in the red section; `ToolVisibilityTests.BaseTools = ["UserGetById", "get_all", "get_scope_id", "get_public_info", "get_mcp_context", "echo_headers"]`; `McpToolDiscoveryTests` member assertion `HaveCount(7, "Member should see 6 base tools + create")`.
`global.json`: `{ "sdk": { "version": "10.0.100", "rollForward": "latestFeature" } }`.
NSubstitute pattern used in rewrites:
```csharp
var accessor = Substitute.For<IHttpContextAccessor>();
accessor.HttpContext.Returns(httpContext);          // was mock.Setup(a => a.HttpContext).Returns(httpContext)
var supplier = Substitute.For<IAuthForMcpSupplier>();
supplier.CheckAuthenticatedAsync().Returns(Task.FromResult(true));
await supplier.Received(1).CheckPolicyAsync(Arg.Any<AuthorizeAttribute>());   // was mock.Verify(..., Times.Once)
```
3rdp: `cd 3rdp/csharp-sdk && git fetch --tags && git checkout v2.2.0`. The folder is recorded in the parent repo as a gitlink (mode 160000, no .gitmodules entry): after the checkout run `git add 3rdp/csharp-sdk` in the parent so the pointer moves to the v2.2.0 commit.
If the SDK bump of the vendored folder makes `dotnet build` of the whole .slnx fail on 3rdp projects only, that is pre-existing and out of the gate: the gate builds `src/` and `tests/` projects (`dotnet build src/Zero.Mcp.Extensions src/McpPoc.Api tests/Zero.Mcp.Extensions.Tests tests/McpPoc.Api.Tests`).

<success>
- [x] No reference to `Moq`, `FluentAssertions`, or `xunit` v2 packages remains in csproj/props, and no `using FluentAssertions;` remains in tests
- [x] `TestStackSmokeTests` both green
- [x] Every pre-existing test green after the tool-name expectation fixes, E2E included with Keycloak running (`docker compose -f docker/docker-compose.yml up -d`)
- [x] `dotnet list package --vulnerable` reports no high-severity package for src/ and tests/ except the MCP SDK line (handled in block 03)
- [x] `3rdp/csharp-sdk` is at tag v2.2.0
- [x] Green-gate BTLT passes — build + tests + lint + typecheck (configured commands, skip n/a)
</success>
</block>

<block id="02-tool-metadata-builder">
## TDDAB-2: Add ToolMetadataBuilder producing SDK-compatible tool metadata

<intro>
Pure helper, no SDK version dependency. Builds the `IReadOnlyList<object>` the SDK's `AddAuthorizationFilters()` inspects: `[MethodInfo, ...declaring-class attributes, ...method attributes]`, with an option to strip authorization metadata (`IAuthorizeData`, `IAllowAnonymous`, `AuthorizationPolicy`, `IAuthorizationRequirementData`) when `UseAuthorization` is false.
Files: new `src/Zero.Mcp.Extensions/ToolMetadataBuilder.cs`, new `tests/Zero.Mcp.Extensions.Tests/ToolMetadataBuilderTests.cs`.
</intro>

<red>
- test: Should_PutMethodInfoFirst_WhenBuilt — first element is the `MethodInfo` passed in
- test: Should_IncludeClassAttributesBeforeMethodAttributes_WhenBothPresent — class `[Authorize]` appears before method `[Authorize(Policy="RequireMember")]`
- test: Should_IncludeAllowAnonymous_WhenMethodHasIt — contains an `AllowAnonymousAttribute` instance
- test: Should_IncludeNonAuthorizationAttributes_WhenPresent — `[Description]` and `[HttpGet]` attributes are included
- test: Should_StripAuthorizationMetadata_WhenIncludeAuthorizationIsFalse — result contains no `IAuthorizeData` and no `IAllowAnonymous` but still contains MethodInfo and `[Description]`
- test: Should_ReturnOnlyMethodInfo_WhenNoAttributes — a method on a bare class yields exactly one element
- test: Should_ThrowArgumentNull_WhenMethodIsNull
</red>

### Implementation
```csharp
namespace Zero.Mcp.Extensions;

internal static class ToolMetadataBuilder
{
    public static IReadOnlyList<object> Build(MethodInfo method, bool includeAuthorization)
    {
        ArgumentNullException.ThrowIfNull(method);
        List<object> metadata = [method];
        if (method.DeclaringType is not null)
            metadata.AddRange(method.DeclaringType.GetCustomAttributes(inherit: true));
        metadata.AddRange(method.GetCustomAttributes(inherit: true));
        if (!includeAuthorization)
            metadata.RemoveAll(static m => m is IAuthorizeData or IAllowAnonymous or AuthorizationPolicy or IAuthorizationRequirementData);
        return metadata;
    }
}
```
Test fixtures: a `[Authorize] class SampleController { [Authorize(Policy="RequireMember")][Description("d")] public void Create(){} [AllowAnonymous] public void Info(){} }` and `class Bare { public void Plain(){} }` inside the test file. Assert with `result[0].Should().BeSameAs(method)`, `result.OfType<AuthorizeAttribute>().Select(a => a.Policy).Should().ContainInOrder(null, "RequireMember")`.

<success>
- [x] All 7 tests green
- [x] `ToolMetadataBuilder` is `internal static`, exposed to tests through the existing `InternalsVisibleTo` (add `<InternalsVisibleTo Include="Zero.Mcp.Extensions.Tests" />` to the library csproj if absent)
- [x] Green-gate BTLT passes — build + tests + lint + typecheck (configured commands, skip n/a)
</success>
</block>

<block id="03-upgrade-sdk-and-adopt-sdk-attributes">
## TDDAB-3: Upgrade MCP SDK to 2.2.0, adopt SDK attributes, migrate filter API

<intro>
Moves the three `ModelContextProtocol*` packages to 2.2.0 and makes the library compile again with behaviour unchanged: own attribute classes deleted, scanning and `ToolNameGenerator` read `ModelContextProtocol.Server.McpServerToolTypeAttribute`/`McpServerToolAttribute`, the `tools/list` filter is re-registered through `WithRequestFilters`. The custom authorization (pre-filter + role store) is still in place after this block; it is replaced in block 05.
Files: `Directory.Packages.props`, `src/Zero.Mcp.Extensions/McpServerBuilderExtensions.cs` (delete the two attribute classes at the bottom; rewrite filter registration), `src/Zero.Mcp.Extensions/ToolNameGenerator.cs`, `src/McpPoc.Api/Controllers/UsersController.cs` (add `using ModelContextProtocol.Server;`; the diagnostics endpoint at ~line 191 reflects over the attribute types and must reference the SDK ones), `tests/Zero.Mcp.Extensions.Tests/ToolNameGeneratorTests.cs`, `tests/Zero.Mcp.Extensions.Tests/McpServerBuilderExtensionsTests.cs`.
</intro>

<red>
- test: ToolNameGeneratorTests.Should_UseExplicitName_WhenSdkAttributeNameIsSet — a method decorated with `[ModelContextProtocol.Server.McpServerTool(Name = "UserGetById")]` yields "UserGetById" regardless of convention
- test: ToolNameGeneratorTests.Should_UseConvention_WhenSdkAttributeNameIsNull — `[McpServerTool]` without Name → snake_case method name (MethodOnly) or `users_get_by_id` (ControllerPrefix)
- test: McpServerBuilderExtensionsTests.Should_RegisterOneMcpServerToolPerAttributedMethod_WhenScanningSdkAttributes — assembly with a `[McpServerToolType]` class having 3 `[McpServerTool]` methods (SDK attributes) registers 3 `McpServerTool` singletons
- test: McpServerBuilderExtensionsTests.Should_IgnoreMethods_WhenSdkToolAttributeIsMissing
- test: McpServerBuilderExtensionsTests.Should_RegisterListToolsFilter_WhenFilterToolsByPermissionsIsTrue — after `AddZeroMcpExtensions(o => o.FilterToolsByPermissions = true)`, resolving `IOptions<McpServerOptions>` yields `Value.Filters.Request.ListToolsFilters.Count == 2` (the SDK's own list-guard filter installed by `WithHttpTransport` plus ours); with `FilterToolsByPermissions = false` the count is exactly 1 (a bare `>= 1` would never fail because the SDK guard is always present)
- test: the library has no type named `Zero.Mcp.Extensions.McpServerToolAttribute` or `Zero.Mcp.Extensions.McpServerToolTypeAttribute` (`typeof(ZeroMcpOptions).Assembly.GetType("Zero.Mcp.Extensions.McpServerToolAttribute").Should().BeNull()`)
- test: E2E regression net (ToolNamingTests, McpToolDiscoveryTests, McpToolInvocationTests, PolicyAuthorizationTests, ToolVisibilityTests) unchanged and green
</red>

### Implementation
`Directory.Packages.props`: the three `ModelContextProtocol*` lines → `Version="2.2.0"`.
`McpServerBuilderExtensions.cs`:
```csharp
using ModelContextProtocol.Server;   // SDK attributes
// scanning
var toolTypes = toolAssembly.GetTypes().Where(t => t.GetCustomAttribute<McpServerToolTypeAttribute>() is not null);
var toolMethods = toolType.GetMethods(...).Where(m => m.GetCustomAttribute<McpServerToolAttribute>() is not null);
// filter registration (replaces builder.AddListToolsFilter)
if (options.FilterToolsByPermissions)
{
    builder.WithRequestFilters(filters => filters.AddListToolsFilter(next => async (context, cancellationToken) =>
    {
        var result = await next(context, cancellationToken).ConfigureAwait(false);
        var store = context.Services?.GetRequiredService<IToolAuthorizationStore>();
        var roleResolver = context.Services?.GetService<IUserRoleResolver>();
        int? userRole = roleResolver is not null && context.User is not null
            ? await roleResolver.GetUserRoleAsync(context.User).ConfigureAwait(false)
            : ToolListFilter.GetUserRole(context.User);
        var allowed = ToolListFilter.FilterByRole(result.Tools.Select(t => t.Name), userRole, store).ToHashSet(StringComparer.Ordinal);
        result.Tools = result.Tools.Where(t => allowed.Contains(t.Name)).ToList();
        return result;
    }));
}
```
Delete the `McpServerToolTypeAttribute` and `McpServerToolAttribute` classes from the file. `ToolNameGenerator.GenerateName`: `method.GetCustomAttribute<ModelContextProtocol.Server.McpServerToolAttribute>()?.Name` is the explicit-name source. `UsersController.cs`: `using ModelContextProtocol.Server;` and the diagnostics reflection uses `typeof(McpServerToolTypeAttribute)` from that namespace. In the `McpServerTool.Create` call keep `new McpServerToolCreateOptions { Services = services, SerializerOptions = serializerOptions }`.
Compile guard: `CallToolResult`/`ListToolsResult` construction uses `Tools = [...]` list; `Tool.Name` is `required` in 2.2.0 — only relevant if tests construct `Tool` objects (`new Tool { Name = "x" }`).

<success>
- [x] `dotnet build` of src/ and tests/ succeeds against ModelContextProtocol 2.2.0
- [x] Own attribute classes gone; demo compiles with SDK attributes
- [x] Unit + E2E suites green (Keycloak up)
- [x] `dotnet list package --vulnerable` clean for src/ and tests/
- [x] Green-gate BTLT passes — build + tests + lint + typecheck (configured commands, skip n/a)
</success>
</block>

<block id="04-tool-create-options-factory">
## TDDAB-4: Add ToolCreateOptionsFactory deriving McpServerToolCreateOptions from SDK attributes

<intro>
The AIFunction overload of `McpServerTool.Create` ignores attributes, so the library must derive `Title`, `Destructive`, `Idempotent`, `OpenWorld`, `ReadOnly`, `UseStructuredContent`, `OutputSchema` (from `OutputSchemaType`), `Icons` (from `IconSource`) and `Description` itself, plus `Metadata` from block 02. Mirrors the SDK's internal `AIFunctionMcpServerTool.DeriveOptions`. Depends on blocks 02 and 03.
Ordering constraint: in THIS block the factory is wired with `includeAuthorization: false` (constant). SDK 2.2.0 `WithHttpTransport` installs guard filters (`AuthorizationFilterSetup.CheckListToolsFilter`, `AuthorizationCallToolFilterGuardSetup`) that throw `InvalidOperationException` on `tools/list` and `tools/call` whenever a tool's `Metadata` carries `IAuthorizeData` without `IAllowAnonymous` and `AddAuthorizationFilters()` was never called. `UsersController` has a class-level `[Authorize]`, so attaching authorization metadata before block 05 registers the SDK filters would break every E2E call. Block 05 flips the argument to `options.UseAuthorization`.
Files: new `src/Zero.Mcp.Extensions/ToolCreateOptionsFactory.cs`, new `tests/Zero.Mcp.Extensions.Tests/ToolCreateOptionsFactoryTests.cs`, `src/Zero.Mcp.Extensions/McpServerBuilderExtensions.cs` (use the factory in both static and instance registration paths).
</intro>

<red>
- test: Should_SetTitle_WhenAttributeHasTitle
- test: Should_LeaveHintsNull_WhenAttributeDoesNotSetThem — Destructive/Idempotent/OpenWorld/ReadOnly are null (SDK defaults apply)
- test: Should_SetReadOnlyTrue_WhenAttributeReadOnlyIsTrue — `[McpServerTool(ReadOnly = true)]` → `ReadOnly == true`
- test: Should_SetDestructiveFalse_WhenAttributeDestructiveIsFalse
- test: Should_SetUseStructuredContent_WhenAttributeSetsIt
- test: Should_CreateOutputSchemaFromType_WhenOutputSchemaTypeIsSet — `[McpServerTool(OutputSchemaType = typeof(User))]` on a method returning `ActionResult<User>` yields an `OutputSchema` JsonElement whose `properties` contain `id` and `name` (snake_case serializer options passed in)
- test: Should_LeaveOutputSchemaNull_WhenOutputSchemaTypeIsNull
- test: Should_SetIcons_WhenIconSourceIsSet — one `Icon` with `Source == "https://x/icon.png"`
- test: Should_SetDescription_WhenDescriptionAttributePresent
- test: Should_UseSdkMetadataLayout_WhenBuilt — `Metadata![0]` is the MethodInfo and contains the class `[Authorize]`
- test: Should_StripAuthorizationMetadata_WhenIncludeAuthorizationIsFalse
- test: Should_SetServicesAndSerializerOptions_WhenProvided
- test: McpServerBuilderExtensionsTests.Should_AttachMetadataWithoutAuthorization_WhenToolsRegistered — after `AddZeroMcpExtensions(o => o.UseAuthorization = true)` every registered `McpServerTool.Metadata` has the `MethodInfo` first, contains the `[Description]` attribute, and contains no `IAuthorizeData` (the SDK guard filters stay silent until block 05)
</red>

### Implementation
```csharp
namespace Zero.Mcp.Extensions;

internal static class ToolCreateOptionsFactory
{
    public static McpServerToolCreateOptions Create(MethodInfo method, IServiceProvider services, JsonSerializerOptions serializerOptions, bool includeAuthorization)
    {
        var attr = method.GetCustomAttribute<McpServerToolAttribute>();
        var options = new McpServerToolCreateOptions
        {
            Services = services,
            SerializerOptions = serializerOptions,
            Metadata = ToolMetadataBuilder.Build(method, includeAuthorization),
            Description = method.GetCustomAttribute<DescriptionAttribute>()?.Description,
        };
        if (attr is null) return options;
        options.Title = attr.Title;
        options.UseStructuredContent = attr.UseStructuredContent;
        // The SDK attribute exposes the hints as non-nullable bool with "was it set" tracked privately;
        // read them through the public properties only when they differ from SDK defaults
        // (Destructive default true, Idempotent false, OpenWorld true, ReadOnly false).
        if (attr.Destructive != true) options.Destructive = attr.Destructive;
        if (attr.Idempotent) options.Idempotent = true;
        if (attr.OpenWorld != true) options.OpenWorld = attr.OpenWorld;
        if (attr.ReadOnly) options.ReadOnly = true;
        if (attr.OutputSchemaType is { } schemaType)
            options.OutputSchema = AIJsonUtilities.CreateJsonSchema(schemaType, serializerOptions: serializerOptions);
        if (!string.IsNullOrEmpty(attr.IconSource))
            options.Icons = [new Icon { Source = attr.IconSource }];
        return options;
    }
}
```
`McpServerBuilderExtensions`: both registration lambdas become `McpServerTool.Create(aiFunction, ToolCreateOptionsFactory.Create(method, services, serializerOptions, includeAuthorization: false))` — the literal `false` is intentional in this block (see intro); block 05 replaces it with `options.UseAuthorization`. `AIJsonUtilities` is in `Microsoft.Extensions.AI` (already transitively referenced). Assert schema with `options.OutputSchema!.Value.GetProperty("properties").TryGetProperty("id", out _).Should().BeTrue()`.

<success>
- [x] All 13 tests green
- [x] No registered `McpServerTool.Metadata` contains `IAuthorizeData` after this block (SDK guard filters must not fire)
- [x] E2E `McpToolDiscoveryTests`, `McpToolInvocationTests`, `PolicyAuthorizationTests` still green (Keycloak up) and tool descriptions unchanged
- [x] Green-gate BTLT passes — build + tests + lint + typecheck (configured commands, skip n/a)
</success>
</block>

<block id="05-replace-custom-auth-with-sdk-filters">
## TDDAB-5: Replace custom authorization with SDK AddAuthorizationFilters

<intro>
Deletes the custom authorization layer and wires `builder.AddAuthorizationFilters()` when `UseAuthorization` is true. Controller instances are now created without a pre-filter; `[Authorize]`/`[AllowAnonymous]` are enforced by the SDK from the metadata produced by the block 04 factory, whose `includeAuthorization` argument switches in this block from the literal `false` to `options.UseAuthorization`. Removes `FilterToolsByPermissions` (the SDK filter always applies when authorization is on). Demo loses `KeycloakAuthSupplier`, `UserRoleResolver` and their registrations. Depends on block 04.
Files: DELETE `src/Zero.Mcp.Extensions/IAuthForMcpSupplier.cs`, `McpAuthorizationPreFilter.cs`, `ToolAuthorizationMetadata.cs`, `ToolListFilter.cs`; DELETE `src/McpPoc.Api/Infrastructure/KeycloakAuthSupplier.cs`, `src/McpPoc.Api/Infrastructure/UserRoleResolver.cs`; DELETE `tests/Zero.Mcp.Extensions.Tests/IAuthForMcpSupplierTests.cs`, `McpAuthorizationPreFilterTests.cs`, `ToolAuthorizationMetadataTests.cs`, `ToolFilteringTests.cs`, `tests/McpPoc.Api.Tests/KeycloakAuthSupplierTests.cs`; MODIFY `src/Zero.Mcp.Extensions/McpServerBuilderExtensions.cs`, `ZeroMcpOptions.cs`, `src/McpPoc.Api/Program.cs`, `tests/Zero.Mcp.Extensions.Tests/McpServerBuilderExtensionsTests.cs`, `McpMiddlewareTests.cs`, `ZeroMcpOptionsTests.cs`, `PackageTests.cs`.
</intro>

<red>
- test: McpServerBuilderExtensionsTests.Should_RegisterSdkAuthorizationFilters_WhenUseAuthorizationIsTrue — after `AddZeroMcpExtensions(o => o.UseAuthorization = true)` plus `services.AddAuthorization()`, resolving `IOptions<McpServerOptions>` yields `Filters.Request.CallToolFilters.Count == 1` (SDK ordinary call-tool authorization checkpoint) and `ListToolsFilters.Count == 2` (SDK authorization list filter + SDK list guard); with `UseAuthorization = false` the counts are 0 and 1
- test: McpServerBuilderExtensionsTests.Should_NotAttachAuthorizationMetadata_WhenUseAuthorizationIsFalse — every registered `McpServerTool.Metadata` contains no `IAuthorizeData`
- test: McpServerBuilderExtensionsTests.Should_AttachAuthorizationMetadata_WhenUseAuthorizationIsTrue — a tool for `[Authorize(Policy="RequireMember")]` method has metadata with that policy
- test: McpServerBuilderExtensionsTests.Should_CreateControllerThroughDI_WhenToolInvoked — `ActivatorUtilities` path still resolves constructor dependencies (a fake `IUserService` substitute is injected)
- test: ZeroMcpOptionsTests: `FilterToolsByPermissions` property no longer exists (reflection assert `typeof(ZeroMcpOptions).GetProperty("FilterToolsByPermissions").Should().BeNull()`)
- test: the library assembly exposes none of: `IAuthForMcpSupplier`, `IUserRoleResolver`, `ToolListFilter`, `ToolAuthorizationMetadata`, `IToolAuthorizationStore` (reflection asserts in PackageTests)
- test: E2E PolicyAuthorizationTests — admin can call all tools; member can call `create` but `tools/call` `update` is rejected by the SDK filter with a JSON-RPC error, which the SDK client surfaces as a thrown `McpProtocolException` whose message contains "Access forbidden" (no `CallToolResult` is returned); viewer calling `create` gets the same exception; viewer calling `get_public_info` (`[AllowAnonymous]`) succeeds
- test: E2E ToolVisibilityTests — `tools/list` for viewer returns exactly the 6 base tools (UserGetById, get_all, get_scope_id, get_public_info, get_mcp_context, echo_headers), for member adds create (7), for manager adds update (8), for admin includes all 9 (the `/mcp` endpoint itself still requires a valid bearer token via `RequireAuthentication`, so there is no unauthenticated list case)
</red>

### Implementation
`ZeroMcpOptions`: remove `FilterToolsByPermissions`; keep `UseAuthorization` (doc: "When true, [Authorize]/[AllowAnonymous] on controllers are enforced by the MCP SDK authorization filters; requires the host to call AddAuthorization().").
`McpServerBuilderExtensions.AddZeroMcpExtensions`:
```csharp
var mcpBuilder = services.AddMcpServer()
    .WithHttpTransport()
    .WithToolsFromAssemblyUnwrappingActionResult(options);
if (options.UseAuthorization)
    mcpBuilder.AddAuthorizationFilters();
return mcpBuilder;
```
`WithToolsFromAssemblyUnwrappingActionResult`: remove `ToolAuthorizationStore` creation, the list filter block, and `CreateControllerWithPreFilter`; both registration paths now call `ToolCreateOptionsFactory.Create(method, services, serializerOptions, options.UseAuthorization)` (was the literal `false` from block 04); the instance path becomes
```csharp
AIFunctionFactory.Create(method,
    args => ActivatorUtilities.CreateInstance(args.Services!, toolType),
    new AIFunctionFactoryOptions { Name = toolName, MarshalResult = static async (r, _, _) => await MarshalResult.UnwrapAsync(r).ConfigureAwait(false), SerializerOptions = serializerOptions });
```
`Program.cs`: delete the `AddScoped<IAuthForMcpSupplier,...>` and `AddScoped<IUserRoleResolver,...>` lines and the `FilterToolsByPermissions` assignment. Keep `AddMcpPocAuthorization()` (policies) and `builder.Services.AddAuthorization()` in both branches.
E2E error assertion: the custom pre-filter used to throw inside the tool, which the SDK turned into a `CallToolResult { IsError = true }`; the SDK authorization filter instead throws `McpProtocolException("Access forbidden: This tool requires authorization.", McpErrorCode.InvalidRequest)` in the request pipeline, so the server answers with a JSON-RPC error and `McpClient.CallToolAsync` (called unwrapped by `McpClientHelper.CallToolAsync`) throws `ModelContextProtocol.McpProtocolException` on the client. Rewrite the forbidden cases in `tests/McpPoc.Api.Tests/PolicyAuthorizationTests.cs` (Member→update, Manager→promote_to_manager, Member→promote_to_manager, Viewer→create) as:
```csharp
Func<Task> act = () => _memberClient.CallToolAsync("update", args);
await act.Should().ThrowAsync<McpProtocolException>().WithMessage("*Access forbidden*");
```
The allowed cases keep `result.IsError.Should().NotBe(true)`.

<success>
- [x] The four library files and two demo files are deleted; library public surface reduced accordingly
- [x] Unit tests green; E2E PolicyAuthorizationTests + ToolVisibilityTests green with Keycloak
- [x] `Auth:Enabled=false` run of the demo lists and invokes every tool without a token (manual check: `dotnet run --project src/McpPoc.Api` with `Auth__Enabled=false`, `tools/list` returns all 9 tools)
- [x] Green-gate BTLT passes — build + tests + lint + typecheck (configured commands, skip n/a)
</success>
</block>

<block id="06-tools-list-cache-hints">
## TDDAB-6: Add tools/list cache hints (TimeToLive + CacheScope)

<intro>
Adds `ZeroMcpOptions.ToolsListTimeToLive` (TimeSpan?, default null). When set, a `WithRequestFilters` list-tools filter stamps `TimeToLive` and `CacheScope` on the `ListToolsResult`: `Private` when `UseAuthorization` is true (list varies per user), `Public` otherwise. Registered as the LAST filter so it wraps the SDK authorization filter output. Depends on block 05.
Files: `src/Zero.Mcp.Extensions/ZeroMcpOptions.cs`, `src/Zero.Mcp.Extensions/McpServerBuilderExtensions.cs`, new `src/Zero.Mcp.Extensions/ToolsListCacheHintFilter.cs`, new `tests/Zero.Mcp.Extensions.Tests/ToolsListCacheHintFilterTests.cs`, `tests/McpPoc.Api.Tests/McpToolDiscoveryTests.cs`.
</intro>

<red>
- test: Should_LeaveResultUntouched_WhenTimeToLiveIsNull — `TimeToLive` and `CacheScope` stay null
- test: Should_SetTimeToLive_WhenConfigured — `TimeSpan.FromMinutes(5)` appears on the result
- test: Should_SetCacheScopePrivate_WhenUseAuthorizationIsTrue
- test: Should_SetCacheScopePublic_WhenUseAuthorizationIsFalse
- test: Should_CallNext_ExactlyOnce — a counting `next` delegate is invoked once and its tools list is returned unchanged
- test: E2E McpToolDiscoveryTests.Should_ReturnTtlAndPrivateScope_WhenConfigured — demo configures `ToolsListTimeToLive = TimeSpan.FromMinutes(5)`; raw JSON of `tools/list` contains `"ttlMs":300000` and `"cacheScope":"private"`
</red>

### Implementation
```csharp
internal static class ToolsListCacheHintFilter
{
    public static McpRequestHandler<ListToolsRequestParams, ListToolsResult> Apply(
        McpRequestHandler<ListToolsRequestParams, ListToolsResult> next, TimeSpan? ttl, bool useAuthorization) =>
        async (context, ct) =>
        {
            var result = await next(context, ct).ConfigureAwait(false);
            if (ttl is { } t)
            {
                result.TimeToLive = t;
                result.CacheScope = useAuthorization ? CacheScope.Private : CacheScope.Public;
            }
            return result;
        };
}
// registration (after AddAuthorizationFilters):
if (options.ToolsListTimeToLive is not null)
    mcpBuilder.WithRequestFilters(f => f.AddListToolsFilter(next => ToolsListCacheHintFilter.Apply(next, options.ToolsListTimeToLive, options.UseAuthorization)));
```
The pure part is extracted as `internal static void Stamp(ListToolsResult result, TimeSpan? ttl, bool useAuthorization)` and `Apply` calls it after `next`. Unit tests target `Stamp` with `new ListToolsResult { Tools = [new Tool { Name = "a" }] }`; the "calls next exactly once" test targets `Apply` with a counting `next` lambda and a context obtained from a real `McpServer` created over an in-memory `Pipe` transport: `var server = McpServer.Create(new StreamServerTransport(clientToServer.Reader.AsStream(), serverToClient.Writer.AsStream()), new McpServerOptions());` then `new RequestContext<ListToolsRequestParams>(server, new JsonRpcRequest { Method = "tools/list" })` — note `serverOptions` is a required parameter of `McpServer.Create` in 2.2.0 (`Create(ITransport transport, McpServerOptions serverOptions, ILoggerFactory? = null, IServiceProvider? = null)`). `Apply` is also covered end-to-end. Demo `Program.cs`: `options.ToolsListTimeToLive = TimeSpan.FromMinutes(5);`.

<success>
- [x] Unit tests on `Stamp` green; E2E TTL test green
- [x] Hint is absent from `tools/list` JSON when the option is null (existing discovery tests unchanged)
- [x] Green-gate BTLT passes — build + tests + lint + typecheck (configured commands, skip n/a)
</success>
</block>

<block id="07-stateless-and-output-schema-e2e">
## TDDAB-7: Verify stateless transport, IMcpRequestContext and OutputSchemaType end-to-end

<intro>
Locks in behaviour under the SDK 2.2.0 defaults: Streamable HTTP in stateless mode, per-request `HttpContext` (so `IMcpRequestContext` and `UseZeroMcpMarking` still work), and `OutputSchemaType` reaching the client. Adds `ZeroMcpOptions.SessionMode` (default `HttpServerSessionMode.Stateless`) passed to `WithHttpTransport`. Depends on block 06.
Files: `src/Zero.Mcp.Extensions/ZeroMcpOptions.cs`, `McpServerBuilderExtensions.cs`, `src/McpPoc.Api/Controllers/UsersController.cs` (annotate `GetById` with `OutputSchemaType = typeof(User)`), `tests/McpPoc.Api.Tests/McpRequestContextE2ETests.cs`, `McpToolDiscoveryTests.cs`, new `tests/McpPoc.Api.Tests/TransportModeTests.cs`, `tests/Zero.Mcp.Extensions.Tests/ZeroMcpOptionsTests.cs`.
</intro>

<red>
- test: ZeroMcpOptionsTests.Should_DefaultSessionModeToStateless
- test: TransportModeTests.Should_AnswerToolsCall_WithoutSessionHeader_WhenStateless — POST `/mcp` `tools/call` without `Mcp-Session-Id` succeeds with the 2026-07-28 client flow used by `McpClientHelper`
- test: TransportModeTests.Should_ReturnSessionId_WhenSessionModeIsStateful — a `WebApplicationFactory` configured with `SessionMode = Stateful` returns an `Mcp-Session-Id` header on initialize for a legacy-protocol client
- test: McpRequestContextE2ETests.Should_ReportIsMcpCallTrue_WhenInvokedOverStatelessTransport — `GetMcpContext` tool returns `is_mcp_call == true` and the `x-mcp-call` header
- test: McpToolDiscoveryTests.Should_ExposeOutputSchemaOfUser_WhenOutputSchemaTypeIsSet — `tools/list` entry `UserGetById` has `outputSchema.properties.id`
- test: McpToolInvocationTests.Should_ReturnUserJson_WhenGetByIdInvoked — unchanged result content (unwrapping still works under 2.2.0)
</red>

### Implementation
`ZeroMcpOptions.SessionMode { get; set; } = HttpServerSessionMode.Stateless;` and `WithHttpTransport(o => o.SessionMode = options.SessionMode)`. Controller: `[McpServerTool(Name = "UserGetById", OutputSchemaType = typeof(User))]`. `McpClientHelper` already speaks the SDK client; for the stateful test use `ModelContextProtocol.Client.McpClient.CreateAsync` with `HttpClientTransport` against the factory's `HttpClient` and assert the session id is present through the transport (or read the raw initialize response with `HttpClient` and assert header `Mcp-Session-Id`).

<success>
- [x] All new E2E tests green with Keycloak
- [x] Demo still works via Claude Code `.mcp.json` `poc` server after `dotnet run` (manual: `tools/list` returns 9 tools)
- [x] Green-gate BTLT passes — build + tests + lint + typecheck (configured commands, skip n/a)
</success>
</block>

<block id="08-close-lint-debt-and-enable-gate">
## TDDAB-8: Close analyzer debt and re-enable the strict lint gate

<intro>
Fixes the analyzer findings still present in `src/Zero.Mcp.Extensions` after block 05 (the logging-related CA1848/CA1873/CA2263 findings lived in the deleted pre-filter code; the survivors from the 2026-09-14 build are CA2007 `ConfigureAwait(false)`, CA1310 `StringComparison.Ordinal` in `ToolNameGenerator`, IDE0005 unused usings, CA1860 `Count == 0` instead of `Any()`, plus anything new the strict build reports) and re-enables `TreatWarningsAsErrors=true`, removes `WarningsNotAsErrors` for NU190x and the `TODO(lint-debt)` block in `.editorconfig`. The RED here is the build itself: with the strict gate on, `dotnet build` must fail before and pass after. Any log call that still exists in the library goes through a `LoggerMessage` source-generated partial in `src/Zero.Mcp.Extensions/Log.cs`; if none remains, `Log.cs` is not created.
Files: `Directory.Build.props`, `.editorconfig`, `src/Zero.Mcp.Extensions/*.cs`, `j-settings.md` (`@lint-debt` line removed).
</intro>

<red>
- test: with `TreatWarningsAsErrors=true` and the `.editorconfig` TODO block removed, `dotnet build src/Zero.Mcp.Extensions` reports zero warnings and zero errors
- test: `dotnet format --verify-no-changes` exits 0 for the solution's src/ and tests/ projects
- test: `dotnet test` (unit + E2E) still green — behaviour unchanged after logging refactor
</red>

### Implementation
Every `await x` in library code → `.ConfigureAwait(false)`; `EndsWith(...)` → `EndsWith(..., StringComparison.Ordinal)`; `.Any()` on collections → `.Count > 0`; `loggerFactory.CreateLogger(typeof(T))` → `CreateLogger<T>()`. `Directory.Build.props`: `TreatWarningsAsErrors=true`, delete the `WarningsNotAsErrors` line and both TODO comments. `.editorconfig`: delete the trailing TODO block. Test projects: if xunit.v3 analyzers raise warnings, fix them rather than suppress.

<success>
- [x] `dotnet build` of src/ and tests/ with zero warnings under the strict gate
- [x] No `TODO(lint-debt)` text remains in the repo (grep)
- [x] Green-gate BTLT passes — build + tests + lint + typecheck (configured commands, skip n/a)
</success>
</block>

<block id="09-release-3-0-0">
## TDDAB-9: Release Zero.Mcp.Extensions 3.0.0 (version, README, package test, Memory Bank)

<intro>
Bumps `MainVersion` to 3.0.0, rewrites the README sections that describe authorization and attributes, adds a CHANGELOG entry with the breaking changes and migration steps (replace `Zero.Mcp.Extensions` attribute usings with `ModelContextProtocol.Server`; delete `IAuthForMcpSupplier`/`IUserRoleResolver` implementations; call `AddAuthorization()`; `FilterToolsByPermissions` removed; new `ToolsListTimeToLive`, `SessionMode`), and updates the Memory Bank. Depends on block 08.
Files: `Version.props`, `README.md`, new `CHANGELOG.md`, `tests/Zero.Mcp.Extensions.Tests/PackageTests.cs`, `mem-bank-mbel5/activeContext.md`, `progress.md`, `techContext.md`, `systemPatterns.md`.
</intro>

<red>
- test: PackageTests.Should_HaveVersion3_WhenPacked — `typeof(ZeroMcpOptions).Assembly.GetName().Version!.Major == 3`
- test: PackageTests.Should_DependOnSdk2_WhenPacked — `dotnet pack` output nuspec lists `ModelContextProtocol` with version `[2.2.0, )` (or the referenced assembly `ModelContextProtocol.Core` has `Version.Major == 2`)
- test: README Quick Start snippet compiles conceptually: it shows `using ModelContextProtocol.Server;`, `builder.Services.AddAuthorization(...)`, `AddZeroMcpExtensions`, `MapZeroMcp()` and no reference to the removed types (grep assert in a test reading `README.md`: must not contain "IAuthForMcpSupplier" or "IUserRoleResolver")
</red>

### Implementation
`Version.props`: `<MainVersion>3.0.0</MainVersion>`. `CHANGELOG.md` top entry `## 3.0.0 - 2026-09-14` with sections Breaking / Added / Changed / Dependencies. README: update Features (SDK-native authorization, OutputSchemaType, cache hints, stateless), Quick Start (steps 1-3), remove "Role-based tool filtering" custom section, add "Migration from 2.x". Memory Bank: `@version::3.0.0`, `@state::TEST` → set by j-close later; record SDK 2.2.0 patterns in `systemPatterns.md` and dependency table in `techContext.md`.

<success>
- [x] `dotnet pack src/Zero.Mcp.Extensions -c Release` produces `Zero.Mcp.Extensions.3.0.0.nupkg`
- [x] PackageTests green; README grep test green
- [x] Memory Bank files updated and consistent with the code
- [x] Green-gate BTLT passes — build + tests + lint + typecheck (configured commands, skip n/a)
</success>
</block>

## Execution Order
01-migrate-test-stack-and-deps        → no dependencies
02-tool-metadata-builder              → depends on 01 (test stack)
03-upgrade-sdk-and-adopt-sdk-attributes → depends on 01, 02
04-tool-create-options-factory        → depends on 02, 03
05-replace-custom-auth-with-sdk-filters → depends on 04
06-tools-list-cache-hints             → depends on 05
07-stateless-and-output-schema-e2e    → depends on 06
08-close-lint-debt-and-enable-gate    → depends on 07 (all code final)
09-release-3-0-0                      → depends on 08
