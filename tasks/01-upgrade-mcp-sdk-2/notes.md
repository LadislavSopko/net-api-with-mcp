# Feature: 01-upgrade-mcp-sdk-2

Branch: feature/laco/upgrade (pre-existing, used as-is)

## Requirements (from user)
"Fai quello che ci siamo detti" = the 5-point plan agreed after the 2026-09-14 ecosystem analysis:
1. Upgrade ModelContextProtocol 0.6.0-preview.1 -> 2.2.0; rewrite tools/list filter with WithRequestFilters; evaluate replacing custom pre-filter with SDK AddAuthorizationFilters(), keeping only controller mapping + ActionResult<T> unwrapping.
2. Security bump: Microsoft.AspNetCore.*/Extensions.* 10.0.2 -> 10.0.12, Scalar 2.17.3, Test.Sdk 18.10.0, SourceLink 10.0.401 (fixes NU1902/NU1903 audit errors).
3. FluentAssertions 8 -> AwesomeAssertions 9.6 (license); evaluate xunit.v3.
4. New differentiators: OutputSchemaType for T, ttlMs hint on tools/list, verify stateless mode, [Authorize] inherited from controller.
5. Close lint debt (~36 analyzer findings) and re-enable TreatWarningsAsErrors.

## Analysis
Library = 11 files / 949 lines in src/Zero.Mcp.Extensions. SDK used: ModelContextProtocol 0.6.0-preview.1 (vendored copy in 3rdp/csharp-sdk at v0.6.0-preview.1-14-g46d3044; 3rdp is in the .slnx but fails restore independently).

What breaks with SDK 2.2.0 (verified against v2.2.0 source cloned in scratchpad):
- `builder.AddListToolsFilter(...)` removed -> `builder.WithRequestFilters(f => f.AddListToolsFilter(...))` (McpServerBuilderExtensions.cs:965, McpRequestFilterBuilderExtensions.cs:33).
- Stateless is the default (`HttpServerTransportOptions.SessionMode = Stateless`). EnableLegacySse off by default.
- SDK `McpServerToolAttribute`/`McpServerToolTypeAttribute` (ModelContextProtocol.Server) collide by name with our own attributes in Zero.Mcp.Extensions. SDK attribute now has Title, UseStructuredContent, OutputSchemaType, IconSource, Destructive/Idempotent/OpenWorld/ReadOnly.

What the SDK now does that we re-implement:
- `AddAuthorizationFilters()` (HttpMcpServerBuilderExtensions.cs:65 + AuthorizationFilterSetup.cs) evaluates [Authorize]/[AllowAnonymous] metadata from `McpServerTool.Metadata` using IAuthorizationPolicyProvider + IAuthorizationService: filters tools/list per user AND blocks tools/call ("Access forbidden"). It also THROWS if tools carry authorization metadata and the filter was not registered (safety net).
- Metadata for a tool = [MethodInfo, class attributes..., method attributes...] (AIFunctionMcpServerTool.CreateMetadata, internal). Settable from outside via `McpServerToolCreateOptions.Metadata`.
- Therefore McpAuthorizationPreFilter, IAuthForMcpSupplier, ToolAuthorizationMetadata, IToolAuthorizationStore, ToolListFilter, IUserRoleResolver become redundant. They also hardcode demo-app policy names (RequireMember/Manager/Admin) and role names (Viewer/Member/Manager/Admin) inside the generic library - a design smell that goes away.

What the SDK still does NOT do:
- `McpServerTool.Create(MethodInfo, createTargetFunc, options)` hardcodes MarshalResult = passthrough (AIFunctionMcpServerTool.cs:77) -> ActionResult<T> is NOT unwrapped. Our AIFunctionFactory + MarshalResult path stays, and we must set `Metadata` ourselves so AddAuthorizationFilters sees [Authorize].
- No controller scanning; SDK WithToolsFromAssembly uses the MethodInfo path (no unwrap).

