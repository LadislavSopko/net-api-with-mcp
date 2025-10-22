# Test: MCP SDK with ASP.NET Core Controllers

## Objective

Create a minimal .NET API project to test if the MCP SDK can automatically discover and expose controller methods as MCP tools when using `[McpServerToolType]` and `[McpServerTool]` attributes directly on controllers.

## Hypothesis

If we add `[McpServerToolType]` to a controller class and `[McpServerTool]` to its methods, the MCP SDK's `.WithToolsFromAssembly()` should automatically discover and expose them as MCP tools without needing a separate bridge library.

## Requirements

1. Create a new .NET 8 or 9 Web API project
2. Add MCP SDK with HTTP transport
3. Create a controller with MCP attributes
4. Test if it works
5. Document findings

## Implementation Steps

### Step 1: Create New Project

```bash
# Create solution directory
mkdir McpControllerTest
cd McpControllerTest

# Create Web API project
dotnet new webapi -n McpControllerTest.Api --use-controllers
cd McpControllerTest.Api

# Add required packages
dotnet add package ModelContextProtocol --prerelease
dotnet add package Microsoft.AspNetCore.Authentication.JwtBearer
```

### Step 2: Create a Simple Model

**File: `Models/User.cs`**

```csharp
namespace McpControllerTest.Api.Models;

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
```

### Step 3: Create a Simple Service

**File: `Services/IUserService.cs`**

```csharp
using McpControllerTest.Api.Models;

namespace McpControllerTest.Api.Services;

public interface IUserService
{
    Task<User?> GetByIdAsync(int id);
    Task<List<User>> GetAllAsync();
    Task<User> CreateAsync(string name, string email);
}

public class UserService : IUserService
{
    private readonly List<User> _users = new()
    {
        new User { Id = 1, Name = "Alice Smith", Email = "alice@example.com" },
        new User { Id = 2, Name = "Bob Jones", Email = "bob@example.com" },
        new User { Id = 3, Name = "Carol White", Email = "carol@example.com" }
    };

    public Task<User?> GetByIdAsync(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(user);
    }

    public Task<List<User>> GetAllAsync()
    {
        return Task.FromResult(_users.ToList());
    }

    public Task<User> CreateAsync(string name, string email)
    {
        var user = new User
        {
            Id = _users.Max(u => u.Id) + 1,
            Name = name,
            Email = email
        };
        _users.Add(user);
        return Task.FromResult(user);
    }
}
```

### Step 4: Create Controller with MCP Attributes (THE TEST!)

**File: `Controllers/UsersController.cs`**

```csharp
using McpControllerTest.Api.Models;
using McpControllerTest.Api.Services;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;
using System.ComponentModel;

namespace McpControllerTest.Api.Controllers;

/// <summary>
/// TEST: Adding [McpServerToolType] directly to controller
/// </summary>
[ApiController]
[Route("api/[controller]")]
[McpServerToolType]  // ← TESTING THIS!
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;

    public UsersController(IUserService userService, ILogger<UsersController> logger)
    {
        _userService = userService;
        _logger = logger;
    }

    /// <summary>
    /// TEST: Regular HTTP endpoint + MCP tool
    /// </summary>
    [HttpGet("{id}")]
    [McpServerTool, Description("Gets a user by their ID")]  // ← TESTING THIS!
    public async Task<ActionResult<User>> GetById(int id)
    {
        _logger.LogInformation("GetById called with id: {Id}", id);
        
        var user = await _userService.GetByIdAsync(id);
        
        if (user == null)
        {
            return NotFound(new { error = "User not found", id });
        }
        
        return Ok(user);
    }

    /// <summary>
    /// TEST: List endpoint + MCP tool
    /// </summary>
    [HttpGet]
    [McpServerTool, Description("Gets all users")]  // ← TESTING THIS!
    public async Task<ActionResult<List<User>>> GetAll()
    {
        _logger.LogInformation("GetAll called");
        
        var users = await _userService.GetAllAsync();
        return Ok(users);
    }

    /// <summary>
    /// TEST: Create endpoint + MCP tool with parameters
    /// </summary>
    [HttpPost]
    [McpServerTool, Description("Creates a new user")]  // ← TESTING THIS!
    public async Task<ActionResult<User>> Create(
        [Description("User's full name")] string name,
        [Description("User's email address")] string email)
    {
        _logger.LogInformation("Create called with name: {Name}, email: {Email}", name, email);
        
        var user = await _userService.CreateAsync(name, email);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    /// <summary>
    /// Regular HTTP endpoint WITHOUT MCP tool
    /// </summary>
    [HttpDelete("{id}")]
    public async Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("Delete called (NOT an MCP tool) with id: {Id}", id);
        return NoContent();
    }
}
```

