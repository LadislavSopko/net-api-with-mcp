# McpPoc.Api Docker Container

MCP (Model Context Protocol) server with HTTP API coexistence. Portable container for testing MCP integrations in any project.

## Container Image

```
mcppoc-api:latest
```

Build from source:
```bash
cd /path/to/net-api-with-mcp
docker build -t mcppoc-api -f docker/Dockerfile .
```

## Quick Start

```bash
# Run without authentication (testing mode)
docker run -d -p 5001:80 -e Auth__Enabled=false mcppoc-api

# Run with Keycloak authentication
docker run -d -p 5001:80 \
  -e Auth__Enabled=true \
  -e Keycloak__Authority=http://keycloak:8080/realms/your-realm \
  mcppoc-api
```

## Endpoints

| Endpoint | Protocol | Description |
|----------|----------|-------------|
| `/mcp` | MCP (JSON-RPC) | MCP server endpoint |
| `/api/users` | HTTP REST | User management API |
| `/api/users/echo-headers` | HTTP/MCP | Returns all request headers |
| `/api/users/tool-diagnostics` | HTTP | Shows registered MCP tools |
| `/scalar` | HTTP | OpenAPI documentation (dev only) |

## Environment Variables

| Variable | Default | Description |
|----------|---------|-------------|
| `Auth__Enabled` | `true` | Enable/disable authentication |
| `Keycloak__Authority` | `http://127.0.0.1:8080/realms/mcppoc-realm` | Keycloak realm URL |
| `Keycloak__Audience` | `account` | JWT audience |
| `Keycloak__RequireHttpsMetadata` | `false` | Require HTTPS for metadata |
| `ASPNETCORE_ENVIRONMENT` | `Production` | Environment (Development/Production) |

## MCP Tools Available

When `Auth__Enabled=false`:

| Tool Name | Description |
|-----------|-------------|
| `echo_headers` | Returns all request headers (for testing) |
| `get_all` | Get all users |
| `UserGetById` | Get user by ID |
| `create` | Create new user |
| `update` | Update user |
| `promote_to_manager` | Promote user to manager |
| `get_scope_id` | DI scope test |
| `get_mcp_context` | MCP context info |
| `get_public_info` | Public information |

## Docker Compose Examples

### Standalone (No Auth)

```yaml
version: '3.8'

services:
  mcp-api:
    image: mcppoc-api:latest
    container_name: mcp-api
    ports:
      - "5001:80"
    environment:
      Auth__Enabled: "false"
      ASPNETCORE_ENVIRONMENT: Development
```

### With Keycloak

```yaml
version: '3.8'

services:
  mcp-api:
    image: mcppoc-api:latest
    container_name: mcp-api
    ports:
      - "5001:80"
    environment:
      Auth__Enabled: "true"
      Keycloak__Authority: "http://keycloak:8080/realms/your-realm"
      Keycloak__Audience: "account"
      Keycloak__RequireHttpsMetadata: "false"
    depends_on:
      keycloak:
        condition: service_healthy
    networks:
      - app-network

  keycloak:
    image: quay.io/keycloak/keycloak:25.0.2
    container_name: keycloak
    command: start-dev
    environment:
      KEYCLOAK_ADMIN: admin
      KEYCLOAK_ADMIN_PASSWORD: admin
    ports:
      - "8080:8080"
    networks:
      - app-network
    healthcheck:
      test: ["CMD", "curl", "-f", "http://localhost:8080/health/ready"]
      interval: 10s
      timeout: 5s
      retries: 10
      start_period: 60s

networks:
  app-network:
    driver: bridge
```

### Integration with Your Application

```yaml
version: '3.8'

services:
  your-app:
    image: your-app:latest
    environment:
      MCP_SERVER_URL: "http://mcp-api:80/mcp"
    depends_on:
      - mcp-api
    networks:
      - app-network

  mcp-api:
    image: mcppoc-api:latest
    environment:
      Auth__Enabled: "false"
    networks:
      - app-network
    # No ports exposed - only accessible within network

networks:
  app-network:
    driver: bridge
```

### Full Stack (from registry)