Dependents of the code slated for removal: src/McpPoc.Api (KeycloakAuthSupplier, UserRoleResolver, Program.cs) and 5 unit-test files (IAuthForMcpSupplierTests, McpAuthorizationPreFilterTests, ToolAuthorizationMetadataTests, ToolFilteringTests, parts of McpServerBuilderExtensionsTests). E2E tests (PolicyAuthorizationTests, ToolVisibilityTests) test behaviour, not internals - they stay as the regression net.

Dependencies (from 2026-09-14 research): Microsoft 10.0.2 -> 10.0.12 (10 security patches; NU1903 on Microsoft.OpenApi 2.0.0 currently fails the build under the lint gate), Scalar 2.17.3, Test.Sdk 18.10.0, SourceLink 10.0.401, FluentAssertions 8 (commercial license) -> AwesomeAssertions 9.6.0, Moq 4.20.72 dead 2y -> NSubstitute 6.2.0, xunit v2 frozen -> xunit.v3 4.0.1 (MTP default in .NET 10 SDK).

Lint debt under the j-setup gate: ~36 analyzer findings in the library (CA1848, CA2007, CA1873, CA1310, IDE0005, CA2263, CA1860); gate currently staged as warnings-only.

## Research
1. Codebase: existing E2E tests (PolicyAuthorizationTests, ToolVisibilityTests, McpToolInvocationTests) already encode the required behaviour -> reuse as regression net.
2. Framework: SDK 2.2.0 `AddAuthorizationFilters()` covers ~100% of authorization + list-filtering requirement -> ADOPT, delete our re-implementation (rule: proven approach >= 80%).
3. Registry: competitors (ZeroMCP.net 2.0.0, Nabu.Mcp.AspNetCore 1.0.14, McpIt 1.4.0) all sit on SDK 1.4+/2.x with SDK-native auth; none unwrap ActionResult<T> via marshaller - that stays our differentiator.
4. SDK v2.2.0 source (scratchpad clone) used as ground truth for signatures.

