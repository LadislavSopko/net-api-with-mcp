# Zerox.Mcp.Extensions Library - Complete Plan

## Mission
Create a production-ready NuGet library that makes ASP.NET Core + MCP integration effortless, handling all common scenarios including authentication, authorization, and ActionResult unwrapping.

---

## Phase 1: Core Library Foundation
**Goal**: Basic library structure with ActionResult unwrapping from POC

### Tasks
- [ ] Create `src/Zerox.Mcp.Extensions/` project
- [ ] Set up .csproj with proper package metadata
  - PackageId: `Zerox.Mcp.Extensions`
  - Authors: Ladislav-Sopko
  - Company: 0ics-srl
  - Description: "ASP.NET Core MCP integration made easy"
  - Tags: mcp, model-context-protocol, aspnetcore, api
- [ ] Add dependency: `ModelContextProtocol` (>= 0.4.0-preview.3)
- [ ] Port `McpServerBuilderExtensions.cs` from POC
- [ ] Port `ActionResultUnwrapper` logic
- [ ] Add `ZeroxMcpServiceCollectionExtensions.cs` with simple API:
  ```csharp
  public static IServiceCollection AddZeroxMcp(this IServiceCollection services)
  ```

### Success Criteria
- ✓ Library compiles
- ✓ POC can reference it and use `AddZeroxMcp()`
- ✓ All 15 POC tests still pass

---

## Phase 2: Configuration & Options
**Goal**: Flexible configuration for different scenarios

### Tasks
- [ ] Create `ZeroxMcpOptions.cs`:
  ```csharp
  public class ZeroxMcpOptions
  {
      public string Endpoint { get; set; } = "/mcp";
      public bool UseSnakeCase { get; set; } = true;
      public bool UnwrapActionResults { get; set; } = true;
      public LogLevel LogLevel { get; set; } = LogLevel.Information;
      public JsonSerializerOptions? SerializerOptions { get; set; }
  }
  ```
- [ ] Add overload: `AddZeroxMcp(Action<ZeroxMcpOptions> configure)`
- [ ] Implement options pattern with `IOptions<ZeroxMcpOptions>`
- [ ] Add validation for options
- [ ] Create configuration builder pattern (optional advanced API)

### Success Criteria
- ✓ Users can configure endpoint
- ✓ Users can disable snake_case if needed
- ✓ Users can provide custom JsonSerializerOptions
- ✓ Invalid options throw clear exceptions

---

## Phase 3: Authentication Support
**Goal**: JWT Bearer token authentication for MCP endpoints

### Sub-Phase 3.1: Basic JWT Support
- [ ] Research MCP SDK authentication model
- [ ] Add dependency: `Microsoft.AspNetCore.Authentication.JwtBearer`
- [ ] Create `ZeroxMcpAuthenticationOptions`:
  ```csharp
  public class ZeroxMcpAuthenticationOptions
  {
      public bool RequireAuthentication { get; set; } = false;
      public string Authority { get; set; }
      public string Audience { get; set; }
      public string[] ValidIssuers { get; set; }
      public TokenValidationParameters ValidationParameters { get; set; }
  }
  ```
- [ ] Implement authentication middleware for MCP endpoint
- [ ] Add authentication to `ZeroxMcpOptions`

### Sub-Phase 3.2: Test Authentication
- [ ] Create test project: `tests/Zerox.Mcp.Extensions.Tests/`
- [ ] Create mock JWT token generator
- [ ] Test scenarios:
  - Anonymous access (no auth required)
  - Valid JWT token
  - Invalid JWT token
  - Expired JWT token
  - Missing token when required
- [ ] Add integration test with real Identity Server (optional)

### Success Criteria
- ✓ MCP endpoints can require authentication
- ✓ Valid JWT tokens are accepted
- ✓ Invalid tokens are rejected with 401
- ✓ HttpContext.User is populated correctly

---

## Phase 4: Authorization Support
**Goal**: Role-based and policy-based authorization for MCP tools

### Sub-Phase 4.1: Basic Authorization
- [ ] Support `[Authorize]` attribute on controllers
- [ ] Support `[Authorize(Roles = "Admin")]`
- [ ] Support `[Authorize(Policy = "PolicyName")]`
- [ ] Add authorization options:
  ```csharp
  public class ZeroxMcpAuthorizationOptions
  {
      public bool RequireAuthorization { get; set; } = false;
      public string DefaultPolicy { get; set; }
      public Dictionary<string, string> ToolPolicies { get; set; }
  }
  ```
