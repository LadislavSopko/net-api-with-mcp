using McpPoc.Api.Authorization;
using McpPoc.Api.Models;
using McpPoc.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Zero.Mcp.Extensions;
using System.ComponentModel;

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

    public UsersController(IUserService userService, ILogger<UsersController> logger, IScopedRequestTracker scopedTracker)
    {
        _userService = userService;
        _logger = logger;
        _scopedTracker = scopedTracker;
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
