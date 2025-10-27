using McpPoc.Api.Models;
using McpPoc.Api.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;
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
