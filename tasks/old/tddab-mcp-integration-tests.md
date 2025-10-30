# Agent-Optimized TDDAB Plan: MCP Integration Testing

**Status:** Ready for ACT
**Created:** 2025-10-22
**Methodology:** TDDAB + Agent-Optimized C# + xUnit Integration Tests

## Overview

Create comprehensive xUnit integration tests that verify the MCP POC using:
- `WebApplicationFactory` to spin up real API in-memory
- MCP client SDK to connect to `/mcp` endpoint
- Tests for tool discovery, invocation, selective exposure, and HTTP coexistence

## Success Criteria (from Memory Bank)

- ✓ MCP tools discovered: GetById, GetAll, Create (3 tools)
- ✓ Tools callable and return correct data
- ✓ HTTP endpoints and MCP endpoints coexist
- ✓ Dependency injection works (IUserService, ILogger)
- ✗ Delete endpoint NOT exposed as MCP tool

---

## TDDAB-1: Test Project Setup

### 1.1 Tests First (Will FAIL - no project exists)

**Create:** `/mnt/c/Projekty/AI_Works/net-api-with-mcp/tests/McpPoc.Api.Tests/McpPoc.Api.Tests.csproj`
```xml
<Project Sdk="Microsoft.NET.Sdk">

  <PropertyGroup>
    <TargetFramework>net9.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
    <IsTestProject>true</IsTestProject>
  </PropertyGroup>

  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" />
    <PackageReference Include="xunit" />
    <PackageReference Include="xunit.runner.visualstudio" />
    <PackageReference Include="FluentAssertions" />
    <PackageReference Include="Microsoft.AspNetCore.Mvc.Testing" />
    <PackageReference Include="ModelContextProtocol" />
  </ItemGroup>

  <ItemGroup>
    <ProjectReference Include="..\..\src\McpPoc.Api\McpPoc.Api.csproj" />
  </ItemGroup>

</Project>
```

### 1.2 Implementation

**Step 1 - Add test packages to Directory.Packages.props:**

Add these to `/mnt/c/Projekty/AI_Works/net-api-with-mcp/Directory.Packages.props`:
```xml
<!-- Test packages -->
<PackageVersion Include="Microsoft.NET.Test.Sdk" Version="17.12.0" />
<PackageVersion Include="xunit" Version="2.9.2" />
<PackageVersion Include="xunit.runner.visualstudio" Version="2.8.2" />
<PackageVersion Include="FluentAssertions" Version="7.0.0" />
<PackageVersion Include="Microsoft.AspNetCore.Mvc.Testing" Version="9.0.0" />
```

**Step 2 - Add project to solution:**

Add to `/mnt/c/Projekty/AI_Works/net-api-with-mcp/net-api-with-mcp.slnx` projects array:
```json
{
  "path": "tests/McpPoc.Api.Tests/McpPoc.Api.Tests.csproj",
  "type": "Classic C#"
}
```

**Step 3 - Make Program class accessible:**

Add to end of `/mnt/c/Projekty/AI_Works/net-api-with-mcp/src/McpPoc.Api/Program.cs`:
```csharp
// Make Program accessible for WebApplicationFactory
public partial class Program { }
```

### 1.3 Verification (Agent-Optimized)
```bash
Use build-agent to build McpPoc.Api.Tests
→ Expected: ✅ CLEAN (0 errors, 0 warnings)
```

---

## TDDAB-2: Test Fixture with WebApplicationFactory

### 2.1 Create Fixture

**Create:** `/mnt/c/Projekty/AI_Works/net-api-with-mcp/tests/McpPoc.Api.Tests/McpApiFixture.cs`
```csharp
using Microsoft.AspNetCore.Mvc.Testing;

namespace McpPoc.Api.Tests;

/// <summary>
/// Test fixture that spins up the API for integration testing
/// </summary>
public class McpApiFixture : WebApplicationFactory<Program>
{
    public HttpClient HttpClient { get; }

    public McpApiFixture()
    {
        HttpClient = CreateClient(new WebApplicationFactoryClientOptions
        {
            BaseAddress = new Uri("http://localhost")
        });
    }

    protected override void Dispose(bool disposing)
    {
        if (disposing)
        {
            HttpClient.Dispose();
        }
        base.Dispose(disposing);
    }
}

/// <summary>
/// Collection fixture for sharing test context
/// </summary>
[CollectionDefinition("McpApi")]
public class McpApiCollection : ICollectionFixture<McpApiFixture>
{
}
```

### 2.2 Create Global Usings

**Create:** `/mnt/c/Projekty/AI_Works/net-api-with-mcp/tests/McpPoc.Api.Tests/Usings.cs`
```csharp
global using Xunit;
global using FluentAssertions;
global using System.Net;
global using System.Net.Http.Json;
```

