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
