# Changelog

All notable changes to Zero.Mcp.Extensions are documented here.

## 3.0.0 - 2026-09-14

Built on the official MCP C# SDK **2.2.0**. Breaking release.

### Breaking

- The library no longer ships its own `McpServerToolTypeAttribute` / `McpServerToolAttribute`. Use the SDK attributes from `ModelContextProtocol.Server` (`using ModelContextProtocol.Server;`).
- Custom authorization layer removed: `IAuthForMcpSupplier`, `McpAuthorizationPreFilter`, `ToolAuthorizationMetadata`, `IToolAuthorizationStore`, `ToolAuthorizationStore`, `ToolListFilter`, `IUserRoleResolver`. Authorization is delegated to the SDK's `AddAuthorizationFilters()`, which evaluates the controller's own `[Authorize]`, policies and `[AllowAnonymous]` through the host `IAuthorizationService`. The host must call `AddAuthorization(...)`.
- `ZeroMcpOptions.FilterToolsByPermissions` removed: `tools/list` is always filtered per user when `UseAuthorization` is true.
- A forbidden `tools/call` is now a JSON-RPC error (`Access forbidden: This tool requires authorization.`), surfaced by SDK clients as `McpProtocolException`, instead of a `CallToolResult` with `IsError = true`.
- Streamable HTTP is stateless by default (no `Mcp-Session-Id`); use `ZeroMcpOptions.SessionMode` for stateful sessions.

### Added

- `ZeroMcpOptions.ToolsListTimeToLive`: when set, `tools/list` carries `ttlMs` and `cacheScope` (`private` when `UseAuthorization`, `public` otherwise).
- `ZeroMcpOptions.SessionMode` (`HttpServerSessionMode`, default `Stateless`).
- SDK attribute members flow to the client for controller tools: `Title`, `ReadOnly` / `Destructive` / `Idempotent` / `OpenWorld` hints, `IconSource`, `UseStructuredContent`, `OutputSchemaType` (output schema is emitted when `UseStructuredContent = true`).
- `IMcpRequestContext` is verified to work inside tools under stateless Streamable HTTP.

### Changed

- Tool registration builds `McpServerToolCreateOptions` (metadata, description, hints, output schema, icons) itself because the AIFunction overload of `McpServerTool.Create` does not read attributes.
- Strict analyzer gate: the solution builds with `TreatWarningsAsErrors=true` and zero warnings.

### Migration from 2.x

1. Replace `using Zero.Mcp.Extensions;` for the attributes with `using ModelContextProtocol.Server;`.
2. Delete your `IAuthForMcpSupplier` / `IUserRoleResolver` implementations and registrations; register policies with `builder.Services.AddAuthorization(...)`.
3. Remove `options.FilterToolsByPermissions`.
4. Optionally set `ToolsListTimeToLive`, `SessionMode`, and `UseStructuredContent` + `OutputSchemaType` on tools.
5. Update tests that asserted `IsError` on forbidden calls to expect `McpProtocolException`.

### Dependencies

- ModelContextProtocol, ModelContextProtocol.Core, ModelContextProtocol.AspNetCore 2.2.0 (was 0.6.0-preview.1)
- Microsoft.AspNetCore.* / Microsoft.Extensions.* 10.0.12
- Test stack: xunit.v3 4.0.1, AwesomeAssertions 9.6.0, NSubstitute 6.2.0

## 2.1.0 - 2026-01-20

- Tool naming conventions (`MethodOnly`, `ControllerPrefix`, explicit `Name`, custom separator).
- `IMcpRequestContext` with `x-mcp-call` header injection middleware.

## 2.0.0

- ActionResult unwrapping, `[Authorize]` policy pre-filter, role-based `tools/list` filtering.

## 1.0.0

- Initial release.