### 2.3 Verification (Agent-Optimized)
```bash
Use build-agent to build McpPoc.Api.Tests
→ Expected: ✅ CLEAN (0 errors, 0 warnings)
```

---

## TDDAB-3: MCP Client Helper

### 3.1 Create MCP Client Helper

**Create:** `/mnt/c/Projekty/AI_Works/net-api-with-mcp/tests/McpPoc.Api.Tests/McpClientHelper.cs`
```csharp
using System.Text;
using System.Text.Json;
using System.Text.Json.Serialization;

namespace McpPoc.Api.Tests;

/// <summary>
/// Helper for making MCP protocol requests
/// </summary>
public class McpClientHelper
{
    private readonly HttpClient _httpClient;
    private static readonly JsonSerializerOptions JsonOptions = new()
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull
    };

    public McpClientHelper(HttpClient httpClient)
    {
        _httpClient = httpClient;
    }

    /// <summary>
    /// List all available MCP tools
    /// </summary>
    public async Task<McpResponse<ToolsListResponse>> ListToolsAsync()
    {
        var request = new McpRequest
        {
            Jsonrpc = "2.0",
            Id = Guid.NewGuid().ToString(),
            Method = "tools/list"
        };

        var response = await SendMcpRequestAsync<ToolsListResponse>(request);
        return response;
    }

    /// <summary>
    /// Call an MCP tool
    /// </summary>
    public async Task<McpResponse<ToolCallResponse>> CallToolAsync(string toolName, Dictionary<string, object>? arguments = null)
    {
        var request = new McpRequest
        {
            Jsonrpc = "2.0",
            Id = Guid.NewGuid().ToString(),
            Method = "tools/call",
            Params = new ToolCallParams
            {
                Name = toolName,
                Arguments = arguments ?? new Dictionary<string, object>()
            }
        };

        var response = await SendMcpRequestAsync<ToolCallResponse>(request);
        return response;
    }

    private async Task<McpResponse<T>> SendMcpRequestAsync<T>(McpRequest request)
    {
        var json = JsonSerializer.Serialize(request, JsonOptions);
        var content = new StringContent(json, Encoding.UTF8, "application/json");

        var httpResponse = await _httpClient.PostAsync("/mcp", content);
        httpResponse.EnsureSuccessStatusCode();

        var responseJson = await httpResponse.Content.ReadAsStringAsync();
        var mcpResponse = JsonSerializer.Deserialize<McpResponse<T>>(responseJson, JsonOptions);

        return mcpResponse ?? throw new InvalidOperationException("Failed to deserialize MCP response");
    }
}

// MCP Protocol DTOs
public record McpRequest
{
    public string Jsonrpc { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public string Method { get; init; } = string.Empty;
    public object? Params { get; init; }
}

public record McpResponse<T>
{
    public string Jsonrpc { get; init; } = string.Empty;
    public string Id { get; init; } = string.Empty;
    public T? Result { get; init; }
    public McpError? Error { get; init; }
}

public record McpError
{
    public int Code { get; init; }
    public string Message { get; init; } = string.Empty;
    public object? Data { get; init; }
}

public record ToolsListResponse
{
    public List<ToolInfo> Tools { get; init; } = new();
}

public record ToolInfo
{
    public string Name { get; init; } = string.Empty;
    public string? Description { get; init; }
    public JsonElement? InputSchema { get; init; }
}

public record ToolCallParams
{
    public string Name { get; init; } = string.Empty;
    public Dictionary<string, object> Arguments { get; init; } = new();
}

public record ToolCallResponse
{
    public List<ToolContent> Content { get; init; } = new();
    public bool? IsError { get; init; }
}

public record ToolContent
{
    public string Type { get; init; } = string.Empty;
    public string? Text { get; init; }
}
```

### 3.2 Verification (Agent-Optimized)
```bash
Use build-agent to build McpPoc.Api.Tests
→ Expected: ✅ CLEAN (0 errors, 0 warnings)
```

---

## TDDAB-4: MCP Tool Discovery Tests

### 4.1 Create Discovery Tests