## Proposed Solution (APPROVED 2026-09-14)
Zero.Mcp.Extensions 3.0.0 on ModelContextProtocol 2.2.0.
1. Tools created via AIFunctionFactory + MarshalResult (kept) and `McpServerTool.Create(aiFunction, new McpServerToolCreateOptions { Services, Metadata = [method, class attrs, method attrs] })` so SDK `AddAuthorizationFilters()` sees [Authorize]/[AllowAnonymous].
2. REMOVE: IAuthForMcpSupplier, McpAuthorizationPreFilter, ToolAuthorizationMetadata, IToolAuthorizationStore/ToolAuthorizationStore, ToolListFilter, IUserRoleResolver; ZeroMcpOptions.UseAuthorization + FilterToolsByPermissions. Authorization = SDK AddAuthorizationFilters() (always on; policies come from the host's AddAuthorization). Demo: delete KeycloakAuthSupplier + UserRoleResolver.
3. REMOVE own McpServerToolTypeAttribute/McpServerToolAttribute; scan for SDK `ModelContextProtocol.Server.McpServerToolTypeAttribute`/`McpServerToolAttribute`; ToolNameGenerator reads SDK attr.Name; Title/OutputSchemaType/UseStructuredContent/IconSource/ReadOnly/Destructive flow via McpServerToolCreateOptions (replicate SDK DeriveOptions for the AIFunction path).
4. New: ttlMs cache hint option on tools/list (ZeroMcpOptions.ToolsListTtl), stateless-mode E2E, OutputSchemaType E2E, IMcpRequestContext verified on Streamable HTTP.
5. Deps: Microsoft.* 10.0.12, Scalar 2.17.3, Test.Sdk 18.10.0, SourceLink 10.0.401, AwesomeAssertions 9.6.0 (replaces FluentAssertions), NSubstitute 6.2.0 (replaces Moq), xunit.v3 4.0.1 + xunit.runner.visualstudio 4.0.0 (replaces xunit 2.9.3). global.json rollForward latestFeature.
6. 3rdp/csharp-sdk: checkout tag v2.2.0 (user choice), keep in .slnx.
7. Lint debt closed; TreatWarningsAsErrors=true and .editorconfig TODO block removed; WarningsNotAsErrors NU* removed.
Tests: E2E (PolicyAuthorizationTests, ToolVisibilityTests, McpToolInvocationTests, McpToolDiscoveryTests, McpRequestContextE2ETests, ToolNamingTests) = regression net. Unit tests on removed internals deleted; new unit tests for metadata builder + option derivation.

## Complexity Assessment
| Task | Score | Split |
|---|---|---|
| Dependency bump (Microsoft/Scalar/SourceLink/Test.Sdk) | 2 | single |
| 3rdp checkout v2.2.0 + solution restore | 3 | single |
| Test stack swap (xunit.v3 + AwesomeAssertions + NSubstitute) | 5 | 2 subtasks: packages+usings ; Moq->NSubstitute rewrite (5 files) |
| SDK 2.2.0 API migration of builder (filters, attributes, metadata) | 8 | 5 subtasks: attribute swap ; metadata builder ; option derivation ; tool registration ; AddAuthorizationFilters wiring |
| Remove custom auth + demo cleanup | 5 | 2 subtasks: library removal ; demo Program/Infrastructure |
| ttlMs hint + stateless/OutputSchema E2E | 4 | 2 subtasks |
| Lint debt (36 findings) + gate re-enable | 4 | 2 subtasks: LoggerMessage/CA1848 ; rest + gate |
| Version 3.0.0, README, MB | 2 | single |

## Status
- [x] Requirements gathered
- [x] Code analyzed
- [x] Solution proposed
- [x] Plan created (tasks/01-upgrade-mcp-sdk-2/plan.md, 9 blocks)
- [x] Plan reviewed twice (2026-09-14). Second review (vs-mcp + SDK v2.2.0 source cross-check) fixed: block 04 must wire `includeAuthorization: false` because SDK `WithHttpTransport` installs authorization guard filters that throw when `[Authorize]` metadata is present without `AddAuthorizationFilters()`; block 06 `McpServer.Create` needs the required `McpServerOptions` argument; block 05 forbidden-call E2E asserts a thrown `McpProtocolException` (SDK filter → JSON-RPC error), not `IsError`; filter-count RED tests use exact counts (SDK list guard is always present); real tool names are `UserGetById` + 8 snake_case incl. `echo_headers` (9 tools). Measured baseline: E2E 45/56 (11 stale-expectation failures fixed in block 01). Constraint from user: MCP authorization must follow the BE endpoint rules exactly — satisfied by SDK AddAuthorizationFilters evaluating the controller's own [Authorize]/[AllowAnonymous]/policies through the host IAuthorizationService.
- [x] Development done (9/9 TDDAB blocks via CVM run-20260914-1620, commits 3ff616f..70488a1)
- [x] Tested (2026-09-17 pre-merge gate: build 0 warnings x4 projects, unit 99/99, E2E 59/59 with Keycloak, dotnet format exit 0, ast-grep absent = skip)
- [x] Deployed (2026-09-17: nuget.org 3.0.0 + snupkg published via publish-nuget.sh; main merged and pushed)

## TDDAB Rules Applied (read from tddab-planner.md + csharp-tddab-overlay.md on 2026-09-14)
1. Decompose bottom-up by layer: pure helpers (metadata builder, options factory) first, then registration/wiring, then E2E-facing behaviour; never one monolithic "upgrade" block.
2. RED is the contract of ONE unit: unit-level tests naming the exact behaviour (happy path, edge, error). E2E tests are the regression net, not the RED of a block.
3. Every block is self-sufficient: full paths, reference code with types/signatures/assertions, no options, no TODO, no "investigate". Setup work (package bumps, file deletions, 3rdp checkout) is merged into the implementation phase of the first block that needs it, never a separate setup block.
4. Every block is atomic and revertable with `git revert HEAD`; the system builds and tests pass after each block; no dependency on future blocks.
5. VERIFY is the BTLT green gate from j-settings.md (dotnet build, dotnet test, dotnet format --verify-no-changes + ast-grep via lint-run.sh, no separate typecheck for C#); every success list ends with the green-gate line.
6. C# overlay: xUnit (v3 here), AwesomeAssertions, NSubstitute; async tests as `async Task`; Should_<Outcome>_When<Condition> naming; nullable enabled and null paths tested.
