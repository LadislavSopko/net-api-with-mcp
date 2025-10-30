# Users and Permissions Guide

## System Overview

The POC has **TWO systems** working together:

1. **Keycloak** (Authentication) - Validates who you are
2. **UserService** (Authorization) - Validates what you can do

Both systems must have matching users for write operations to work!

## Available Users (Updated)

| Username | Password | Keycloak ✓ | UserService ✓ | User ID | App Role | Can Do |
|----------|----------|------------|---------------|---------|----------|---------|
| **alice@example.com** | alice123 | ✅ | ✅ | 1 | **Member** | Read + Create |
| **bob@example.com** | bob123 | ✅ | ✅ | 2 | **Manager** | Read + Create + Update |
| **carol@example.com** | carol123 | ✅ | ✅ | 3 | **Admin** | Everything (Create, Update, Promote) |
| **admin** | admin123 | ✅ | ✅ | 100 | **Admin** | Everything (Create, Update, Promote) |
| **user** | user123 | ✅ | ✅ | 101 | **Member** | Read + Create |
| **viewer** | viewer123 | ✅ | ✅ | 102 | **Viewer** | Read only |

## Role Hierarchy

```
Admin (3)    → Can do EVERYTHING (Read + Create + Update + Promote)
   ↓
Manager (2)  → Can Read + Create + Update users
   ↓
Member (1)   → Can Read + Create users
   ↓
Viewer (0)   → Can Read only (no write operations)
```

## MCP Tools and Required Roles

| Tool Name | HTTP Method | Required Role | Who Can Use It |
|-----------|-------------|---------------|----------------|
| `get_by_id` | GET | Authenticated | Everyone (admin, user, viewer, alice, bob, carol) |
| `get_all` | GET | Authenticated | Everyone (admin, user, viewer, alice, bob, carol) |
| `get_public_info` | GET | None (public) | Anyone (no token needed) |
| `get_scope_id` | GET | Authenticated | Everyone (admin, user, viewer, alice, bob, carol) |
| `create` | POST | Member+ | admin, user, alice, bob, carol (NOT viewer) |
| `update` | PUT | Manager+ | admin, bob, carol (NOT viewer, user, alice) |
| `promote_to_manager` | POST | Admin | admin, carol (NOT viewer, user, alice, bob) |

## Quick Reference

### For Admin Access (Full Permissions)
```bash
# Login with admin (User ID: 100)
TOKEN=$(bash get-token.sh admin admin123)

# Or use Carol (User ID: 3, also Admin)
TOKEN=$(bash get-token.sh carol@example.com carol123)
```

### For Manager Access (Create + Update)
```bash
TOKEN=$(bash get-token.sh bob@example.com bob123)
```

### For Member Access (Read + Create)
```bash
TOKEN=$(bash get-token.sh user user123)
# Or
TOKEN=$(bash get-token.sh alice@example.com alice123)
```

### For Viewer Access (Read only)
```bash
TOKEN=$(bash get-token.sh viewer viewer123)
```

## Testing Different Permissions

### Test 1: Everyone can READ
```bash
TOKEN=$(bash get-token.sh admin admin123)
curl -X GET http://127.0.0.1:5001/api/users \
  -H "Authorization: Bearer $TOKEN"
```

### Test 2: Member can CREATE
```bash
TOKEN=$(bash get-token.sh alice@example.com alice123)
curl -X POST http://127.0.0.1:5001/api/users \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"New User","email":"new@example.com"}'
```

### Test 3: Only Manager+ can UPDATE
```bash
# This works (Bob is Manager)
TOKEN=$(bash get-token.sh bob@example.com bob123)
curl -X PUT http://127.0.0.1:5001/api/users/1 \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Updated Name","email":"updated@example.com"}'

# This FAILS (Alice is only Member)
TOKEN=$(bash get-token.sh alice@example.com alice123)
curl -X PUT http://127.0.0.1:5001/api/users/1 \
  -H "Authorization: Bearer $TOKEN" \
  -H "Content-Type: application/json" \
  -d '{"name":"Updated Name","email":"updated@example.com"}'
# Returns: 403 Forbidden
```

### Test 4: Only Admin can PROMOTE
```bash
# This works (admin is Admin)
TOKEN=$(bash get-token.sh admin admin123)
curl -X POST http://127.0.0.1:5001/api/users/3/promote \
  -H "Authorization: Bearer $TOKEN"

# This FAILS (Bob is only Manager)
TOKEN=$(bash get-token.sh bob@example.com bob123)
curl -X POST http://127.0.0.1:5001/api/users/3/promote \
  -H "Authorization: Bearer $TOKEN"
# Returns: 403 Forbidden
```

## How It Works

1. **Authentication** (Keycloak):
   - You provide username/password
   - Keycloak validates credentials
   - Returns JWT token with claims (username, email, realm_roles)

2. **Authorization** (UserService + Policies):
   - Your token contains `preferred_username` claim
   - App looks up that username in UserService
   - Checks if user's Role >= Required Role for the endpoint
   - Allows or denies access

## Common Issues

### "Unauthorized" (401)
- Token missing or invalid
- Token expired (tokens last 1 hour)
- Solution: Get a new token

### "Forbidden" (403)
- User authenticated but doesn't have required role
- Example: Member trying to Update (needs Manager+)
- Solution: Use user with higher role

### Write operations not working
- User exists in Keycloak but NOT in UserService
- Solution: User has been added to UserService (see table above)

## Recommended Users

**For Development/Testing:**
- Use **admin** (admin123) - Has full access to everything

**For MCP Integration:**
- Use **admin** token in `.mcp.json` for full tool access

**For Testing Authorization:**
- Use different users (alice, bob, carol) to test permission levels