### Step 5: Configure Program.cs

**File: `Program.cs`**

```csharp
using McpControllerTest.Api.Services;
using ModelContextProtocol.Server;

var builder = WebApplication.CreateBuilder(args);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();
builder.Services.AddSwaggerGen();

// Register user service
builder.Services.AddSingleton<IUserService, UserService>();

// Add HTTP context accessor (might be needed)
builder.Services.AddHttpContextAccessor();

// ============================================
// TEST: Add MCP Server with HTTP transport
// ============================================
builder.Services
    .AddMcpServer()
    .WithHttpServerTransport()  // HTTP transport for full pipeline
    .WithToolsFromAssembly();   // Should discover controller with [McpServerToolType]

var app = builder.Build();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI();
}

app.UseHttpsRedirection();
app.UseAuthorization();
app.MapControllers();

// ============================================
// TEST: Map MCP endpoint
// ============================================
app.MapMcp();  // This should expose MCP at /mcp

app.Logger.LogInformation("===========================================");
app.Logger.LogInformation("TEST: MCP + Controller Integration");
app.Logger.LogInformation("HTTP API: https://localhost:5001/api/users");
app.Logger.LogInformation("MCP Endpoint: https://localhost:5001/mcp");
app.Logger.LogInformation("Swagger: https://localhost:5001/swagger");
app.Logger.LogInformation("===========================================");

app.Run();
```

### Step 6: Update launchSettings.json

**File: `Properties/launchSettings.json`**

```json
{
  "$schema": "http://json.schemastore.org/launchsettings.json",
  "profiles": {
    "http": {
      "commandName": "Project",
      "dotnetRunMessages": true,
      "launchBrowser": true,
      "launchUrl": "swagger",
      "applicationUrl": "http://localhost:5001",
      "environmentVariables": {
        "ASPNETCORE_ENVIRONMENT": "Development"
      }
    }
  }
}
```

## Testing Instructions

### Test 1: Verify HTTP API Works

```bash
# Run the application
dotnet run

# Test HTTP endpoints
curl http://localhost:5001/api/users
curl http://localhost:5001/api/users/1
curl -X POST http://localhost:5001/api/users \
  -H "Content-Type: application/json" \
  -d '{"name":"Dave","email":"dave@example.com"}'
```

### Test 2: Check MCP Tool Discovery

**Method A: Check logs**

When the app starts, look for log messages indicating MCP tools were discovered:
- Look for messages about tool registration
- Count how many tools were found
- Note any errors about tool discovery

**Method B: Use MCP Inspector**

```bash
# Install MCP Inspector
npm install -g @modelcontextprotocol/inspector

# Test the MCP server
npx @modelcontextprotocol/inspector http://localhost:5001/mcp
```

**Method C: Manual HTTP Request**

```bash
# List available MCP tools
curl -X POST http://localhost:5001/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 1,
    "method": "tools/list"
  }'
```