- [ ] Implement authorization filter for tool invocation
- [ ] Add proper 403 Forbidden responses

### Sub-Phase 4.2: Tool-Level Authorization
- [ ] Allow per-tool authorization configuration
- [ ] Support custom authorization handlers
- [ ] Add attribute: `[ZeroxMcpAuthorize(Policy = "...")]`
- [ ] Handle authorization metadata from controller attributes

### Sub-Phase 4.3: Test Authorization
- [ ] Test role-based authorization
- [ ] Test policy-based authorization
- [ ] Test custom authorization handlers
- [ ] Test tool-level vs controller-level authorization
- [ ] Test authorization with JWT claims

### Success Criteria
- ✓ Tools respect `[Authorize]` attributes
- ✓ Role checks work correctly
- ✓ Policy checks work correctly
- ✓ Unauthorized access returns 403
- ✓ Authorization integrates with JWT authentication

---

## Phase 5: Advanced Features
**Goal**: Additional features for production use

### Sub-Phase 5.1: Logging & Diagnostics
- [ ] Add structured logging for tool invocation
- [ ] Log authentication/authorization events
- [ ] Add metrics/telemetry support (optional)
- [ ] Create diagnostic middleware
- [ ] Add request/response logging (with PII filtering)

### Sub-Phase 5.2: Error Handling
- [ ] Standardized error responses
- [ ] Exception handling middleware
- [ ] Validation error mapping
- [ ] HTTP status code mapping
- [ ] Error detail configuration (dev vs prod)

### Sub-Phase 5.3: Performance
- [ ] Tool caching (if applicable)
- [ ] Response compression
- [ ] Async/await optimization
- [ ] Memory usage profiling
- [ ] Load testing

### Success Criteria
- ✓ Comprehensive logging
- ✓ Clear error messages
- ✓ Good performance under load
- ✓ No memory leaks

---

## Phase 6: Documentation & Examples
**Goal**: Excellent developer experience

### Tasks
- [ ] Create README.md with:
  - Quick start
  - Installation instructions
  - Basic usage
  - Configuration options
  - Authentication setup
  - Authorization setup
- [ ] Create examples:
  - `examples/BasicExample/` - Simple MCP API
  - `examples/AuthenticatedExample/` - With JWT
  - `examples/AuthorizedExample/` - With roles/policies
  - `examples/AdvancedExample/` - All features
- [ ] Create wiki/docs site (optional)
- [ ] Add XML documentation comments
- [ ] Create migration guide from POC to library
- [ ] Add troubleshooting guide

### Success Criteria
- ✓ Clear, comprehensive README
- ✓ Working example projects
- ✓ All public APIs documented
- ✓ Easy to get started

---

## Phase 7: Testing & Quality
**Goal**: Production-ready quality

### Sub-Phase 7.1: Unit Tests
- [ ] Test ActionResult unwrapping
- [ ] Test snake_case conversion
- [ ] Test options validation
- [ ] Test authentication logic
- [ ] Test authorization logic
- [ ] Achieve >80% code coverage

### Sub-Phase 7.2: Integration Tests
- [ ] Test full MCP flow with TestServer
- [ ] Test with real HTTP client
- [ ] Test authentication integration
- [ ] Test authorization integration
- [ ] Test error scenarios

### Sub-Phase 7.3: End-to-End Tests
- [ ] Create sample application
- [ ] Test with MCP client (like Claude Code)
- [ ] Test all example projects
- [ ] Performance testing
- [ ] Security testing

### Success Criteria
- ✓ Comprehensive test coverage
- ✓ All tests pass
- ✓ No known bugs
- ✓ Good performance

---

## Phase 8: Packaging & Distribution
**Goal**: Published to NuGet, ready for use

### Tasks
- [ ] Set up NuGet package configuration
- [ ] Configure package versioning (SemVer)
- [ ] Create package icon/logo
- [ ] Create LICENSE file (MIT?)
- [ ] Create CHANGELOG.md
- [ ] Set up CI/CD pipeline (if available)
- [ ] Pack library: `dotnet pack`
- [ ] Test package locally
- [ ] Publish to your NuGet repo
- [ ] Create GitHub releases (if using GitHub)
- [ ] Announce on relevant channels