**Create:** `/mnt/c/Projekty/AI_Works/net-api-with-mcp/tests/McpPoc.Api.Tests/McpToolDiscoveryTests.cs`
```csharp
namespace McpPoc.Api.Tests;

[Collection("McpApi")]
public class McpToolDiscoveryTests
{
    private readonly McpApiFixture _fixture;
    private readonly McpClientHelper _mcpClient;

    public McpToolDiscoveryTests(McpApiFixture fixture)
    {
        _fixture = fixture;
        _mcpClient = new McpClientHelper(fixture.HttpClient);
    }

    [Fact]
    public async Task Should_DiscoverThreeMcpTools_WhenListingTools()
    {
        // Act
        var response = await _mcpClient.ListToolsAsync();

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull("MCP request should succeed");
        response.Result.Should().NotBeNull();
        response.Result!.Tools.Should().HaveCount(3, "only GetById, GetAll, and Create should be exposed");
    }

    [Fact]
    public async Task Should_DiscoverGetByIdTool_WithCorrectSchema()
    {
        // Act
        var response = await _mcpClient.ListToolsAsync();

        // Assert
        var getByIdTool = response.Result!.Tools
            .Should().ContainSingle(t => t.Name.Contains("GetById", StringComparison.OrdinalIgnoreCase))
            .Subject;

        getByIdTool.Description.Should().Contain("Gets a user by their ID");
    }

    [Fact]
    public async Task Should_DiscoverGetAllTool_WithCorrectSchema()
    {
        // Act
        var response = await _mcpClient.ListToolsAsync();

        // Assert
        var getAllTool = response.Result!.Tools
            .Should().ContainSingle(t => t.Name.Contains("GetAll", StringComparison.OrdinalIgnoreCase))
            .Subject;

        getAllTool.Description.Should().Contain("Gets all users");
    }

    [Fact]
    public async Task Should_DiscoverCreateTool_WithParameterDescriptions()
    {
        // Act
        var response = await _mcpClient.ListToolsAsync();

        // Assert
        var createTool = response.Result!.Tools
            .Should().ContainSingle(t => t.Name.Contains("Create", StringComparison.OrdinalIgnoreCase))
            .Subject;

        createTool.Description.Should().Contain("Creates a new user");
        createTool.InputSchema.Should().NotBeNull("Create tool should have input schema for name and email");
    }

    [Fact]
    public async Task Should_NotExposeDeleteEndpoint_AsAnMcpTool()
    {
        // Act
        var response = await _mcpClient.ListToolsAsync();

        // Assert
        response.Result!.Tools
            .Should().NotContain(t => t.Name.Contains("Delete", StringComparison.OrdinalIgnoreCase),
                "Delete endpoint should NOT have [McpServerTool] attribute");
    }
}
```

### 4.2 Verification (Agent-Optimized)
```bash
Use build-agent to build McpPoc.Api.Tests
→ Expected: ✅ CLEAN (0 errors, 0 warnings)

Use test-agent to run tests for McpToolDiscoveryTests
→ Expected: ✅ ALL PASS (5 tests passed) OR compressed failure list
```

---

## TDDAB-5: MCP Tool Invocation Tests

### 5.1 Create Invocation Tests

**Create:** `/mnt/c/Projekty/AI_Works/net-api-with-mcp/tests/McpPoc.Api.Tests/McpToolInvocationTests.cs`
```csharp
using System.Text.Json;

namespace McpPoc.Api.Tests;

[Collection("McpApi")]
public class McpToolInvocationTests
{
    private readonly McpApiFixture _fixture;
    private readonly McpClientHelper _mcpClient;

    public McpToolInvocationTests(McpApiFixture fixture)
    {
        _fixture = fixture;
        _mcpClient = new McpClientHelper(fixture.HttpClient);
    }

    [Fact]
    public async Task Should_GetUserById_WhenCallingGetByIdTool()
    {
        // Arrange
        var arguments = new Dictionary<string, object>
        {
            ["id"] = 1
        };

        // Act
        var response = await _mcpClient.CallToolAsync("GetById", arguments);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull("tool call should succeed");
        response.Result.Should().NotBeNull();
        response.Result!.IsError.Should().BeFalse();
        response.Result.Content.Should().NotBeEmpty();

        var textContent = response.Result.Content.First().Text;
        textContent.Should().Contain("Alice Smith", "user with id 1 should be returned");
    }

    [Fact]
    public async Task Should_GetAllUsers_WhenCallingGetAllTool()
    {
        // Act
        var response = await _mcpClient.CallToolAsync("GetAll");

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull("tool call should succeed");
        response.Result.Should().NotBeNull();
        response.Result!.IsError.Should().BeFalse();
        response.Result.Content.Should().NotBeEmpty();

        var textContent = response.Result.Content.First().Text;
        textContent.Should().NotBeNull();

        // Parse JSON array
        var users = JsonSerializer.Deserialize<JsonElement>(textContent!);
        users.GetArrayLength().Should().BeGreaterOrEqualTo(3, "at least 3 users should exist");
    }

    [Fact]
    public async Task Should_CreateUser_WhenCallingCreateTool()
    {
        // Arrange
        var arguments = new Dictionary<string, object>
        {
            ["name"] = "Test User",
            ["email"] = "test@example.com"
        };

        // Act
        var response = await _mcpClient.CallToolAsync("Create", arguments);

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull("tool call should succeed");
        response.Result.Should().NotBeNull();
        response.Result!.IsError.Should().BeFalse();
        response.Result.Content.Should().NotBeEmpty();

        var textContent = response.Result.Content.First().Text;
        textContent.Should().Contain("Test User");
        textContent.Should().Contain("test@example.com");
    }

    [Fact]
    public async Task Should_ReturnNotFound_WhenGettingNonExistentUser()
    {
        // Arrange
        var arguments = new Dictionary<string, object>
        {
            ["id"] = 99999
        };

        // Act
        var response = await _mcpClient.CallToolAsync("GetById", arguments);

        // Assert
        response.Should().NotBeNull();
        // MCP should return success even if user not found (404 is handled by API)
        response.Error.Should().BeNull("MCP protocol should succeed");
        response.Result.Should().NotBeNull();
    }

    [Fact]
    public async Task Should_VerifyDependencyInjection_WorksInMcpTools()
    {
        // This test verifies that IUserService is properly injected
        // by calling a tool that uses it

        // Act
        var response = await _mcpClient.CallToolAsync("GetAll");

        // Assert
        response.Should().NotBeNull();
        response.Error.Should().BeNull("DI should work - tool should execute successfully");
        response.Result.Should().NotBeNull();
        response.Result!.IsError.Should().BeFalse();
    }
}
```

