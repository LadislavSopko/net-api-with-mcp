# POC Phase 2: Authentication Testing

## Goal
Prove that JWT Bearer tokens work with MCP protocol + ASP.NET Core controllers.

## Approach: Simple Local JWT (No Keycloak Yet)

### Why Local JWT First?
- ✅ Fast to implement
- ✅ No external dependencies
- ✅ Easy to test
- ✅ Proves the concept
- Later: Can add Keycloak/IdentityServer for real scenarios

## Implementation Plan

### Step 1: Add JWT Dependencies
```xml
<PackageReference Include="Microsoft.AspNetCore.Authentication.JwtBearer" Version="9.0.0" />
<PackageReference Include="System.IdentityModel.Tokens.Jwt" Version="8.2.1" />
```

### Step 2: Create JWT Token Generator
`src/McpPoc.Api/Auth/JwtTokenGenerator.cs`
- Generate symmetric key
- Create tokens with claims (sub, name, role, etc.)
- Configurable expiration
- For testing only (not production!)

### Step 3: Configure Authentication in Program.cs
```csharp
builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
    .AddJwtBearer(options =>
    {
        options.TokenValidationParameters = new TokenValidationParameters
        {
            ValidateIssuer = true,
            ValidateAudience = true,
            ValidateLifetime = true,
            ValidateIssuerSigningKey = true,
            ValidIssuer = "McpPocApi",
            ValidAudience = "McpPocApi",
            IssuerSigningKey = new SymmetricSecurityKey(key)
        };
    });

builder.Services.AddAuthorization();
```

### Step 4: Configure Swagger with JWT
```csharp
builder.Services.AddSwaggerGen(c =>
{
    c.AddSecurityDefinition("Bearer", new OpenApiSecurityScheme
    {
        Description = "JWT Authorization header using the Bearer scheme.",
        Name = "Authorization",
        In = ParameterLocation.Header,
        Type = SecuritySchemeType.Http,
        Scheme = "bearer"
    });

    c.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiReference
            {
                Type = ReferenceType.SecurityScheme,
                Id = "Bearer"
            },
            Array.Empty<string>()
        }
    });
});
```

### Step 5: Add [Authorize] to Test Endpoint
```csharp
[McpServerTool]
[Authorize]
public async Task<ActionResult<User>> GetById(int id)
{
    var user = await _userService.GetByIdAsync(id);
    if (user == null)
        return NotFound();
    return Ok(user);
}
```

### Step 6: Add Token Generator Endpoint (for testing)
```csharp
[HttpPost("auth/token")]
public IActionResult GenerateToken([FromBody] TokenRequest request)
{
    var token = JwtTokenGenerator.Generate(request.Username);
    return Ok(new { token });
}
```

### Step 7: Update MCP Endpoint (Optional - if needed)
```csharp
app.MapMcp("/mcp").RequireAuthorization(); // or leave open
```

### Step 8: Test Scenarios

#### HTTP Tests (via Swagger)
1. Call `/auth/token` to get JWT
2. Use JWT in Swagger "Authorize" button
3. Call protected endpoint → Success
4. Call without token → 401

#### MCP Tests
1. MCP client calls tool without token → 401?
2. MCP client calls tool with token → Success?
3. Verify HttpContext.User is populated
4. Check claims in controller

## Test Implementation

### New Test File: `AuthenticationTests.cs`
```csharp
public class AuthenticationTests : IClassFixture<McpApiFixture>
{
    [Fact]
    public async Task Should_Return401_WhenNoTokenProvided()
    {
        // Call get_by_id without token
        // Assert: 401 Unauthorized
    }

    [Fact]
    public async Task Should_ReturnData_WhenValidTokenProvided()
    {
        // Generate token
        // Call get_by_id with token
        // Assert: 200 + data
    }

    [Fact]
    public async Task Should_Return401_WhenInvalidTokenProvided()
    {
        // Use invalid/expired token
        // Call get_by_id
        // Assert: 401
    }

    [Fact]
    public async Task Should_PopulateHttpContextUser_WhenTokenValid()
    {
        // Call endpoint with token
        // Verify User.Identity.Name is set
        // Verify claims are present
    }
}
```

## Questions to Answer

1. ✅ Does MCP SDK pass Authorization header to ASP.NET Core?
2. ✅ Does JWT Bearer auth work with MCP transport?
3. ✅ Is HttpContext.User populated correctly?
4. ✅ Do [Authorize] attributes work on MCP tools?
5. ✅ Do role/policy checks work?

## Success Criteria

- [ ] JWT tokens can be generated
- [ ] Swagger accepts JWT tokens
- [ ] HTTP API respects [Authorize]
- [ ] MCP tools respect [Authorize]
- [ ] 401 returned when no/invalid token
- [ ] 200 returned when valid token
- [ ] HttpContext.User populated with claims
- [ ] Tests pass for all scenarios

## Next Steps After Success

If this works:
1. Extract auth logic to Zerox.Mcp.Extensions library
2. Add support for external providers (Keycloak, Azure AD, etc.)
3. Add policy-based authorization
4. Add role-based authorization
5. Add refresh tokens
6. Add proper key management

## If It Doesn't Work

Debug:
1. Check MCP SDK authentication handler
2. Check how MCP client sends tokens
3. Check middleware order
4. Check if MCP bypasses authentication somehow
5. May need custom authentication scheme

---

**Status**: Planning Complete
**Estimated Time**: 2-3 hours
**Risk**: Medium (unknown if MCP SDK propagates auth correctly)
