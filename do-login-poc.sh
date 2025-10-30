#!/bin/bash

# Login to POC and update .mcp.json with new token
# Usage: ./do-login-poc.sh <username> <password>
# Example: ./do-login-poc.sh admin admin123

set -e

if [ $# -ne 2 ]; then
    echo "Error: Username and password required"
    echo "Usage: ./do-login-poc.sh <username> <password>"
    echo ""
    echo "Available users:"
    echo "  viewer / viewer123        (Viewer - ID: 102 - Read only)"
    echo "  user / user123            (Member - ID: 101 - Read + Create)"
    echo "  alice@example.com / alice123  (Member - ID: 1 - Read + Create)"
    echo "  bob@example.com / bob123      (Manager - ID: 2 - Read + Create + Update)"
    echo "  carol@example.com / carol123  (Admin - ID: 3 - Everything)"
    echo "  admin / admin123          (Admin - ID: 100 - Everything)"
    exit 1
fi

USERNAME="$1"
PASSWORD="$2"

echo "Logging in as: $USERNAME"

# Get token from Keycloak
TOKEN_RESPONSE=$(curl -s -X POST \
  http://127.0.0.1:8080/realms/mcppoc-realm/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=mcppoc-api" \
  -d "grant_type=password" \
  -d "username=$USERNAME" \
  -d "password=$PASSWORD")

# Check if login was successful
if echo "$TOKEN_RESPONSE" | grep -q "error"; then
    echo "Error: Login failed"
    echo "$TOKEN_RESPONSE" | python3 -m json.tool
    exit 1
fi

# Extract access token
TOKEN=$(echo "$TOKEN_RESPONSE" | python3 -c "import sys, json; print(json.load(sys.stdin)['access_token'])")

if [ -z "$TOKEN" ]; then
    echo "Error: Failed to extract access token"
    exit 1
fi

echo "Token obtained successfully"

# Update .mcp.json with new token
if [ ! -f .mcp.json ]; then
    echo "Error: .mcp.json not found in current directory"
    exit 1
fi

# Create backup
cp .mcp.json .mcp.json.bak

# Update token using jq
if command -v jq >/dev/null 2>&1; then
    # Use jq if available
    jq --arg token "Bearer $TOKEN" '.mcpServers.poc.headers.Authorization = $token' .mcp.json > .mcp.json.tmp
    mv .mcp.json.tmp .mcp.json
else
    # Fallback to python if jq not available
    python3 << EOF
import json

with open('.mcp.json', 'r') as f:
    config = json.load(f)

config['mcpServers']['poc']['headers']['Authorization'] = 'Bearer $TOKEN'

with open('.mcp.json', 'w') as f:
    json.dump(config, f, indent=2)
    f.write('\n')
EOF
fi

echo "✓ Token updated in .mcp.json"
echo ""
echo "Logged in as: $USERNAME"
echo ""
echo "IMPORTANT: Restart Claude Code / Claude Desktop to apply changes"
echo ""
echo "Token expires in: $(echo "$TOKEN_RESPONSE" | python3 -c "import sys, json; print(json.load(sys.stdin).get('expires_in', 'unknown'))") seconds"