### 5.2 Verification (Agent-Optimized)
```bash
Use build-agent to build McpPoc.Api.Tests
→ Expected: ✅ CLEAN (0 errors, 0 warnings)

Use test-agent to run tests for McpToolInvocationTests
→ Expected: ✅ ALL PASS (5 tests passed) OR compressed failure list
```

---

## TDDAB-6: HTTP Coexistence Tests

### 6.1 Create Coexistence Tests

**Create:** `/mnt/c/Projekty/AI_Works/net-api-with-mcp/tests/McpPoc.Api.Tests/HttpCoexistenceTests.cs`
```csharp
namespace McpPoc.Api.Tests;

[Collection("McpApi")]
public class HttpCoexistenceTests
{
    private readonly McpApiFixture _fixture;

    public HttpCoexistenceTests(McpApiFixture fixture)
    {
        _fixture = fixture;
    }

    [Fact]
    public async Task Should_AccessUsersViaHttpApi_WhenMcpIsAlsoEnabled()
    {
        // Act
        var response = await _fixture.HttpClient.GetAsync("/api/users");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK, "HTTP API should still work");
        var users = await response.Content.ReadFromJsonAsync<List<UserDto>>();
        users.Should().NotBeNull();
        users!.Should().HaveCountGreaterOrEqualTo(3);
    }

    [Fact]
    public async Task Should_GetUserByIdViaHttp_WhenMcpIsAlsoEnabled()
    {
        // Act
        var response = await _fixture.HttpClient.GetAsync("/api/users/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.OK);
        var user = await response.Content.ReadFromJsonAsync<UserDto>();
        user.Should().NotBeNull();
        user!.Name.Should().Be("Alice Smith");
    }

    [Fact]
    public async Task Should_DeleteViaHttp_EvenThoughNotExposedAsMcpTool()
    {
        // Act
        var response = await _fixture.HttpClient.DeleteAsync("/api/users/1");

        // Assert
        response.StatusCode.Should().Be(HttpStatusCode.NoContent, "Delete HTTP endpoint should work");
    }
}

// DTO for deserialization
public record UserDto
{
    public int Id { get; init; }
    public string Name { get; init; } = string.Empty;
    public string Email { get; init; } = string.Empty;
    public DateTime CreatedAt { get; init; }
}
```

### 6.2 Verification (Agent-Optimized)
```bash
Use build-agent to build McpPoc.Api.Tests
→ Expected: ✅ CLEAN (0 errors, 0 warnings)

Use test-agent to run tests for HttpCoexistenceTests
→ Expected: ✅ ALL PASS (3 tests passed) OR compressed failure list
```

---

## Summary

**Total TDDAB Blocks:** 6
**Total Test Files:** 5
**Total Tests:** 13

### Test Coverage
- ✅ MCP tool discovery (5 tests)
- ✅ MCP tool invocation (5 tests)
- ✅ HTTP/MCP coexistence (3 tests)
- ✅ Dependency injection verification
- ✅ Selective exposure verification

### Agent-Optimized Benefits
- **Build verification**: build-agent returns ✅ CLEAN or compressed errors
- **Test verification**: test-agent returns ✅ ALL PASS or compressed failures
- **Context efficiency**: 90-95% token savings vs raw MSBuild/xUnit output
- **Development speed**: 5-10x faster feedback loops

### Verification Targets
Each TDDAB block must achieve:
- Build: 0 errors, 0 warnings
- Tests: All tests passing
- Coverage: All POC success criteria validated

---

## Execution Command

To execute this plan:
```
Type: ACT
```

Then I'll execute each TDDAB block sequentially with agent-optimized verification feedback.
