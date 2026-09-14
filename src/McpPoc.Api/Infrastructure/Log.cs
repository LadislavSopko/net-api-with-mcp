using McpPoc.Api.Models;

namespace McpPoc.Api.Infrastructure;

/// <summary>
/// Source-generated, high-performance log messages (CA1848) for the API project.
/// Message templates and levels are identical to the previous direct ILogger calls.
/// </summary>
internal static partial class Log
{
    // ----- UsersController -----

    [LoggerMessage(EventId = 1000, Level = LogLevel.Information, Message = "GetById called with id: {Id} call done by mcp: {IsMcp}")]
    public static partial void GetByIdCalled(ILogger logger, int id, bool isMcp);

    [LoggerMessage(EventId = 1001, Level = LogLevel.Information, Message = "GetAll called")]
    public static partial void GetAllCalled(ILogger logger);

    [LoggerMessage(EventId = 1002, Level = LogLevel.Information, Message = "Create called with name: {Name}, email: {Email}")]
    public static partial void CreateCalled(ILogger logger, string name, string email);

    [LoggerMessage(EventId = 1003, Level = LogLevel.Information, Message = "Update called for id: {Id}")]
    public static partial void UpdateCalled(ILogger logger, int id);

    [LoggerMessage(EventId = 1004, Level = LogLevel.Information, Message = "Promote called for id: {Id}")]
    public static partial void PromoteCalled(ILogger logger, int id);

    [LoggerMessage(EventId = 1005, Level = LogLevel.Information, Message = "GetScopeId called - RequestId: {RequestId}")]
    public static partial void GetScopeIdCalled(ILogger logger, Guid requestId);

    [LoggerMessage(EventId = 1006, Level = LogLevel.Information, Message = "GetMcpContext called - IsMcpCall: {IsMcpCall}, x-mcp-call: {XMcpCall}")]
    public static partial void GetMcpContextCalled(ILogger logger, bool isMcpCall, string? xMcpCall);

    [LoggerMessage(EventId = 1007, Level = LogLevel.Information, Message = "EchoHeaders called - IsMcpCall: {IsMcpCall}, HeaderCount: {Count}")]
    public static partial void EchoHeadersCalled(ILogger logger, bool isMcpCall, int count);

    [LoggerMessage(EventId = 1008, Level = LogLevel.Information, Message = "Delete called (NOT an MCP tool) with id: {Id}")]
    public static partial void DeleteCalled(ILogger logger, int id);

    // ----- MinimumRoleRequirementHandler -----

    [LoggerMessage(EventId = 2000, Level = LogLevel.Trace, Message = "HandleRequirementAsync called for requirement: {MinRole}")]
    public static partial void HandleRequirementCalled(ILogger logger, UserRole minRole);

    [LoggerMessage(EventId = 2001, Level = LogLevel.Warning, Message = "User is not authenticated")]
    public static partial void UserNotAuthenticated(ILogger logger);

    [LoggerMessage(EventId = 2002, Level = LogLevel.Trace, Message = "User is authenticated")]
    public static partial void UserAuthenticated(ILogger logger);

    [LoggerMessage(EventId = 2003, Level = LogLevel.Warning, Message = "No preferred_username claim found in token")]
    public static partial void NoPreferredUsernameClaim(ILogger logger);

    [LoggerMessage(EventId = 2004, Level = LogLevel.Trace, Message = "Found preferred_username claim: {Username}")]
    public static partial void FoundPreferredUsernameClaim(ILogger logger, string username);

    [LoggerMessage(EventId = 2005, Level = LogLevel.Trace, Message = "GetAllAsync returned {Count} users")]
    public static partial void GetAllAsyncReturned(ILogger logger, int count);

    [LoggerMessage(EventId = 2006, Level = LogLevel.Trace, Message = "  User in list: Id={Id}, Email={Email}, Role={Role}")]
    public static partial void UserInList(ILogger logger, int id, string email, UserRole role);

    [LoggerMessage(EventId = 2007, Level = LogLevel.Warning, Message = "User not found in UserService for username: {Username}")]
    public static partial void UserNotFoundForUsername(ILogger logger, string username);

    [LoggerMessage(EventId = 2008, Level = LogLevel.Trace, Message = "Found user: Id={Id}, Name={Name}, Email={Email}, Role={Role}")]
    public static partial void FoundUser(ILogger logger, int id, string name, string email, UserRole role);

    [LoggerMessage(EventId = 2009, Level = LogLevel.Information, Message = "User {Email} with role {Role} meets minimum role {MinRole}")]
    public static partial void UserMeetsMinimumRole(ILogger logger, string email, UserRole role, UserRole minRole);

    [LoggerMessage(EventId = 2010, Level = LogLevel.Warning, Message = "User {Username} with role {Role} does NOT meet minimum role {MinRole}")]
    public static partial void UserDoesNotMeetMinimumRole(ILogger logger, string username, UserRole role, UserRole minRole);

    // ----- Program (JwtBearer events + startup banner) -----

    [LoggerMessage(EventId = 3000, Level = LogLevel.Error, Message = "Authentication failed")]
    public static partial void AuthenticationFailed(ILogger logger, Exception exception);

    [LoggerMessage(EventId = 3001, Level = LogLevel.Information, Message = "Token validated for user: {User}")]
    public static partial void TokenValidated(ILogger logger, string user);

    [LoggerMessage(EventId = 3002, Level = LogLevel.Information, Message = "===========================================")]
    public static partial void BannerSeparator(ILogger logger);

    [LoggerMessage(EventId = 3003, Level = LogLevel.Information, Message = "MCP POC API")]
    public static partial void BannerTitle(ILogger logger);

    [LoggerMessage(EventId = 3004, Level = LogLevel.Information, Message = "HTTP API: http://127.0.0.1:5001/api/users")]
    public static partial void BannerHttpApi(ILogger logger);

    [LoggerMessage(EventId = 3005, Level = LogLevel.Information, Message = "MCP Endpoint: http://127.0.0.1:5001/mcp")]
    public static partial void BannerMcpEndpoint(ILogger logger);

    [LoggerMessage(EventId = 3006, Level = LogLevel.Information, Message = "Scalar UI: http://127.0.0.1:5001/scalar")]
    public static partial void BannerScalarUi(ILogger logger);
}
