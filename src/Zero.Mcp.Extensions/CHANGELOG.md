# Changelog

## [1.9.0] - 2025-01-30

### Added
- **Configuration System**: Professional ZeroMcpOptions for flexible configuration
- `RequireAuthentication` option to enable/disable authentication requirement
- `UseAuthorization` option to enable/disable [Authorize] policy enforcement
- `McpEndpointPath` option to customize MCP endpoint path
- `ToolAssembly` option to explicitly specify assembly to scan
- `SerializerOptions` option to customize JSON serialization
- Clear error messages when IAuthForMcpSupplier is required but not registered

### Changed
- `AddZeroMcpExtensions` now accepts `Action<ZeroMcpOptions>` for configuration
- `MapZeroMcp` now uses configuration from ZeroMcpOptions
- IAuthForMcpSupplier is now optional when `UseAuthorization = false`

## [1.8.1] - 2025-01-30

### Fixed
- **CRITICAL FIX**: Assembly resolution in AddZeroMcpExtensions now captures calling assembly before method chaining
- Without this fix, GetCallingAssembly() would resolve to the library assembly instead of the host assembly, causing tools to not be discovered

## [1.8.0] - 2025-01-30

### Security
- **CRITICAL FIX**: Multiple [Authorize] attributes now enforced (all must pass)
- Security-hardened authorization pre-filter

### Fixed
- Null values in ActionResult<T> now handled correctly (Ok(null) is valid)
- Parameter binding now uses name-based matching (robust against reordering)

### Added
- Initial production release
- IAuthForMcpSupplier interface for flexible authorization
- Automatic ActionResult<T> unwrapping via MarshalResult
- Pre-filter authorization checks with [AllowAnonymous] support
- Attribute inheritance from base classes
- Support for [Authorize] and [Authorize(Policy="...")] attributes
- WithToolsFromAssemblyUnwrappingActionResult extension
- Complete invocation handler with authorization + marshaling
- Error result handling (throws exception instead of serializing)

### Architecture
- Library completely decoupled from HttpContext
- Host manages dependencies (IHttpContextAccessor, IAuthorizationService)
- Minimal dependencies (only abstractions)
- Production-ready with comprehensive test coverage
