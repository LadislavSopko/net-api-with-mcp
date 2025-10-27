namespace McpPoc.Api.Services;

/// <summary>
/// Service to test DI scoping behavior in MCP tool invocations.
/// Each instance gets a unique RequestId when created.
/// If scoping works correctly, each MCP tool call should get a different instance.
/// </summary>
public interface IScopedRequestTracker
{
    Guid RequestId { get; }
    DateTime CreatedAt { get; }
}

public class ScopedRequestTracker : IScopedRequestTracker
{
    public Guid RequestId { get; } = Guid.NewGuid();
    public DateTime CreatedAt { get; } = DateTime.UtcNow;
}