```yaml
version: '3.8'

services:
  mcp-api:
    image: your-registry.com/mcppoc-api:latest
    ports:
      - "${MCP_PORT:-5001}:80"
    environment:
      Auth__Enabled: "${AUTH_ENABLED:-false}"
    restart: unless-stopped
```

## Testing

### List MCP Tools

```bash
curl -X POST http://localhost:5001/mcp \
  -H "Content-Type: application/json" \
  -d '{"jsonrpc":"2.0","id":1,"method":"tools/list"}'
```

### Call echo_headers Tool (MCP)

```bash
curl -X POST http://localhost:5001/mcp \
  -H "Content-Type: application/json" \
  -H "X-Custom-Header: my-value" \
  -H "Authorization: Bearer test-token" \
  -d '{
    "jsonrpc":"2.0",
    "id":2,
    "method":"tools/call",
    "params":{
      "name":"echo_headers",
      "arguments":{}
    }
  }'
```

Response:
```json
{
  "result": {
    "content": [{
      "type": "text",
      "text": "{\"is_mcp_call\":true,\"headers\":{\"X-Custom-Header\":\"my-value\",\"Authorization\":\"Bearer test-token\",\"x-mcp-call\":\"true\",...}}"
    }]
  }
}
```

### HTTP echo-headers Endpoint

```bash
curl http://localhost:5001/api/users/echo-headers \
  -H "X-Custom-Header: my-value" \
  -H "Authorization: Bearer token123"
```

### Check Tool Diagnostics

```bash
curl http://localhost:5001/api/users/tool-diagnostics | jq .
```

## MCP Client Configuration

### Claude Desktop / Cursor

```json
{
  "mcpServers": {
    "mcppoc": {
      "url": "http://localhost:5001/mcp"
    }
  }
}
```

### Programmatic (C#)

```csharp
var client = await McpClientFactory.CreateAsync(
    new SseClientTransport("http://localhost:5001/mcp"));

var tools = await client.ListToolsAsync();
var result = await client.CallToolAsync("echo_headers", new {});
```

## Port Mapping

Container listens on port **80** internally. Map to any external port:

```bash
docker run -p 9999:80 mcppoc-api    # Access via localhost:9999
docker run -p 5001:80 mcppoc-api    # Access via localhost:5001
docker run -p 80:80 mcppoc-api      # Access via localhost:80
```

## Health Check

```bash
curl http://localhost:5001/api/users/public
```

Returns `200 OK` with server info if healthy.

## Logs

```bash
docker logs -f mcp-api
```

## Troubleshooting

### Tools list empty

Ensure `Auth__Enabled=false` or configure valid Keycloak. When auth enabled but Keycloak unavailable, tools are filtered out.

### Connection refused to Keycloak

Use container name (`keycloak`) not `localhost` when both services in same Docker network.

### Headers not appearing in MCP call

Headers are forwarded through MCP. Use `echo_headers` tool to verify which headers reach the server.

---

# Development Setup (Full Stack)

For local development with all infrastructure:

## Services

| Service | Image | Port | Description |
|---------|-------|------|-------------|
| PostgreSQL | postgres:16-alpine | 5432 | Database |
| Keycloak | keycloak:25.0.2 | 8080 | Auth server |
| API | mcppoc-api | 5001 | MCP + HTTP API |

## Quick Start (Dev)

```bash
cd docker
cp .env.example .env
docker-compose up -d
```

## Development Workflow

1. **Local Development** (API outside Docker):
   ```bash
   docker-compose up -d postgres keycloak
   dotnet run --project src/McpPoc.Api
   ```

2. **Full Docker**:
   ```bash
   docker-compose up -d
   ```

## Keycloak Test Users

| User | Password | Role |
|------|----------|------|
| admin@mcppoc.com | admin123 | admin |
| user@mcppoc.com | user123 | user |

## Architecture

```
┌─────────────────┐
│   MCP Client    │
└────────┬────────┘
         │
         ▼
┌─────────────────┐      ┌─────────────────┐
│   API (5001)    │◄────►│ Keycloak (8080) │
│  - HTTP/REST    │      │  - Auth/OIDC    │
│  - MCP Tools    │      └─────────┬───────┘
└────────┬────────┘                │
         │                         │
         ▼                         ▼
┌─────────────────────────────────────┐
│         PostgreSQL (5432)           │
└─────────────────────────────────────┘
```
