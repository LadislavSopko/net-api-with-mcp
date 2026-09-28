# TDDAB Plan: Bugfix 02 — inherited tool methods must carry the scanned controller's [Authorize]
**Date:** 2026-09-28
**Bug notes:** tasks/02-inherited-tool-authorize/bug-notes.md · **Issue:** GitHub #1

<mission>
Bugfix on branch `bugfix/02-inherited-tool-authorize`, repo `D:\Projekty\AI_Works\net-api-with-mcp`
(.NET 10, solution `net-api-with-mcp.slnx`, central package management in `Directory.Packages.props`,
version in `Version.props` via `MainVersion`, currently 3.0.0).

Defect (GitHub issue #1, authorization bypass): `ToolMetadataBuilder.Build` reads class-level
attributes from `method.DeclaringType`. For a tool method INHERITED from a base controller and not
overridden, `DeclaringType` is the base type, and `GetCustomAttributes(inherit: true)` only walks
UPWARD, so an `[Authorize]` declared on the scanned (derived) controller never reaches
`McpServerTool.Metadata`. The SDK authorization filters then enforce only the base policy: a tool
that REST rejects with 403 is listed in `tools/list` and callable over `/mcp` by a less-privileged
user. Runtime proof: with base policy `ReaderOrAbove` and derived policy `AdminOnly`, reading from
`DeclaringType` yields `[ReaderOrAbove]` while reading from the scanned type yields
`[AdminOnly, ReaderOrAbove]`.

Fix: thread the scanned `toolType` through the registration chain and read class attributes from it.
Both affected types are `internal static`, so the public API does not change and this ships as the
PATCH release 3.0.1.

Call sites (verified with vs-mcp FindSymbolUsages, 2026-09-28):
- `ToolMetadataBuilder.Build` → production `ToolCreateOptionsFactory.cs:37`, plus 7 calls in
  `tests/Zero.Mcp.Extensions.Tests/ToolMetadataBuilderTests.cs` (lines 23, 31, 41, 49, 58, 69, 77).
- `ToolCreateOptionsFactory.Create` → production `McpServerBuilderExtensions.cs:107` (static method
  path) and `:129` (instance method path), plus the `Create` helper in
  `tests/Zero.Mcp.Extensions.Tests/ToolCreateOptionsFactoryTests.cs:25`.
- The scanned type is already in scope at both production call sites: the same `toolType` loop
  variable is passed to `ToolNameGenerator.GenerateName(method, toolType, options)` on line 90.

Project conventions: xUnit v3 + AwesomeAssertions (namespace `AwesomeAssertions`) + NSubstitute;
test names `Should_<Outcome>_When<Condition>`; `IAsyncLifetime` members return `ValueTask`; run tests
with `dotnet test --project <csproj-dir>` and NEVER pass `--nologo` (it is forwarded to the test host,
which rejects it with exit code 5). Strict analyzer gate: `TreatWarningsAsErrors=true`, zero warnings
in all four projects; library code needs `ConfigureAwait(false)` (CA2007) and ordinal string
comparisons (CA1310); `[tests/**.cs]` in `.editorconfig` already disables CA1707/CA1822/CA2007.
E2E tests need Keycloak: `docker compose -f docker/docker-compose.yml up -d` (prefix with
`POSTGRES_PORT=15432` if host port 5432 is taken). Demo policies: `PolicyNames.RequireMember`,
`RequireManager`, `RequireAdmin`. Demo users: viewer/viewer123, alice@example.com/alice123 (member),
bob@example.com/bob123 (manager), carol@example.com/carol123 and admin/admin123 (admin).
Demo tools today: 9 — `UserGetById`, `get_all`, `create`, `update`, `promote_to_manager`,
`get_scope_id`, `get_public_info`, `get_mcp_context`, `echo_headers`; per-role visibility viewer 6,
member 7, manager 8, admin 9. Baseline before this bugfix: unit 99/99, E2E 59/59, all green.
</mission>

<block id="01-metadata-builder-uses-tooltype">
## TDDAB-1: ToolMetadataBuilder reads class attributes from the scanned tool type

<intro>
Pure helper change, no SDK dependency, no behaviour outside metadata construction. `Build` gains a
`Type toolType` parameter and collects class-level attributes from it; `inherit: true` still pulls in
the base-class attributes, so an inherited method ends up with BOTH the derived and the base policy,
mirroring what ASP.NET MVC does for the REST endpoint. The 7 existing call sites in the test file are
updated to pass the declaring type explicitly, which keeps their current expectations intact.
Files: `src/Zero.Mcp.Extensions/ToolMetadataBuilder.cs`,
`tests/Zero.Mcp.Extensions.Tests/ToolMetadataBuilderTests.cs`.
Fixtures to add inside the test file: an abstract `BaseFixture` with class attribute
`[Authorize(Policy = "ReaderOrAbove")]` and a `[Description("q")] public virtual void Query()`;
`[Authorize(Policy = "AdminOnly")] sealed class DerivedFixture : BaseFixture` (Query NOT overridden);
`[Authorize(Policy = "AdminOnly")] sealed class OverridingFixture : BaseFixture` (Query overridden);
`sealed class PlainDerivedFixture : BaseFixture` (no class attribute);
`[AllowAnonymous] sealed class AnonDerivedFixture : BaseFixture`;
`[Authorize(Policy = "ManagerOnly")] sealed class OtherDerivedFixture : BaseFixture`.
Reach the inherited method with `typeof(DerivedFixture).GetMethod(nameof(BaseFixture.Query))!`, whose
`DeclaringType` is `BaseFixture` — that is exactly the shape the scanner produces.
</intro>

<red>
- test: Should_IncludeDerivedClassPolicy_WhenMethodIsInherited — Build(inherited Query, typeof(DerivedFixture), includeAuthorization: true) contains an AuthorizeAttribute with Policy "AdminOnly"
- test: Should_StillIncludeBaseClassPolicy_WhenMethodIsInherited — the same result also contains Policy "ReaderOrAbove"
- test: Should_PutMethodInfoFirst_WhenMethodIsInherited — result[0] is the MethodInfo passed in
- test: Should_KeepBehaviour_WhenMethodIsOverriddenInDerivedType — Build(overridden Query, typeof(OverridingFixture), true) contains both "AdminOnly" and "ReaderOrAbove"
- test: Should_ApplyOnlyBasePolicy_WhenDerivedHasNoClassAttribute — Build(Query, typeof(PlainDerivedFixture), true) yields exactly one AuthorizeAttribute, Policy "ReaderOrAbove"
- test: Should_IncludeAllowAnonymousOfDerivedType_WhenDeclaredThere — Build(Query, typeof(AnonDerivedFixture), true) contains an AllowAnonymousAttribute
- test: Should_GiveEachDerivedTypeItsOwnPolicy_WhenSharingABaseMethod — the same MethodInfo built for DerivedFixture yields "AdminOnly" and for OtherDerivedFixture yields "ManagerOnly"
- test: Should_StripDerivedAuthorizationMetadata_WhenIncludeAuthorizationIsFalse — Build(Query, typeof(DerivedFixture), false) contains no IAuthorizeData and no IAllowAnonymous, still contains the MethodInfo and the [Description]
- test: Should_ThrowArgumentNull_WhenToolTypeIsNull — Build(method, null!, true) throws ArgumentNullException with parameter name "toolType"
- test: the 7 pre-existing ToolMetadataBuilderTests keep passing with the new signature (MethodInfo first, class-before-method order, strip when includeAuthorization is false, only-MethodInfo for a bare type, ArgumentNull on a null method)
</red>

### Implementation
```csharp
/// <param name="toolType">
/// The type being scanned for tools. Class-level attributes are read from this type, NOT from
/// <c>method.DeclaringType</c>: a tool method inherited from a base controller must carry the
/// authorization attributes of the controller actually being registered (issue #1).
/// </param>
public static IReadOnlyList<object> Build(MethodInfo method, Type toolType, bool includeAuthorization)
{
    ArgumentNullException.ThrowIfNull(method);
    ArgumentNullException.ThrowIfNull(toolType);

    List<object> metadata = [method];
    // inherit: true still walks up to the base class, so an inherited method gets the derived
    // policy AND the base policy, exactly as MVC composes them for the REST endpoint.
    metadata.AddRange(toolType.GetCustomAttributes(inherit: true));
    metadata.AddRange(method.GetCustomAttributes(inherit: true));

    if (!includeAuthorization)
    {
        metadata.RemoveAll(static m => m is IAuthorizeData or IAllowAnonymous or AuthorizationPolicy or IAuthorizationRequirementData);
    }

    return metadata;
}
```
Assertions use `result.OfType<AuthorizeAttribute>().Select(a => a.Policy)` and
`result.Should().Contain(m => m is AllowAnonymousAttribute)`; ordering assertions use
`.Should().Equal(...)` on the projected policy list, as in the existing tests.

<success>
- [ ] All new tests green and the 7 pre-existing ToolMetadataBuilder tests still green
- [ ] `grep -rn "DeclaringType" src/` returns 0 hits in the library
- [ ] Green-gate BTLT passes — build + tests + lint + typecheck (configured commands, skip n/a)
</success>
</block>

<block id="02-thread-tooltype-through-registration">
## TDDAB-2: ToolCreateOptionsFactory and both registration paths pass the scanned type

<intro>
`ToolCreateOptionsFactory.Create` gains a `Type toolType` parameter and forwards it to
`ToolMetadataBuilder.Build`. Both registration lambdas in `WithToolsFromAssemblyUnwrappingActionResult`
pass the `toolType` loop variable that is already used for the tool name on line 90, removing the
current split where the tool NAME comes from the scanned type while authorization came from the
declaring type. Depends on block 01.
Files: `src/Zero.Mcp.Extensions/ToolCreateOptionsFactory.cs`,
`src/Zero.Mcp.Extensions/McpServerBuilderExtensions.cs`,
`tests/Zero.Mcp.Extensions.Tests/ToolCreateOptionsFactoryTests.cs`,
`tests/Zero.Mcp.Extensions.Tests/McpServerBuilderExtensionsTests.cs`.
The builder tests need an assembly-level fixture pair, because the scanner reads
`options.ToolAssembly`: `[Authorize(Policy = "ReaderOrAbove")] public abstract class InheritedToolBase`
with `[McpServerTool(Name = "inherited_tool"), Description("d")] public virtual string Run() => "x";`,
plus `[Authorize(Policy = "AdminOnly")] [McpServerToolType] internal sealed class AdminScanFixture :
InheritedToolBase` and `[Authorize(Policy = "ManagerOnly")] [McpServerToolType] internal sealed class
ManagerScanFixture : InheritedToolBase`. Both fixtures inherit the SAME method, so with the default `MethodOnly`
convention they would both be named `run`; set
`options.NamingConvention = ToolNamingConvention.ControllerPrefix` in these two tests so the tools are
registered as `admin_scan_fixture_run` and `manager_scan_fixture_run` and stay distinguishable.
(The name collision under `MethodOnly` is a separate, non-security defect — see the note at the end
of this plan.)
</intro>

<red>
- test: ToolCreateOptionsFactoryTests.Should_UseScannedTypeForClassMetadata_WhenMethodIsInherited — Create(inherited method, typeof(DerivedFixture), services, serializerOptions, true).Metadata contains Policy "AdminOnly"
- test: ToolCreateOptionsFactoryTests.Should_ThrowArgumentNull_WhenToolTypeIsNull
- test: ToolCreateOptionsFactoryTests: the 12 existing tests keep passing with the new signature
- test: McpServerBuilderExtensionsTests.Should_AttachDerivedControllerPolicy_WhenToolMethodIsInherited — after AddZeroMcpExtensions with UseAuthorization true over the fixture assembly, the tool registered for AdminScanFixture has metadata containing Policy "AdminOnly"
- test: McpServerBuilderExtensionsTests.Should_GiveEachControllerItsOwnPolicy_WhenTwoTypesShareABaseMethod — the AdminScanFixture tool carries "AdminOnly" and the ManagerScanFixture tool carries "ManagerOnly", and neither carries the other's policy
- test: McpServerBuilderExtensionsTests: the existing registration, metadata and filter-count tests stay green
</red>

### Implementation
```csharp
// ToolCreateOptionsFactory
public static McpServerToolCreateOptions Create(
    MethodInfo method,
    Type toolType,
    IServiceProvider services,
    JsonSerializerOptions serializerOptions,
    bool includeAuthorization)
{
    ArgumentNullException.ThrowIfNull(method);
    ArgumentNullException.ThrowIfNull(toolType);

    var options = new McpServerToolCreateOptions
    {
        Services = services,
        SerializerOptions = serializerOptions,
        Metadata = ToolMetadataBuilder.Build(method, toolType, includeAuthorization),
        Description = method.GetCustomAttribute<DescriptionAttribute>()?.Description,
    };
    // ... rest unchanged (Title, hints, UseStructuredContent, OutputSchema, Icons) ...
}

// McpServerBuilderExtensions — static path (~line 107)
ToolCreateOptionsFactory.Create(method, toolType, services, serializerOptions, options.UseAuthorization)
// McpServerBuilderExtensions — instance path (~line 129)
ToolCreateOptionsFactory.Create(methodCopy, toolType, services, serializerOptions, options.UseAuthorization)
```
`toolType` is the existing `foreach` variable and is already captured per iteration, so no extra
closure copy is needed beyond the `methodCopy`/`toolNameCopy` ones already present.

<success>
- [ ] All new tests green; the 12 ToolCreateOptionsFactory tests and the whole McpServerBuilderExtensions suite still green
- [ ] vs-mcp FindSymbolUsages on `ToolCreateOptionsFactory.Create` shows no caller left on the old 4-argument signature
- [ ] Green-gate BTLT passes — build + tests + lint + typecheck (configured commands, skip n/a)
</success>
</block>

<block id="03-e2e-inherited-authorization">
## TDDAB-3: End-to-end proof over /mcp with Keycloak roles

<intro>
Proves the bypass is closed through the real pipeline — SDK authorization filters plus the host
`IAuthorizationService` plus a Keycloak JWT — and not only in unit tests. Adds to the demo an
abstract base controller that declares the tool method and carries the weak policy, and a derived
controller that carries `RequireAdmin` and does NOT override the method: the exact shape of issue #1.
Depends on block 02.
Files: new `src/McpPoc.Api/Controllers/ReportsControllerBase.cs`, new
`src/McpPoc.Api/Controllers/AdminReportsController.cs`, new
`tests/McpPoc.Api.Tests/InheritedToolAuthorizationTests.cs`,
`tests/McpPoc.Api.Tests/ToolVisibilityTests.cs`, `tests/McpPoc.Api.Tests/McpToolDiscoveryTests.cs`.
The demo tool inventory goes from 9 to 10 with `admin_reports_summary`, visible to admin only, so only
the admin expectation changes: viewer 6, member 7, manager 8, admin 10. The base class is abstract and
has no `[McpServerToolType]`, so it is not scanned on its own. Demo logging must use the
`LoggerMessage` source-generated methods in `src/McpPoc.Api/Infrastructure/Log.cs` if any log call is
added, and library/demo code must keep `ConfigureAwait(false)` where awaits exist, to satisfy the
strict analyzer gate.
</intro>

<red>
- test: InheritedToolAuthorizationTests.Should_HideInheritedAdminTool_WhenUserIsViewer — tools/list as viewer does not contain admin_reports_summary
- test: InheritedToolAuthorizationTests.Should_HideInheritedAdminTool_WhenUserIsManager — tools/list as manager does not contain it either, which proves the DERIVED policy is enforced and not the weaker base one
- test: InheritedToolAuthorizationTests.Should_DenyInheritedAdminTool_WhenManagerCallsIt — CallToolAsync throws McpProtocolException whose message contains "Access forbidden"
- test: InheritedToolAuthorizationTests.Should_ExposeAndRunInheritedAdminTool_WhenUserIsAdmin — admin sees the tool in tools/list and the call returns a payload containing the status field
- test: ToolVisibilityTests.Admin_Should_See_AllTools — expected list updated to the 10 tools including admin_reports_summary
- test: ToolVisibilityTests viewer/member/manager expectations unchanged at 6/7/8 and must not contain admin_reports_summary
- test: McpToolDiscoveryTests.Should_DiscoverToolsFilteredByRole_WhenListingTools — member count stays 7 and the list does not contain admin_reports_summary
</red>

### Implementation
```csharp
// src/McpPoc.Api/Controllers/ReportsControllerBase.cs
[ApiController]
[Authorize]  // weak policy on the base: any authenticated user
public abstract class ReportsControllerBase : ControllerBase
{
    [HttpGet("summary")]
    [McpServerTool(Name = "admin_reports_summary"), Description("Returns a report summary")]
    public virtual ActionResult<ReportSummary> Summary() => Ok(new ReportSummary("ok", DateTime.UtcNow));
}

public record ReportSummary(string Status, DateTime GeneratedAt);

// src/McpPoc.Api/Controllers/AdminReportsController.cs
[Route("api/admin-reports")]
[Authorize(Policy = PolicyNames.RequireAdmin)]  // stricter policy on the SCANNED type
[McpServerToolType]
public sealed class AdminReportsController : ReportsControllerBase
{
    // Summary is inherited and deliberately NOT overridden — this is the issue #1 shape.
}
```
The E2E test class follows the existing pattern: `[Collection("McpApi")]`, `IAsyncLifetime` with
`ValueTask`, clients from `_fixture.GetAuthenticatedClientAsync(user, password)` wrapped in
`McpClientHelper`; the forbidden case asserts
`await act.Should().ThrowAsync<McpProtocolException>().WithMessage("*Access forbidden*")`.
Before the fix these tests fail (manager and viewer both see and can call the tool); after it they pass.

<success>
- [ ] The 4 new E2E tests green with Keycloak running
- [ ] ToolVisibilityTests at 6/7/8/10 and McpToolDiscoveryTests green
- [ ] Full suites green, no pre-existing test disabled, skipped or weakened
- [ ] Green-gate BTLT passes — build + tests + lint + typecheck (configured commands, skip n/a)
</success>
</block>

<block id="04-release-3-0-1">
## TDDAB-4: Release 3.0.1 and document the fix

<intro>
Patch release: no public API change, because `ToolMetadataBuilder` and `ToolCreateOptionsFactory` are
`internal static`. Updates version, changelog and the authorization documentation, so that anyone
using controller inheritance knows they must upgrade. Depends on block 03.
Files: `Version.props`, `CHANGELOG.md`, `docs/MCP-AUTHORIZATION-COMPLETE-GUIDE.md`,
`src/Zero.Mcp.Extensions/README.md`, `tests/Zero.Mcp.Extensions.Tests/PackageTests.cs`.
</intro>

<red>
- test: PackageTests.Should_HaveVersion301_WhenPacked — typeof(ZeroMcpOptions).Assembly.GetName().Version has Major 3, Minor 0, Build 1
- test: PackageTests.Should_DocumentInheritedAuthorization_WhenReadingReadme — the package README contains a statement that the class-level attributes of the scanned controller apply to inherited tool methods
- test: PackageTests.Should_DependOnSdk2_WhenPacked — unchanged, still green
- test: dotnet pack -c Release produces Zero.Mcp.Extensions.3.0.1.nupkg listing ModelContextProtocol 2.2.0
</red>

### Implementation
`Version.props`: `<MainVersion>3.0.1</MainVersion>`.
`CHANGELOG.md`: new `## 3.0.1 - 2026-09-28` entry with a Fixed section — inherited tool methods now
carry the class-level authorization attributes of the scanned controller (GitHub issue #1); describes
the bypass, states that projects using controller inheritance with per-controller policies must
upgrade, and notes there is no API change.
`docs/MCP-AUTHORIZATION-COMPLETE-GUIDE.md`: new subsection "Inherited tool methods" explaining that
the scanned type supplies the class-level attributes, that overriding the method is not required, and
that the base policy still applies through `inherit: true`.
`src/Zero.Mcp.Extensions/README.md`: one line in the authorization section with the same statement.

<success>
- [ ] PackageTests green and `dotnet pack -c Release` yields Zero.Mcp.Extensions.3.0.1.nupkg
- [ ] CHANGELOG and authorization guide describe the fix and who must upgrade
- [ ] Green-gate BTLT passes — build + tests + lint + typecheck (configured commands, skip n/a)
</success>
</block>

## Execution Order
01-metadata-builder-uses-tooltype       → no dependencies
02-thread-tooltype-through-registration → depends on 01
03-e2e-inherited-authorization          → depends on 02
04-release-3-0-1                        → depends on 03

## Out of scope (separate issue)
With the default `MethodOnly` naming convention, two controllers inheriting the same base tool method
produce the same tool name, so one silently overwrites the other in `tools/list`. Only
`ControllerPrefix` disambiguates them. This is a correctness defect, not a security one, and is not
addressed here; block 02 works around it in its fixtures by selecting `ControllerPrefix`.
