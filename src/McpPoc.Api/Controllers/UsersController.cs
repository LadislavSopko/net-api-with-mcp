using McpPoc.Api.Authorization;
using McpPoc.Api.Models;
using McpPoc.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zero.Mcp.Extensions;
using ModelContextProtocol.Server;
using System.ComponentModel;
using Microsoft.Extensions.Primitives;

namespace McpPoc.Api.Controllers;

/// <summary>
/// TEST: Adding [McpServerToolType] directly to controller with Keycloak authentication
/// </summary>
[ApiController]
[Route("api/[controller]")]
[Authorize]  // ← Authentication required
[McpServerToolType]  // ← TESTING THIS!
public class UsersController : ControllerBase
{
    private readonly IUserService _userService;
    private readonly ILogger<UsersController> _logger;
    private readonly IScopedRequestTracker _scopedTracker;
    private readonly IMcpRequestContext _mcpContext;

    public UsersController(
        IUserService userService,
        ILogger<UsersController> logger,
        IScopedRequestTracker scopedTracker,
        IMcpRequestContext mcpContext)
    {
        _userService = userService;
        _logger = logger;
        _scopedTracker = scopedTracker;
        _mcpContext = mcpContext;
    }

    /// <summary>
    /// TEST: Regular HTTP endpoint + MCP tool
    /// </summary>
    [HttpGet("{id}")]
    [McpServerTool(Name = "UserGetById", UseStructuredContent = true, OutputSchemaType = typeof(User)), Description("Gets a user by their ID")]  // ← TESTING THIS!
    public async Task<ActionResult<User>> GetById(int id)
    {
        _logger.LogInformation("GetById called with id: {Id} call done by mcp: {isMcp}", id, _mcpContext.IsMcpCall);


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
    /// TEST: Create endpoint + MCP tool with parameters - requires Member role
    /// </summary>
    [HttpPost]
    [McpServerTool, Description("Creates a new user - requires Member role")]
    [Authorize(Policy = PolicyNames.RequireMember)]
    public async Task<ActionResult<User>> Create(
        [Description("User creation data")] CreateUserRequest request)
    {
        _logger.LogInformation("Create called with name: {Name}, email: {Email}", request.Name, request.Email);

        var user = await _userService.CreateAsync(request.Name, request.Email);
        return CreatedAtAction(nameof(GetById), new { id = user.Id }, user);
    }

    /// <summary>
    /// TEST: Update endpoint + MCP tool - requires Manager role
    /// </summary>
    [HttpPut("{id}")]
    [McpServerTool, Description("Updates a user - requires Manager role")]
    [Authorize(Policy = PolicyNames.RequireManager)]
    public async Task<ActionResult<User>> Update(
        int id,
        [Description("User update data")] UpdateUserRequest request)
    {
        _logger.LogInformation("Update called for id: {Id}", id);

        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { error = "User not found", id });
        }

        user.Name = request.Name;
        user.Email = request.Email;

