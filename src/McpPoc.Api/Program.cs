using McpPoc.Api.Authorization;
using McpPoc.Api.Infrastructure;
using McpPoc.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Scalar.AspNetCore;
using Serilog;
using Zero.Mcp.Extensions;

// Configure Serilog for file logging
Log.Logger = new LoggerConfiguration()
    .MinimumLevel.Verbose()
    .WriteTo.File("logs/mcppoc-.log", rollingInterval: RollingInterval.Day)
    .CreateLogger();

var builder = WebApplication.CreateBuilder(args);

// Use Serilog
builder.Host.UseSerilog();

// Add services
builder.Services.AddControllers();
builder.Services.AddEndpointsApiExplorer();

// Configure OpenAPI (native)
builder.Services.AddOpenApi();

// Configure JWT Bearer authentication with Keycloak
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
                context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>()
                    .LogError(context.Exception, "Authentication failed");
                return Task.CompletedTask;
            },
            OnTokenValidated = context =>
            {
                context.HttpContext.RequestServices
                    .GetRequiredService<ILogger<Program>>()
                    .LogInformation("Token validated for user: {User}",
                        context.Principal?.Identity?.Name ?? "Unknown");
                return Task.CompletedTask;
            }
        };
    });

builder.Services.AddAuthorization();
builder.Services.AddMcpPocAuthorization();
builder.Services.AddHttpContextAccessor();
builder.Services.AddSingleton<UserStore>();  // HACK: In-memory persistence
builder.Services.AddScoped<IUserService, UserService>();
builder.Services.AddScoped<IScopedRequestTracker, ScopedRequestTracker>();

builder.Services.AddScoped<IAuthForMcpSupplier, KeycloakAuthSupplier>();
builder.Services.AddScoped<IUserRoleResolver, UserRoleResolver>();

// Configure MCP with authentication and authorization
builder.Services.AddZeroMcpExtensions(options =>
{
    options.RequireAuthentication = true;  // Require auth for MCP endpoint
    options.UseAuthorization = true;       // Use [Authorize] policies
    options.McpEndpointPath = "/mcp";      // MCP endpoint path
});

var app = builder.Build();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
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

app.Logger.LogInformation("===========================================");
app.Logger.LogInformation("MCP POC API");
app.Logger.LogInformation("HTTP API: http://127.0.0.1:5001/api/users");
app.Logger.LogInformation("MCP Endpoint: http://127.0.0.1:5001/mcp");
app.Logger.LogInformation("Scalar UI: http://127.0.0.1:5001/scalar");
app.Logger.LogInformation("===========================================");

app.Run();

// Make Program accessible for WebApplicationFactory
public partial class Program { }
