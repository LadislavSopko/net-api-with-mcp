# Bug: 02-inherited-tool-authorize

Source: GitHub issue #1 (LadislavSopko/net-api-with-mcp), opened 2026-09-28 by LadislavSopko.
Affects: Zero.Mcp.Extensions 3.0.0 (published on nuget.org) with ModelContextProtocol 2.2.0.

## Problem

**Expected:** a tool method inherited from a base controller must carry the class-level
`[Authorize]` attributes of the SCANNED (derived) controller, exactly as ASP.NET MVC does for the
REST endpoint. Declaring a stricter policy on the derived class must be enough.

**Actual:** the derived class attributes are silently dropped. Only the base class policy reaches
`McpServerTool.Metadata`, so the SDK authorization filters enforce the weaker base policy. A tool
that REST answers with 403 for a low-privileged user is listed and callable over `/mcp`.

**Where:** `src/Zero.Mcp.Extensions/ToolMetadataBuilder.cs:27` — class attributes are read from
`method.DeclaringType` instead of the scanned `toolType`.

**Severity:** authorization bypass on inherited, non-overridden tool methods.

## Analysis

Protocol D — READ / ISOLATE / DOCS. Code navigation: the first pass ran with vs-mcp DOWN (grep/read
fallback); the analysis was then RE-RUN with vs-mcp connected and every finding below is confirmed
semantically (FindSymbolUsages / FindSymbols / GetInheritance), plus a runtime reflection probe.

### Call sites that the signature change touches (vs-mcp FindSymbolUsages, 2026-09-28)
- `ToolMetadataBuilder.Build` — 9 usages: production `ToolCreateOptionsFactory.cs:37`, 7 calls in
  `ToolMetadataBuilderTests.cs` (23, 31, 41, 49, 58, 69, 77) and 1 doc-comment reference.
- `ToolCreateOptionsFactory.Create` — 3 usages: production `McpServerBuilderExtensions.cs:107` (static
  path) and `:129` (instance path), plus the `Create` helper in `ToolCreateOptionsFactoryTests.cs:25`.
- Both types are `internal static` (Build is public *within* an internal class), so the signature
  change is NOT a public API break → 3.0.1 can ship as a PATCH release.
- `GetInheritance(UsersController)` → baseTypes = [ControllerBase] only, derivedTypes = []. The demo has
  no controller inheritance at all: that is exactly why 158 green tests never exercised this path.

### READ — observed behaviour
Reported: `tools/list` as Reader contains `utenti_query` and `tools/call utenti_query` returns rows,
while `POST /api/Utenti/query` as the same Reader returns 403.

### ISOLATE — exact location
- `src/Zero.Mcp.Extensions/ToolMetadataBuilder.cs:27-30` — class-level attributes are collected from
  `method.DeclaringType`, the ONLY use of `DeclaringType` in the library (grep: 2 hits, same block).
- `src/Zero.Mcp.Extensions/McpServerBuilderExtensions.cs:83` — `toolType.GetMethods(...)` also returns
  INHERITED methods, whose `DeclaringType` is the base controller, not `toolType`.
- `src/Zero.Mcp.Extensions/ToolCreateOptionsFactory.cs:25-29,37` — `Create` never receives `toolType`,
  so it cannot pass it down; it calls `ToolMetadataBuilder.Build(method, includeAuthorization)`.
- Inconsistency confirmed: the tool NAME already uses the scanned type
  (`McpServerBuilderExtensions.cs:90` → `ToolNameGenerator.GenerateName(method, toolType, options)`),
  so name and authorization are derived from two different types today.

### Runtime probe (2026-09-28)
Minimal hierarchy compiled and reflected over: base class attribute `ReaderOrAbove`,
derived class attribute `AdminOnly`, method declared only on the base.

    DeclaringType = BaseCtrl
    via DeclaringType (current code): [ReaderOrAbove]
    via toolType     (proposed fix) : [AdminOnly, ReaderOrAbove]

`GetCustomAttributes(inherit: true)` walks the hierarchy UPWARD only, so from the base type the
derived attribute is unreachable. The derived policy is therefore never seen by the SDK filters.

### Scope of impact
- Only tool methods that are INHERITED and NOT overridden. When the derived class overrides the method,
  `DeclaringType` is already the derived type and behaviour is correct today.
- Only when the derived class adds/restricts class-level authorization metadata.
- The demo app has no controller inheritance, which is why all 158 tests stayed green.

### Related observation (NOT part of this fix)
With the default `MethodOnly` naming convention, two derived controllers inheriting the same base
method produce the SAME tool name (`query`). Only `ControllerPrefix` disambiguates them. Worth a
separate issue; it does not affect the authorization defect.

## Fix

Plan: `tasks/02-inherited-tool-authorize/plan.md` — TDDAB, 4 blocks, awaiting approval.
01 ToolMetadataBuilder takes the scanned toolType · 02 thread it through the factory and both
registration paths · 03 E2E proof with an inherited admin-only tool in the demo · 04 release 3.0.1.

Implemented 2026-09-28 via CVM run-20260928-bug02, 4/4 blocks, commits 78ea589..65b8ef4.
- `ToolMetadataBuilder.Build(MethodInfo, Type toolType, bool)` reads class attributes from the scanned
  type; `inherit: true` still contributes the base policy and the SDK combines both like MVC.
- `ToolCreateOptionsFactory.Create` takes the scanned type and both registration paths pass the
  `toolType` already used for the tool name, so name and authorization now come from one type.
- Demo gained `ReportsControllerBase` (abstract, weak `[Authorize]`, declares the tool) and
  `AdminReportsController` (`RequireAdmin`, `[McpServerToolType]`, inherits without overriding):
  `admin_reports_summary`, admin-only. Demo inventory 9 to 10 tools.
- Released as 3.0.1 (patch: both touched types are internal, no API change).
Final: unit 114/114, E2E 63/63, 0 warnings on 4 projects, dotnet format clean.

## Verification
- [x] Bug reproduced (reflection probe + code isolation; regression test to be added in RED)
- [x] Fix applied (blocks 01-02: metadata from the scanned type)
- [x] Tested working (block 03 E2E with Keycloak roles: viewer and manager neither list nor call the inherited admin tool; admin does)
- [ ] Deployed
