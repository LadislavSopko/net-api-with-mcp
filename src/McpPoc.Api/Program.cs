using McpPoc.Api.Authorization;
using McpPoc.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Scalar.AspNetCore;
using Serilog;
using ModelContextProtocol.AspNetCore;
using Zero.Mcp.Extensions;
using AppLog = McpPoc.Api.Infrastructure.Log;

// Configure Serilog for file logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Information()
    .WriteTo.Console(formatProvider: System.Globalization.CultureInfo.InvariantCulture)
    .WriteTo.File("logs/mcppoc-.log", formatProvider: System.Globalization.CultureInfo.InvariantCulture, rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog
builder.Host.UseSerilog();

// Check if auth is enabled (default: true)
var authEnabled = builder.Configuration.GetValue("Auth:Enabled", true);

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure OpenAPI (native)
builder.Services.AddOpenApi();

// Configure JWT Bearer authentication with Keycloak (only if auth enabled)
if (authEnabled)
{
    builder.Services.AddAuthentication(JwtBearerDefaults.AuthenticationScheme)
        .AddJwtBearer(options =>
        {
            var keycloakAuthority = builder.Configuration["Keycloak:Authority"];

            options.Authority = keycloakAuthority;
            options.Audience = builder.Configuration["Keycloak:Audience"];
            options.RequireHttpsMetadata = builder.Configuration.GetValue<bool>("Keycloak:RequireHttpsMetadata");

            options.TokenValidationParameters = new Microsoft.IdentityModel.Tokens.TokenValidationParameters
            {
                ValidateAudience = false,  // TODO: Configure Keycloak to add audience claim
                ValidateIssuer = true,
                ValidateLifetime = true,
                ValidateIssuerSigningKey = true
            };

            options.Events = new JwtBearerEvents
            {
                OnAuthenticationFailed = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    AppLog.AuthenticationFailed(logger, context.Exception);
                    return Task.CompletedTask;
                },
                OnTokenValidated = context =>
                {
                    var logger = context.HttpContext.RequestServices.GetRequiredService<ILogger<Program>>();
                    var userName = context.Principal?.Identity?.Name ?? "Unknown";
                    AppLog.TokenValidated(logger, userName);
                    return Task.CompletedTask;
                }
            };
        });

    builder.Services.AddAuthorization();
    builder.Services.AddMcpPocAuthorization();
}
else
{
    // No-op authorization when auth disabled
    builder.Services.AddAuthorization();
    Log.Warning("Authentication is DISABLED - all endpoints are accessible without auth");
}

builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<UserStore>();  // HACK: In-memory persistence
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IScopedRequestTracker, ScopedRequestTracker>();

// Configure MCP with authentication and authorization
builder.Services.AddZeroMcpExtensions(options =>
{
    options.RequireAuthentication = authEnabled;  // Require auth only if enabled
    options.UseAuthorization = authEnabled;       // SDK authorization filters enforce [Authorize] policies only if enabled
    options.ToolsListTimeToLive = TimeSpan.FromMinutes(5);  // tools/list cache hint (ttlMs + cacheScope)
    options.SessionMode = builder.Configuration.GetValue("Mcp:SessionMode", HttpServerSessionMode.Stateless);  // Stateless default
    options.McpEndpointPath = "/mcp";             // MCP endpoint path
    options.ToolAssembly = typeof(McpPoc.Api.Controllers.UsersController).Assembly;  // Explicit assembly for Docker
});

var app = builder.Build();

// Configure HTTP pipeline
// Note: OpenAPI/Scalar disabled in Docker due to .NET 10 preview bug
// Enable only for local development with ENABLE_OPENAPI=true
if (app.Environment.IsDevelopment() && builder.Configuration.GetValue("ENABLE_OPENAPI", false))
{
    app.MapOpenApi();

    // Scalar UI with OAuth2 configuration
    var keycloakAuthority = builder.Configuration["Keycloak:Authority"];
    app.MapScalarApiReference(options =>
    {
        options
            .WithTitle("MCP POC API")
            .WithDefaultHttpClient(ScalarTarget.CSharp, ScalarClient.HttpClient)
            .AddAuthorizationCodeFlow("keycloak", flow =>
            {
                flow.ClientId = "mcppoc-api";
                flow.AuthorizationUrl = $"{keycloakAuthority}/protocol/openid-connect/auth";
                flow.TokenUrl = $"{keycloakAuthority}/protocol/openid-connect/token";
            });
    });
}

app.UseHttpsRedirection();
app.UseZeroMcpMarking();  // Mark MCP requests BEFORE authentication
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();
app.MapZeroMcp();  // Uses configuration from AddZeroMcpExtensions

AppLog.BannerSeparator(app.Logger);
AppLog.BannerTitle(app.Logger);
AppLog.BannerHttpApi(app.Logger);
AppLog.BannerMcpEndpoint(app.Logger);
AppLog.BannerScalarUi(app.Logger);
AppLog.BannerSeparator(app.Logger);

app.Run();

// Make Program accessible for WebApplicationFactory
public partial class Program { }
