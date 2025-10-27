using McpPoc.Api.Extensions;
using McpPoc.Api.Services;
using Microsoft.AspNetCore.Authentication.JwtBearer;
using Microsoft.OpenApi.Models;
using Serilog;

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

// Configure Swagger with OAuth2
builder.Services.AddSwaggerGen(options =>
{
    options.SwaggerDoc("v1", new OpenApiInfo
    {
        Title = "MCP POC API",
        Version = "v1",
        Description = "API with MCP tools and Keycloak authentication"
    });

    // Add OAuth2 security definition
    var keycloakAuthority = builder.Configuration["Keycloak:Authority"];
    options.AddSecurityDefinition("oauth2", new OpenApiSecurityScheme
    {
        Type = SecuritySchemeType.OAuth2,
        Flows = new OpenApiOAuthFlows
        {
            Implicit = new OpenApiOAuthFlow
            {
                AuthorizationUrl = new Uri($"{keycloakAuthority}/protocol/openid-connect/auth"),
                TokenUrl = new Uri($"{keycloakAuthority}/protocol/openid-connect/token"),
                Scopes = new Dictionary<string, string>
                {
                    { "openid", "OpenID Connect" },
                    { "profile", "User profile" },
                    { "email", "User email" }
                }
            }
        }
    });

    options.AddSecurityRequirement(new OpenApiSecurityRequirement
    {
        {
            new OpenApiSecurityScheme
            {
                Reference = new OpenApiReference
                {
                    Type = ReferenceType.SecurityScheme,
                    Id = "oauth2"
                }
            },
            new[] { "openid", "profile", "email" }
        }
    });
});

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

// Add HttpContextAccessor for accessing HttpContext in filter pipeline
builder.Services.AddHttpContextAccessor();

// Register user service
builder.Services.AddSingleton<IUserService, UserService>();

// Register scoped request tracker for DI scoping tests
builder.Services.AddScoped<IScopedRequestTracker, ScopedRequestTracker>();

// ============================================
// TEST: Add MCP Server with HTTP transport
// ============================================
builder.Services
    .AddMcpServer()
    .WithHttpTransport()  // HTTP transport for full pipeline
    .WithToolsFromAssemblyUnwrappingActionResult();  // Custom extension that unwraps ActionResult<T>

var app = builder.Build();

// Configure HTTP pipeline
if (app.Environment.IsDevelopment())
{
    app.UseSwagger();
    app.UseSwaggerUI(options =>
    {
        options.SwaggerEndpoint("/swagger/v1/swagger.json", "MCP POC API v1");
        options.OAuthClientId("mcppoc-api");
        options.OAuthAppName("MCP POC API");
        options.OAuthUsePkce();
    });
}

app.UseHttpsRedirection();
app.UseAuthentication();
app.UseAuthorization();
app.MapControllers();

// ============================================
// TEST: Map MCP endpoint with authentication
// ============================================
app.MapMcp("/mcp").RequireAuthorization();  // Expose MCP at /mcp with auth required

app.Logger.LogInformation("===========================================");
app.Logger.LogInformation("TEST: MCP + Controller Integration");
app.Logger.LogInformation("HTTP API: http://127.0.0.1:5001/api/users");
app.Logger.LogInformation("MCP Endpoint: http://127.0.0.1:5001/mcp");
app.Logger.LogInformation("Swagger: http://127.0.0.1:5001/swagger");
app.Logger.LogInformation("===========================================");

app.Run();

// Make Program accessible for WebApplicationFactory
public partial class Program { }