### Success Criteria
- ✓ Package available on your NuGet repo
- ✓ Package installs correctly
- ✓ Examples work with package
- ✓ Documentation references package

---

## Phase 9: Maintenance & Improvements
**Goal**: Keep library up-to-date and improved

### Ongoing Tasks
- [ ] Monitor MCP SDK updates
- [ ] Handle SDK breaking changes
- [ ] Review and merge community PRs (if open source)
- [ ] Fix reported bugs
- [ ] Add requested features
- [ ] Update documentation
- [ ] Performance improvements
- [ ] Security updates

---

## Technical Decisions

### Package Dependencies
```xml
<PackageReference Include="ModelContextProtocol" Version="0.4.0-preview.3" />
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />
<PackageReference Include="Microsoft.Extensions.Options" Version="9.0.0" />
```

### Target Framework
- `net9.0` (primary)
- `net8.0` (optional, for wider compatibility)

### Naming Conventions
- Namespace: `Zerox.Mcp.Extensions`
- Main class: `ZeroxMcpServiceCollectionExtensions`
- Options: `ZeroxMcpOptions`, `ZeroxMcpAuthenticationOptions`, etc.

### API Design Principles
1. **Convention over Configuration** - Sensible defaults
2. **Progressive Disclosure** - Simple API, advanced options available
3. **Explicit is Better** - Clear method names, no magic
4. **Fail Fast** - Validate early, clear error messages

---

## Risk Assessment

### High Risk
- ⚠️ MCP SDK is preview (breaking changes possible)
- ⚠️ Authentication/Authorization complexity
- ⚠️ Security vulnerabilities if not done carefully

### Medium Risk
- ⚠️ Performance under load
- ⚠️ Memory leaks in long-running scenarios
- ⚠️ Breaking changes between versions

### Mitigation
- ✓ Comprehensive testing
- ✓ Version pinning with ranges
- ✓ Security review before release
- ✓ Performance profiling
- ✓ Clear migration guides

---

## Success Metrics

### Library Adoption
- [ ] Used in at least 3 internal projects
- [ ] Positive user feedback
- [ ] No critical bugs reported

### Code Quality
- [ ] >80% test coverage
- [ ] No critical security issues
- [ ] Good performance benchmarks
- [ ] Clean code analysis results

### Developer Experience
- [ ] Setup time < 5 minutes
- [ ] Clear documentation
- [ ] Good examples
- [ ] Easy troubleshooting

---

## Timeline Estimate

- **Phase 1**: 2-4 hours (Core library)
- **Phase 2**: 2-3 hours (Configuration)
- **Phase 3**: 8-12 hours (Authentication - complex!)
- **Phase 4**: 8-12 hours (Authorization - complex!)
- **Phase 5**: 6-8 hours (Advanced features)
- **Phase 6**: 4-6 hours (Documentation)
- **Phase 7**: 8-10 hours (Testing)
- **Phase 8**: 2-4 hours (Packaging)
- **Phase 9**: Ongoing

**Total**: ~40-60 hours spread across multiple sessions

---

## Next Steps

1. **Immediate**: Create Phase 1 (Core library) and test with POC
2. **Short-term**: Add configuration (Phase 2)
3. **Medium-term**: Authentication (Phase 3) - research required
4. **Long-term**: Authorization and advanced features

---

## Notes

- This is a **multi-session project** - expect 8-12 sessions
- Each phase should be tested before moving to next
- Authentication/Authorization are most complex parts
- Can release early versions without all features
- Keep POC as integration test reference
- Update Memory Bank after each major phase

---

## Open Questions

1. Should we support API Key authentication in addition to JWT?
2. Should we include rate limiting?
3. Should we support multiple authentication schemes simultaneously?
4. Should we create separate packages (Core + Auth)?
5. What NuGet repository will we use? (Azure Artifacts, MyGet, private?)
6. Open source or proprietary?
7. What license? (MIT, Apache, proprietary?)

---

**Status**: Planning Complete - Ready for Phase 1
**Created**: 2025-10-22
**Last Updated**: 2025-10-22