        return Ok(user);
    }

    /// <summary>
    /// TEST: Promote user to Manager - requires Admin role
    /// </summary>
    [HttpPost("{id}/promote")]
    [McpServerTool, Description("Promotes a user to Manager - requires Admin role")]
    [Authorize(Policy = PolicyNames.RequireAdmin)]
    public async Task<ActionResult<User>> PromoteToManager(int id)
    {
        _logger.LogInformation("Promote called for id: {Id}", id);

        var user = await _userService.GetByIdAsync(id);
        if (user == null)
        {
            return NotFound(new { error = "User not found", id });
        }

        user.Role = UserRole.Manager;
        return Ok(user);
    }

    /// <summary>
    /// TEST: DI Scoping verification - returns unique scope ID
    /// Each call should return a DIFFERENT ID if scoping works correctly
    /// </summary>
    [HttpGet("scope-test")]
    [McpServerTool, Description("Returns the current request scope ID for DI testing")]
    public ActionResult<ScopeIdResponse> GetScopeId()
    {
        _logger.LogInformation("GetScopeId called - RequestId: {RequestId}", _scopedTracker.RequestId);

        var response = new ScopeIdResponse(
            _scopedTracker.RequestId,
            _scopedTracker.CreatedAt,
            "Each call should return a different ID if scoping works correctly"
        );

        return Ok(response);
    }

    /// <summary>
    /// Public information endpoint for testing [AllowAnonymous] with MCP.
    /// </summary>
    [HttpGet("public")]
    [McpServerTool, Description("Gets public information without authentication")]
    [AllowAnonymous]
    public ActionResult<object> GetPublicInfo()
    {
        return Ok(new
        {
            message = "This is public information accessible without authentication",
            timestamp = DateTime.UtcNow,
            serverVersion = "1.8.0"
        });
    }

    /// <summary>
    /// Diagnostic endpoint to verify IMcpRequestContext works.
    /// Returns information about the current request context.
    /// </summary>
    [HttpGet("mcp-context")]
    [McpServerTool, Description("Returns MCP request context information for diagnostics")]
    public ActionResult<McpContextInfo> GetMcpContext()
    {
        var xMcpCallHeader = _mcpContext.GetHeader("x-mcp-call");

        _logger.LogInformation(
            "GetMcpContext called - IsMcpCall: {IsMcpCall}, x-mcp-call: {XMcpCall}",
            _mcpContext.IsMcpCall,
            xMcpCallHeader);

        return Ok(new McpContextInfo(
            _mcpContext.IsMcpCall,
            xMcpCallHeader,
            _mcpContext.Headers?.Count ?? 0
        ));
    }

    /// <summary>
    /// Diagnostic endpoint to check tool registration.
    /// </summary>
    [HttpGet("tool-diagnostics")]
    [AllowAnonymous]
    public ActionResult<ToolDiagnosticsResponse> GetToolDiagnostics()
    {
        var assembly = typeof(UsersController).Assembly;
        var toolTypes = assembly.GetTypes()
            .Where(t => t.GetCustomAttributes(typeof(McpServerToolTypeAttribute), false).Any())
            .Select(t => t.FullName ?? t.Name)
            .ToList();

        var toolMethods = new List<string>();
        foreach (var type in assembly.GetTypes().Where(t => t.GetCustomAttributes(typeof(McpServerToolTypeAttribute), false).Any()))
        {
            var methods = type.GetMethods()
                .Where(m => m.GetCustomAttributes(typeof(McpServerToolAttribute), false).Any())
                .Select(m => $"{type.Name}.{m.Name}");
            toolMethods.AddRange(methods);
        }

        return Ok(new ToolDiagnosticsResponse(assembly.FullName ?? "unknown", toolTypes, toolMethods));
    }

    /// <summary>
    /// Echo all request headers - useful for testing MCP header forwarding.
    /// </summary>
    [HttpGet("echo-headers")]
    [McpServerTool, Description("Returns all request headers with their values - for testing")]
    [AllowAnonymous]
    public ActionResult<EchoHeadersResponse> EchoHeaders()
    {
        var headers = new Dictionary<string, string>();

        // For MCP calls, use IMcpRequestContext
        if (_mcpContext.IsMcpCall && _mcpContext.Headers != null)
        {
            foreach (var header in _mcpContext.Headers)
            {
                headers[header.Key] = header.Value.ToString();
            }
        }
        else
        {
            // For HTTP calls, use HttpContext directly
            foreach (var header in Request.Headers)
            {
                headers[header.Key] = header.Value.ToString();
            }
        }

        _logger.LogInformation("EchoHeaders called - IsMcpCall: {IsMcpCall}, HeaderCount: {Count}",
            _mcpContext.IsMcpCall, headers.Count);

        return Ok(new EchoHeadersResponse(_mcpContext.IsMcpCall, headers));
    }

    /// <summary>
    /// Regular HTTP endpoint WITHOUT MCP tool
    /// </summary>
    [HttpDelete("{id}")]
    public Task<IActionResult> Delete(int id)
    {
        _logger.LogInformation("Delete called (NOT an MCP tool) with id: {Id}", id);
        return Task.FromResult((IActionResult)NoContent());
    }
}

/// <summary>
/// Response containing MCP context information for diagnostics.
/// </summary>
public record McpContextInfo(bool IsMcpCall, string? XMcpCallHeader, int HeaderCount);

/// <summary>
/// Response for GetScopeId tool - used to verify DI scoping works correctly
/// </summary>
public record ScopeIdResponse(Guid RequestId, DateTime CreatedAt, string Message);

/// <summary>
/// Request for creating a user
/// </summary>
public record CreateUserRequest(string Name, string Email);

/// <summary>
/// Request for updating a user
/// </summary>
public record UpdateUserRequest(string Name, string Email);

/// <summary>
/// Response for EchoHeaders tool - returns all request headers
/// </summary>
public record EchoHeadersResponse(bool IsMcpCall, Dictionary<string, string> Headers);

/// <summary>
/// Response for tool diagnostics
/// </summary>
public record ToolDiagnosticsResponse(string AssemblyName, List<string> ToolTypes, List<string> ToolMethods);