Expected response if it works:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "tools": [
      {
        "name": "GetById",
        "description": "Gets a user by their ID",
        "inputSchema": { ... }
      },
      {
        "name": "GetAll",
        "description": "Gets all users",
        "inputSchema": { ... }
      },
      {
        "name": "Create",
        "description": "Creates a new user",
        "inputSchema": { ... }
      }
    ]
  }
}
```

**Method D: Call an MCP Tool**

```bash
# Call the GetById tool
curl -X POST http://localhost:5001/mcp \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "id": 2,
    "method": "tools/call",
    "params": {
      "name": "GetById",
      "arguments": {
        "id": 1
      }
    }
  }'
```

## Expected Outcomes

### ✅ SUCCESS Scenario:

1. App starts without errors
2. `tools/list` returns 3 tools (GetById, GetAll, Create)
3. Calling tools returns user data
4. HTTP endpoints still work normally
5. Delete endpoint NOT exposed as MCP tool

**Conclusion**: Controllers can be MCP tools directly! No bridge library needed!

### ❌ FAILURE Scenarios:

**Scenario A: No tools discovered**
- `tools/list` returns empty array
- Logs show 0 tools registered
- **Reason**: SDK doesn't scan controller classes

**Scenario B: Discovery error**
- Exception during startup
- Error about incompatible types
- **Reason**: Controllers incompatible with MCP tool system

**Scenario C: ActionResult not handled**
- Tools discovered but fail when called
- Error about return type
- **Reason**: SDK doesn't understand `ActionResult<T>`

**Scenario D: DI doesn't work**
- Controller constructor not called
- NullReferenceException in tool
- **Reason**: DI not working for controller instances

## Documentation

After testing, document the findings:

### Create `TEST-RESULTS.md`:

```markdown
# MCP + Controller Integration Test Results

## Date: [DATE]
## SDK Version: [VERSION]

## Test Setup
- .NET Version: [VERSION]
- ModelContextProtocol Package: [VERSION]

## Results

### Test 1: Tool Discovery
- [ ] Tools discovered: YES / NO
- [ ] Number of tools found: [NUMBER]
- [ ] Tool names: [LIST]

### Test 2: Tool Invocation
- [ ] GetById works: YES / NO
- [ ] GetAll works: YES / NO
- [ ] Create works: YES / NO
- [ ] ActionResult unwrapped correctly: YES / NO

### Test 3: HTTP Endpoints
- [ ] HTTP GET still works: YES / NO
- [ ] HTTP POST still works: YES / NO
- [ ] Delete (no MCP attribute) not exposed: YES / NO

### Test 4: Dependency Injection
- [ ] IUserService injected: YES / NO
- [ ] ILogger injected: YES / NO
- [ ] IHttpContextAccessor available: YES / NO

## Conclusion

[SUCCESS / FAILURE]

[DETAILED EXPLANATION]

## Recommendations

If SUCCESS:
- Document this approach
- Can use directly in production
- No bridge library needed

If FAILURE:
- Need to build bridge library
- Document specific issues found
- Propose workaround solutions
```

## Cleanup

```bash
# Remove test project when done
cd ..
rm -rf McpControllerTest
```

## Next Steps

Based on test results:

### If it WORKS:
1. Document the pattern
2. Apply to DocFlowPro directly
3. Add authentication testing
4. Test with JWT tokens

### If it DOESN'T WORK:
1. Analyze the specific failure mode
2. Design the bridge library accordingly
3. Focus on solving the specific problem found
4. Implement minimal solution needed

## Success Criteria

This test is successful if we can definitively answer:

**"Can ASP.NET Core controllers with `[McpServerToolType]` and `[McpServerTool]` attributes be automatically discovered and exposed as MCP tools by the SDK's `.WithToolsFromAssembly()` method?"**

- ✅ YES → No library needed, just add attributes
- ❌ NO → Build targeted bridge library for specific issue

---

## Summary

This test project will take ~15 minutes to create and test, and will give us a definitive answer about whether we need a bridge library or if we can use controllers directly as MCP tools.

**The answer will guide the entire implementation strategy!** 🎯