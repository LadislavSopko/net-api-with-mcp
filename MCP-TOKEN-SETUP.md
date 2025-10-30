# MCP Token Authentication Setup

## Quick Start - Login Script

The easiest way to login and update your MCP configuration:

```bash
./do-login-poc.sh <username> <password>
```

**Examples:**
```bash
# Login as admin (full permissions)
./do-login-poc.sh admin admin123

# Login as regular user (limited permissions)
./do-login-poc.sh user user123

# Login with email-based users
./do-login-poc.sh alice@example.com alice123
./do-login-poc.sh bob@example.com bob123
./do-login-poc.sh carol@example.com carol123
```

The script will:
1. Authenticate with Keycloak
2. Get a JWT access token
3. Update `.mcp.json` with the new token
4. Create a backup (`.mcp.json.bak`)

**After running the script, restart Claude Code / Claude Desktop to apply changes.**

## Available Users

| Username | Password | Role | User ID | Permissions |
|----------|----------|------|---------|-------------|
| viewer | viewer123 | Viewer | 102 | Read only |
| alice@example.com | alice123 | Member | 1 | Read + Create |
| bob@example.com | bob123 | Manager | 2 | Read + Create + Update |
| carol@example.com | carol123 | Admin | 3 | Everything |
| admin | admin123 | Admin | 100 | Everything |
| user | user123 | Member | 101 | Read + Create |

See [USERS-AND-PERMISSIONS.md](USERS-AND-PERMISSIONS.md) for detailed role descriptions.

## Configuration Structure

The `.mcp.json` file stores the token directly in the Authorization header:

```json
{
  "mcpServers": {
    "poc": {
      "type": "http",
      "url": "http://localhost:5001/mcp",
      "headers": {
        "Authorization": "Bearer eyJhbGciOiJSUzI1NiIsInR5cCI6..."
      }
    }
  }
}
```

**Note**: For HTTP-type MCP servers, tokens must be in headers, not environment variables.

## Manual Token Management

### Method 1: Get Token Only

If you only want to get a token without updating `.mcp.json`:

```bash
# Get token and print to stdout
TOKEN=$(bash get-token.sh admin admin123)
echo $TOKEN
```

### Method 2: Manual Update

1. Get token:
```bash
bash get-token.sh admin admin123
```

2. Copy the token output

3. Edit `.mcp.json` and replace the token in `mcpServers.poc.headers.Authorization`

4. Restart Claude Code / Claude Desktop

## Testing Your Configuration

Test if authentication is working:

```bash
# Read current token from .mcp.json
TOKEN=$(python3 -c "import json; print(json.load(open('.mcp.json'))['mcpServers']['poc']['headers']['Authorization'].split(' ')[1])")

# Test MCP endpoint
curl -X POST http://127.0.0.1:5001/mcp \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/list",
    "id": 1
  }'
```

Expected response:
```json
{
  "jsonrpc": "2.0",
  "id": 1,
  "result": {
    "tools": [
      {"name": "get_by_id", "description": "Get user by ID", ...},
      {"name": "get_all", "description": "Get all users", ...},
      {"name": "create", "description": "Create new user", ...}
    ]
  }
}
```

## Security Notes

⚠️ **IMPORTANT**:

1. **Token Expiration**: Tokens expire after 1 hour (3600 seconds)
   - Re-run `./do-login-poc.sh` when tokens expire
   - You'll get 401 Unauthorized when token expires

2. **Git Security**:
   - `.mcp.json` contains sensitive tokens
   - Consider using `.mcp.json.example` template instead
   - Add `.mcp.json` to `.gitignore` if sharing code
   - The repo includes `.mcp.json.example` without tokens

3. **Production**:
   - This setup uses password grant (not recommended for production)
   - For production, use proper OAuth 2.1 flows with PKCE
   - Consider using shorter-lived tokens with refresh tokens

4. **Backup Files**:
   - `.mcp.json.bak` is created automatically
   - Backups are git-ignored
   - Keep backups secure (they contain valid tokens)

## Troubleshooting

### Error: "unauthorized_client"
- **Cause**: Client configuration mismatch
- **Solution**: Verify Keycloak client is set to public with PKCE enabled
- **Check**: `docker/keycloak/mcppoc-realm.json` should have `"publicClient": true`

### Error: 401 Unauthorized
- **Cause**: Token expired or invalid
- **Solution**: Re-run `./do-login-poc.sh <username> <password>`

### Error: Login failed / "invalid_grant"
- **Cause**: Wrong username or password
- **Solution**: Check credentials in [USERS-AND-PERMISSIONS.md](USERS-AND-PERMISSIONS.md)

### Error: Connection refused
- **Cause**: Keycloak or API not running
- **Solution**:
  ```bash
  # Check Keycloak (port 8080)
  curl http://127.0.0.1:8080/health

  # Check API (port 5001)
  curl http://127.0.0.1:5001/health

  # Start services if needed
  cd docker && docker-compose up -d
  ```

### Error: "jq: command not found" (old set-mcp-token.sh)
- **Note**: `do-login-poc.sh` works without jq (uses Python fallback)
- **Optional**: Install jq for better JSON handling:
  ```bash
  # Ubuntu/Debian/WSL
  sudo apt-get install jq

  # MacOS
  brew install jq
  ```

## Related Documentation

- [USERS-AND-PERMISSIONS.md](USERS-AND-PERMISSIONS.md) - Complete user and role documentation
- [.mcp.json.example](.mcp.json.example) - Template configuration without tokens
- [docs/MCP-AUTHORIZATION-COMPLETE-GUIDE.md](docs/MCP-AUTHORIZATION-COMPLETE-GUIDE.md) - Authorization system details
