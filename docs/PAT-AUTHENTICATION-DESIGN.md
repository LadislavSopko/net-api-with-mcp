# Personal Access Token (PAT) Authentication Design

**Version:** 1.0
**Date:** 2025-10-28
**Purpose:** Enable AI agents to authenticate with MCP API without interactive OAuth flows

---

## Problem Statement

**Current Issue:**
- MCP API requires JWT tokens from Keycloak OAuth2 flow
- AI agents cannot perform interactive authentication (login forms, redirects)
- Current solution uses password grant which requires storing user credentials
- AI agents need long-lived, revocable authentication tokens

**Solution:**
Implement Personal Access Token (PAT) system similar to GitHub, GitLab, and Azure DevOps.

---

## System Architecture

### Hybrid Architecture: Centralized Auth + Decentralized Authz

**Company-Wide Setup:**
```
┌─────────────────────────────────────────────────────────────────┐
│                        KEYCLOAK (Company-Wide)                   │
│                   Central User Authentication                    │
│  - User pool for entire company                                 │
│  - Authentication only: user exists, account enabled            │
│  - NO application-specific roles                                │
└─────────────────────────────────────────────────────────────────┘
                                 │
                    ┌────────────┼────────────┐
                    │            │            │
         ┌──────────▼───┐  ┌────▼─────┐  ┌──▼──────────┐
         │   App A DB   │  │ App B DB │  │  App C DB   │
         │              │  │          │  │             │
         │ Users:       │  │ Users:   │  │ Users:      │
         │ - alice: Mgr │  │ - alice: │  │ - alice:    │
         │ - bob: Admin │  │   Member │  │   pending   │
         │ - carol: -   │  │ - bob:   │  │ - bob: Mgr  │
         │              │  │   Admin  │  │ - carol: Mbr│
         └──────────────┘  └──────────┘  └─────────────┘
         Each app manages   Same user,    Independent
         its own roles      different     role
                           roles          assignment
```

### ⚠️ CRITICAL SECURITY REQUIREMENT

**The PAT system MUST NOT bypass user validation or authorization:**

1. **Authentication (Keycloak)**: Validates user identity, account status
2. **Authorization (App DB)**: Queries CURRENT user roles from this app's database
3. When user's role changes in **this app's DB** (e.g., Member → Manager), their PAT must immediately respect new permissions
4. When user is disabled in **Keycloak**, all PATs must stop working immediately

### Hybrid Flow: Token Exchange + DB Roles

**Token Exchange Purpose**: Validate user identity with Keycloak (NOT to get roles)
**SecurityService Purpose**: Extract CURRENT roles from this app's database

---

## Approach A: Token Exchange + DB Roles (RECOMMENDED)

### User Onboarding Flow

```mermaid
sequenceDiagram
    participant NewUser as New User
    participant KC as Keycloak
    participant App as MCP API
    participant AppDB as App Database
    participant Admin as App Admin

    Note over NewUser,KC: 1. First Login (Keycloak Auth)
    NewUser->>KC: Login (company credentials)
    KC->>NewUser: JWT (identity only, no app roles)

    Note over NewUser,App: 2. App Sees Unknown User
    NewUser->>App: Access app with JWT
    App->>AppDB: Query user by email
    AppDB->>App: User NOT FOUND
    App->>AppDB: Create user(email, status='pending')
    App->>NewUser: "Access pending - contact admin"

    Note over Admin,AppDB: 3. Admin Assigns Roles
    Admin->>App: View pending users
    App->>Admin: Show: alice@company.com (pending)
    Admin->>App: Assign role: Member
    App->>AppDB: UPDATE users SET role='Member' WHERE email='alice@...'

    Note over NewUser,App: 4. User Gets Access
    NewUser->>App: Access app with JWT
    App->>AppDB: Query user by email
    AppDB->>App: User found, role=Member
    App->>NewUser: Access granted as Member
```

### PAT Generation Flow

```mermaid
sequenceDiagram
    participant User
    participant WebUI as Profile/Settings UI
    participant API as MCP API
    participant AppDB as App Database
    participant KC as Keycloak

    Note over User,WebUI: User Must Have Assigned Role
    User->>WebUI: Login with Keycloak
    User->>WebUI: Navigate to Profile → PAT
    User->>WebUI: Click "Generate PAT"
    WebUI->>API: POST /api/tokens<br/>Authorization: Bearer <jwt>

    API->>API: Extract user email from JWT
    API->>AppDB: Query user role
    alt User has pending/no role
        API->>WebUI: 403 Forbidden - No role assigned
    end

    AppDB->>API: User found, role=Member
    API->>API: Generate PAT (mcppat_xxx)
    API->>API: Hash token (SHA-256)
    API->>AppDB: Store(hash, userId, name, expires)
    API->>WebUI: Return plaintext PAT (ONCE!)
    WebUI->>User: Display token with warning:<br/>"Copy now, won't be shown again"
```

### PAT Authentication Flow (Hybrid)

```mermaid
sequenceDiagram
    participant Agent as AI Agent
    participant API as MCP API
    participant AppDB as App Database
    participant KC as Keycloak
    participant SecSvc as SecurityService
    participant Auth as Authorization Handler

    Note over Agent,API: 1. PAT Validation (App DB)
    Agent->>API: POST /mcp<br/>Authorization: Bearer mcppat_xxx
    API->>API: Hash incoming PAT
    API->>AppDB: Lookup by hash
    alt PAT Invalid/Expired/Revoked
        API->>Agent: 401 Unauthorized
    end
    AppDB->>API: PAT valid, userId=123

    Note over API,KC: 2. Identity Validation (Keycloak)
    API->>KC: Token Exchange<br/>subject_token=mcppat_xxx<br/>requested_subject=userId:123
    KC->>KC: Validate user exists<br/>Check account enabled
    alt User disabled/deleted in Keycloak
        KC->>API: 401 User invalid
        API->>Agent: 401 Unauthorized
    end
    KC->>API: JWT with user identity<br/>(preferred_username, email)

    Note over API,AppDB: 3. CRITICAL: Get CURRENT Roles from App DB
    API->>API: Extract user email from JWT
    API->>SecSvc: GetUserRoles(email)
    SecSvc->>AppDB: Query user by email
    AppDB->>SecSvc: User(id=123, role=Manager)
    SecSvc->>API: Current role: Manager

    Note over API,Auth: 4. Authorization with Current Roles
    API->>Auth: Execute policy check<br/>(user=alice, role=Manager)
    Auth->>Auth: Check: role >= required?
    alt Role insufficient
        Auth->>API: Deny (403)
        API->>Agent: 403 Forbidden
    end
    Auth->>API: Allow
    API->>Agent: MCP Response (200 OK)
    API->>AppDB: Update last_used_at
```

### How Hybrid Flow Works

**Step-by-step breakdown:**

1. **PAT Validation (App DB)**
   - Hash incoming PAT
   - Query app's database: `SELECT userId, expiresAt, revoked FROM personal_access_tokens WHERE hash = ?`
   - Check expiration and revocation status
   - If invalid → 401 Unauthorized

2. **Identity Validation (Keycloak Token Exchange - RFC 8693)**
   - Exchange PAT for JWT from Keycloak
   ```http
   POST /realms/mcppoc-realm/protocol/openid-connect/token
   grant_type=urn:ietf:params:oauth:grant-type:token-exchange
   subject_token=mcppat_xxx
   subject_token_type=urn:custom:pat
   requested_subject=userId:123
   client_id=mcppoc-api
   client_secret=<secret>
   ```
   - **Keycloak validates**: User exists? Account enabled? Not locked?
   - Returns JWT with user identity (email, preferred_username)
   - **Does NOT return roles** (roles are app-specific)

3. **Role Extraction (SecurityService + App DB)**
   ```csharp
   var email = jwt.Claims.First(c => c.Type == "preferred_username").Value;
   var user = await _dbContext.Users.FirstOrDefaultAsync(u => u.Email == email);
   // user.Role = CURRENT role in THIS app's DB (Member/Manager/Admin)
   ```

4. **Authorization (Existing MinimumRoleRequirementHandler)**
   - Uses CURRENT role from app DB
   - Policy check: `user.Role >= requirement.MinimumRole`
   - If insufficient → 403 Forbidden

### Benefits

✅ **Dual validation** - Both Keycloak (identity) and App DB (roles)
✅ **No Keycloak bypass** - User identity validated every request
✅ **Role changes immediate** - Queries current role from app DB
✅ **Account disabled = PAT stops** - Keycloak blocks disabled accounts
✅ **Independent app roles** - Each app manages its own authorization
✅ **Standard OAuth2** - Uses RFC 8693 token exchange
✅ **Audit trail** - Keycloak logs identity validation
✅ **No role sync needed** - Roles live only in app DB

### Keycloak Configuration Required

```javascript
// In Keycloak Admin Console:
// 1. Client Settings → mcppoc-api
// 2. Enable "Token Exchange" capability
// 3. Add custom token type: urn:custom:pat
// 4. Configure exchange policy:
{
  "tokenExchange": {
    "enabled": true,
    "supportedSubjectTokenTypes": [
      "urn:ietf:params:oauth:token-type:access_token",
      "urn:custom:pat"  // Custom PAT type
    ],
    "policy": {
      "validateSubject": true,  // Verify userId exists in Keycloak
      "validateAccountStatus": true  // Check account enabled/locked
    }
  }
}
```

**Note:** Keycloak does NOT need to know about app-specific roles. Each application manages its own roles independently.

---

## Approach B: PAT Without Keycloak Validation (NOT RECOMMENDED)

### High-Level Flow

```mermaid
sequenceDiagram
    participant Agent as AI Agent
    participant API as MCP API
    participant AppDB as App Database
    participant SecSvc as SecurityService
    participant Auth as Authorization Handler

    Note over Agent,API: 1. PAT Validation (App DB Only)
    Agent->>API: POST /mcp<br/>Authorization: Bearer mcppat_xxx
    API->>API: Hash incoming PAT
    API->>AppDB: Lookup by hash
    alt PAT Invalid/Expired/Revoked
        API->>Agent: 401 Unauthorized
    end
    AppDB->>API: PAT valid, userId=123, email=alice@company.com

    Note over API,AppDB: 2. Get Roles (No Keycloak Check!)
    API->>API: Create ClaimsPrincipal<br/>with email from PAT record
    API->>SecSvc: GetUserRoles(email)
    SecSvc->>AppDB: Query user by email
    AppDB->>SecSvc: User(id=123, role=Manager)
    SecSvc->>API: Current role: Manager

    Note over API,Auth: 3. Authorization with Current Roles
    API->>Auth: Execute policy check<br/>(user=alice, role=Manager)
    Auth->>Auth: Check: role >= required?
    alt Role insufficient
        Auth->>API: Deny (403)
        API->>Agent: 403 Forbidden
    end
    Auth->>API: Allow
    API->>Agent: MCP Response (200 OK)
    API->>AppDB: Update last_used_at
```

### How It Works

1. **PAT Validation (App DB)**: Validates PAT exists and is not expired/revoked
2. **Direct Role Query**: Queries CURRENT user role from app database
3. **Authorization**: Uses existing `MinimumRoleRequirementHandler` with role from DB

### ⚠️ Critical Limitation

**SKIPS Keycloak identity validation!**

If user is disabled/deleted in Keycloak, their PAT will STILL WORK because:
- PAT record in app DB is still valid
- No check against Keycloak user status
- App DB doesn't know user was disabled in Keycloak

### When This Might Be Acceptable

Only use this approach if:
1. **Keycloak configuration is impossible** (strict corporate policy)
2. **Manual user management** - App admin manually disables users in app DB when notified of Keycloak changes
3. **Low-risk environment** - Internal tools with trusted users
4. **User lifecycle sync** - Automated process syncs user disabled/deleted events from Keycloak to app DB

### Benefits

✅ **Simpler implementation** - No token exchange setup
✅ **Fewer Keycloak calls** - Only app DB queries
✅ **Works without Keycloak changes** - No server configuration needed

### ⚠️ Limitations

❌ **No Keycloak user validation** - Disabled users can still authenticate with PAT
❌ **Manual sync required** - Need process to sync user status from Keycloak
❌ **Security gap** - User deleted in Keycloak but PAT still works
❌ **Less audit trail** - No Keycloak logs of PAT usage

---

## Comparison: Approach A vs B

| Aspect | Approach A (Token Exchange + DB Roles) | Approach B (DB Only) |
|--------|---------------------------------------|---------------------|
| **Keycloak validation** | ✅ Every request validates user identity | ❌ No Keycloak check |
| **Role source** | ✅ App DB (immediate) | ✅ App DB (immediate) |
| **User disabled in KC** | ✅ PAT stops working immediately | ❌ PAT continues to work |
| **User deleted in KC** | ✅ PAT fails authentication | ❌ PAT continues to work |
| **Role changes in app** | ✅ Immediate (queries DB) | ✅ Immediate (queries DB) |
| **Complexity** | ⚠️ Requires Keycloak config | ✅ Simpler |
| **Audit trail** | ✅ Keycloak + app logs | ⚠️ App logs only |
| **Security** | ✅ Dual validation (KC + DB) | ⚠️ App DB only |
| **Enterprise ready** | ✅ Yes | ⚠️ Requires user lifecycle sync |

## Recommended Approach

**Use Approach A (Token Exchange + DB Roles)** for production systems because:

1. **Security first** - Validates user identity with Keycloak on every request
2. **Immediate role changes** - Queries current role from app DB
3. **Account disabled = PAT stops** - Keycloak blocks disabled accounts
4. **Independent app authorization** - Each app manages its own roles
5. **Standard protocol** - RFC 8693 is industry standard
6. **Better audit** - Complete trail in both Keycloak and app

**Use Approach B** only if:
- Keycloak token exchange configuration is not possible
- You have automated Keycloak → App DB user lifecycle sync
- Low-risk internal tools environment

---

## Database Schema

### PersonalAccessToken Table

```sql
CREATE TABLE PersonalAccessTokens
(
    Id              UNIQUEIDENTIFIER PRIMARY KEY DEFAULT NEWID(),
    UserId          INT NOT NULL,                    -- Links to Users table
    TokenHash       VARCHAR(64) NOT NULL UNIQUE,     -- SHA-256 hash (hex string)
    TokenPrefix     VARCHAR(10) NOT NULL,            -- First 8 chars for identification
    Name            NVARCHAR(100) NOT NULL,          -- User-friendly name
    Scopes          NVARCHAR(500) NOT NULL,          -- JSON array of scopes
    ExpiresAt       DATETIME2 NOT NULL,              -- Expiration date
    CreatedAt       DATETIME2 NOT NULL DEFAULT GETUTCDATE(),
    LastUsedAt      DATETIME2 NULL,                  -- Updated on each use
    RevokedAt       DATETIME2 NULL,                  -- NULL = active, set = revoked
    CreatedByIp     VARCHAR(50) NULL,                -- Audit trail
    LastUsedIp      VARCHAR(50) NULL,                -- Audit trail

    CONSTRAINT FK_PAT_User FOREIGN KEY (UserId) REFERENCES Users(Id) ON DELETE CASCADE,
    INDEX IX_PAT_TokenHash (TokenHash),              -- Fast lookup
    INDEX IX_PAT_UserId (UserId),                    -- User's tokens
    INDEX IX_PAT_ExpiresAt (ExpiresAt)               -- Cleanup expired tokens
);
```

### Example Data

| Id | UserId | TokenHash | TokenPrefix | Name | Scopes | ExpiresAt | LastUsedAt | RevokedAt |
|----|--------|-----------|-------------|------|--------|-----------|------------|-----------|
| guid1 | 1 | sha256(token) | mcppat_ab | "Claude Agent" | ["mcp:read", "mcp:write"] | 2025-11-27 | 2025-10-28 | NULL |
| guid2 | 2 | sha256(token) | mcppat_xy | "GitHub Actions" | ["mcp:read"] | 2025-12-27 | NULL | NULL |
| guid3 | 1 | sha256(token) | mcppat_cd | "Old Token" | ["mcp:read"] | 2025-09-01 | 2025-08-15 | 2025-10-20 |

---

## Token Format

### Structure

```
mcppat_[32-40 random characters]
```

**Example:**
```
mcppat_a7b9d8e3f2c1d0e9f8a7b6c5d4e3f2a1b0c9d8e7f6
```

### Components

1. **Prefix: `mcppat_`**
   - Identifies token type (MCP Personal Access Token)
   - Helps with token scanning/detection in logs
   - Similar to GitHub (`ghp_`), GitLab (`glpat-`)

2. **Random String: 32-40 characters**
   - Cryptographically secure random bytes
   - Base62 encoded (alphanumeric: `[A-Za-z0-9]`)
   - Provides ~190-238 bits of entropy
   - Sufficient to prevent brute force attacks

### Generation Algorithm

```csharp
public static class PatGenerator
{
    private const string Prefix = "mcppat_";
    private const int TokenLength = 40; // characters after prefix
    private const string Base62Chars = "0123456789ABCDEFGHIJKLMNOPQRSTUVWXYZabcdefghijklmnopqrstuvwxyz";

    public static string GenerateToken()
    {
        var randomBytes = new byte[30]; // 240 bits of entropy
        using (var rng = RandomNumberGenerator.Create())
        {
            rng.GetBytes(randomBytes);
        }

        var tokenPart = new StringBuilder(TokenLength);
        foreach (var b in randomBytes)
        {
            tokenPart.Append(Base62Chars[b % Base62Chars.Length]);
        }

        return Prefix + tokenPart.ToString().Substring(0, TokenLength);
    }

    public static string HashToken(string token)
    {
        using var sha256 = SHA256.Create();
        var hashBytes = sha256.ComputeHash(Encoding.UTF8.GetBytes(token));
        return Convert.ToHexString(hashBytes); // Returns 64-char hex string
    }

    public static string GetTokenPrefix(string token)
    {
        // Store first 8 chars for user identification (safe to store)
        return token.Length >= 8 ? token.Substring(0, 8) : token;
    }
}
```

---

## Token Scopes

### Scope Definitions

| Scope | Description | Example Operations |
|-------|-------------|-------------------|
| `mcp:read` | Read-only access to MCP tools | list_tools, call read-only tools (get_by_id, get_all) |
| `mcp:write` | Write access to MCP tools | call write tools (create, update) |
| `mcp:admin` | Administrative operations | promote_to_manager, delete |
| `mcp:*` | Full access | All MCP operations |

### Scope Storage

```json
{
  "scopes": ["mcp:read", "mcp:write"]
}
```

Stored as JSON string in database, deserialized during validation.

---

## Token Lifecycle

### 1. Token Generation

**Endpoint:** `POST /api/tokens`

```http
POST /api/tokens HTTP/1.1
Authorization: Bearer <keycloak-jwt>
Content-Type: application/json

{
  "name": "Claude AI Agent",
  "scopes": ["mcp:read", "mcp:write"],
  "expiresInDays": 90
}
```

**Response:**
```json
{
  "token": "mcppat_a7b9d8e3f2c1d0e9f8a7b6c5d4e3f2a1b0c9d8e7f6",
  "tokenId": "guid-here",
  "name": "Claude AI Agent",
  "scopes": ["mcp:read", "mcp:write"],
  "createdAt": "2025-10-28T10:00:00Z",
  "expiresAt": "2026-01-26T10:00:00Z",
  "warning": "Save this token now. You won't be able to see it again!"
}
```

**⚠️ CRITICAL:** Token is shown in plaintext ONLY ONCE during creation.

### 2. Token Validation

**Flow:**

1. Extract token from `Authorization: Bearer <token>` header
2. Check token format (starts with `mcppat_`)
3. Hash token using SHA-256
4. Lookup hash in database
5. Check if token exists and not revoked (`RevokedAt IS NULL`)
6. Check if token is not expired (`ExpiresAt > GETUTCDATE()`)
7. Parse scopes from JSON
8. Create `ClaimsPrincipal` with user claims
9. Update `LastUsedAt` timestamp
10. Continue to authorization policies

### 3. Token Revocation

**Endpoint:** `DELETE /api/tokens/{tokenId}`

```http
DELETE /api/tokens/guid-here HTTP/1.1
Authorization: Bearer <keycloak-jwt>
```

**Action:**
- Set `RevokedAt = GETUTCDATE()`
- Token immediately becomes invalid

### 4. Token Rotation

**Endpoint:** `POST /api/tokens/{tokenId}/rotate`

```http
POST /api/tokens/guid-here/rotate HTTP/1.1
Authorization: Bearer <keycloak-jwt>
```

**Action:**
1. Revoke old token
2. Generate new token with same metadata
3. Return new token (same one-time display rules)

---

## Authentication Middleware

### Approach A: PAT Authentication Handler with Token Exchange

```csharp
public class PatAuthenticationHandler : AuthenticationHandler<PatAuthenticationOptions>
{
    private readonly IPatService _patService;
    private readonly IKeycloakTokenExchange _keycloakExchange;
    private readonly IConfiguration _config;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 1. Check Authorization header
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return AuthenticateResult.NoResult();
        }

        var headerValue = authHeader.ToString();
        if (!headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = headerValue.Substring("Bearer ".Length).Trim();

        // 2. Check if it's a PAT (starts with mcppat_)
        if (!token.StartsWith("mcppat_", StringComparison.OrdinalIgnoreCase))
        {
            // Not a PAT, let JWT handler process it
            return AuthenticateResult.NoResult();
        }

        // 3. Validate PAT against our database
        var validationResult = await _patService.ValidateTokenAsync(
            token,
            Request.HttpContext.Connection.RemoteIpAddress?.ToString());

        if (!validationResult.IsValid)
        {
            return AuthenticateResult.Fail(validationResult.ErrorMessage);
        }

        // 4. CRITICAL: Exchange PAT for Keycloak JWT with CURRENT user roles
        var exchangeResult = await _keycloakExchange.ExchangePatForJwtAsync(
            token,
            validationResult.UserId);

        if (!exchangeResult.Success)
        {
            Logger.LogWarning("Token exchange failed for PAT {TokenId}: {Error}",
                validationResult.TokenId, exchangeResult.ErrorMessage);
            return AuthenticateResult.Fail("Failed to exchange PAT for access token");
        }

        // 5. Validate the exchanged JWT
        var jwtHandler = new JwtSecurityTokenHandler();
        var jwtToken = jwtHandler.ReadJwtToken(exchangeResult.AccessToken);

        // 6. Extract claims from JWT (these have CURRENT roles!)
        var claims = jwtToken.Claims.ToList();

        // Add PAT-specific claims
        claims.Add(new Claim("auth_method", "pat"));
        claims.Add(new Claim("token_id", validationResult.TokenId.ToString()));

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        Logger.LogInformation(
            "PAT authentication successful for user {User} via token exchange",
            principal.FindFirst("preferred_username")?.Value);

        return AuthenticateResult.Success(ticket);
    }
}

public interface IKeycloakTokenExchange
{
    Task<TokenExchangeResult> ExchangePatForJwtAsync(string pat, int userId);
}

public class KeycloakTokenExchange : IKeycloakTokenExchange
{
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly IConfiguration _config;
    private readonly ILogger<KeycloakTokenExchange> _logger;

    public async Task<TokenExchangeResult> ExchangePatForJwtAsync(string pat, int userId)
    {
        var httpClient = _httpClientFactory.CreateClient();

        var tokenEndpoint = $"{_config["Keycloak:Authority"]}/protocol/openid-connect/token";

        var parameters = new Dictionary<string, string>
        {
            ["grant_type"] = "urn:ietf:params:oauth:grant-type:token-exchange",
            ["subject_token"] = pat,
            ["subject_token_type"] = "urn:custom:pat",
            ["requested_subject"] = userId.ToString(),
            ["client_id"] = _config["Keycloak:ClientId"]!,
            ["client_secret"] = _config["Keycloak:ClientSecret"]!,
            ["requested_token_type"] = "urn:ietf:params:oauth:token-type:access_token"
        };

        var content = new FormUrlEncodedContent(parameters);

        try
        {
            var response = await httpClient.PostAsync(tokenEndpoint, content);

            if (!response.IsSuccessStatusCode)
            {
                var error = await response.Content.ReadAsStringAsync();
                _logger.LogWarning("Token exchange failed: {StatusCode} - {Error}",
                    response.StatusCode, error);

                return new TokenExchangeResult
                {
                    Success = false,
                    ErrorMessage = $"Token exchange failed: {response.StatusCode}"
                };
            }

            var tokenResponse = await response.Content.ReadFromJsonAsync<KeycloakTokenResponse>();

            return new TokenExchangeResult
            {
                Success = true,
                AccessToken = tokenResponse!.AccessToken,
                ExpiresIn = tokenResponse.ExpiresIn
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Exception during token exchange");
            return new TokenExchangeResult
            {
                Success = false,
                ErrorMessage = ex.Message
            };
        }
    }
}

public class TokenExchangeResult
{
    public bool Success { get; set; }
    public string AccessToken { get; set; } = string.Empty;
    public int ExpiresIn { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
}

public class KeycloakTokenResponse
{
    [JsonPropertyName("access_token")]
    public string AccessToken { get; set; } = string.Empty;

    [JsonPropertyName("expires_in")]
    public int ExpiresIn { get; set; }

    [JsonPropertyName("refresh_token")]
    public string? RefreshToken { get; set; }
}
```

### Approach B: PAT Authentication Handler (Direct Query)

```csharp
public class PatAuthenticationHandlerDirect : AuthenticationHandler<PatAuthenticationOptions>
{
    private readonly IPatService _patService;
    private readonly IUserService _userService;

    protected override async Task<AuthenticateResult> HandleAuthenticateAsync()
    {
        // 1. Check Authorization header
        if (!Request.Headers.TryGetValue("Authorization", out var authHeader))
        {
            return AuthenticateResult.NoResult();
        }

        var headerValue = authHeader.ToString();
        if (!headerValue.StartsWith("Bearer ", StringComparison.OrdinalIgnoreCase))
        {
            return AuthenticateResult.NoResult();
        }

        var token = headerValue.Substring("Bearer ".Length).Trim();

        // 2. Check if it's a PAT (starts with mcppat_)
        if (!token.StartsWith("mcppat_", StringComparison.OrdinalIgnoreCase))
        {
            // Not a PAT, let JWT handler process it
            return AuthenticateResult.NoResult();
        }

        // 3. Validate PAT
        var validationResult = await _patService.ValidateTokenAsync(
            token,
            Request.HttpContext.Connection.RemoteIpAddress?.ToString());

        if (!validationResult.IsValid)
        {
            return AuthenticateResult.Fail(validationResult.ErrorMessage);
        }

        // 4. Get user from token
        var user = await _userService.GetByIdAsync(validationResult.UserId);
        if (user == null)
        {
            return AuthenticateResult.Fail("User not found");
        }

        // 5. Create claims principal
        // IMPORTANT: Authorization handler will query CURRENT user.Role from IUserService
        var claims = new List<Claim>
        {
            new Claim(ClaimTypes.NameIdentifier, user.Id.ToString()),
            new Claim(ClaimTypes.Name, user.Email),
            new Claim("preferred_username", user.Email),  // Match Keycloak claim
            new Claim("auth_method", "pat"),
            new Claim("token_id", validationResult.TokenId.ToString())
        };

        // Add scope claims
        foreach (var scope in validationResult.Scopes)
        {
            claims.Add(new Claim("scope", scope));
        }

        var identity = new ClaimsIdentity(claims, Scheme.Name);
        var principal = new ClaimsPrincipal(identity);
        var ticket = new AuthenticationTicket(principal, Scheme.Name);

        Logger.LogInformation(
            "PAT authentication successful for user {Email}",
            user.Email);

        return AuthenticateResult.Success(ticket);
    }
}
```

### Registration in Program.cs

```csharp
// Add PAT authentication BEFORE JWT
builder.Services.AddAuthentication(options =>
{
    options.DefaultAuthenticateScheme = "MultiScheme";
    options.DefaultChallengeScheme = "MultiScheme";
})
.AddPolicyScheme("MultiScheme", "PAT or JWT", options =>
{
    options.ForwardDefaultSelector = context =>
    {
        var authHeader = context.Request.Headers["Authorization"].ToString();
        if (authHeader.StartsWith("Bearer mcppat_", StringComparison.OrdinalIgnoreCase))
        {
            return "PAT";
        }
        return JwtBearerDefaults.AuthenticationScheme;
    };
})
.AddScheme<PatAuthenticationOptions, PatAuthenticationHandler>("PAT", null)
.AddJwtBearer(options =>
{
    // Existing JWT configuration
    options.Authority = builder.Configuration["Keycloak:Authority"];
    // ...
});
```

---

## Service Implementation

### IPatService Interface

```csharp
public interface IPatService
{
    Task<PatCreationResult> CreateTokenAsync(int userId, string name, string[] scopes, int expiresInDays);
    Task<PatValidationResult> ValidateTokenAsync(string token, string? ipAddress);
    Task<IEnumerable<PatListItem>> GetUserTokensAsync(int userId);
    Task<bool> RevokeTokenAsync(Guid tokenId, int userId);
    Task<PatCreationResult> RotateTokenAsync(Guid tokenId, int userId);
}

public class PatValidationResult
{
    public bool IsValid { get; set; }
    public string ErrorMessage { get; set; } = string.Empty;
    public int UserId { get; set; }
    public Guid TokenId { get; set; }
    public string[] Scopes { get; set; } = Array.Empty<string>();
}

public class PatCreationResult
{
    public bool Success { get; set; }
    public string? Token { get; set; }  // Plaintext token (only on creation)
    public Guid TokenId { get; set; }
    public string Name { get; set; } = string.Empty;
    public string[] Scopes { get; set; } = Array.Empty<string>();
    public DateTime CreatedAt { get; set; }
    public DateTime ExpiresAt { get; set; }
}
```

### PatService Implementation

```csharp
public class PatService : IPatService
{
    private readonly DbContext _db;
    private readonly ILogger<PatService> _logger;

    public async Task<PatCreationResult> CreateTokenAsync(
        int userId, string name, string[] scopes, int expiresInDays)
    {
        // 1. Generate token
        var token = PatGenerator.GenerateToken();
        var tokenHash = PatGenerator.HashToken(token);
        var tokenPrefix = PatGenerator.GetTokenPrefix(token);

        // 2. Calculate expiration
        var expiresAt = DateTime.UtcNow.AddDays(expiresInDays);

        // 3. Create entity
        var pat = new PersonalAccessToken
        {
            Id = Guid.NewGuid(),
            UserId = userId,
            TokenHash = tokenHash,
            TokenPrefix = tokenPrefix,
            Name = name,
            Scopes = JsonSerializer.Serialize(scopes),
            ExpiresAt = expiresAt,
            CreatedAt = DateTime.UtcNow,
            CreatedByIp = GetCurrentIpAddress()
        };

        _db.PersonalAccessTokens.Add(pat);
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "PAT created for user {UserId}: {TokenPrefix}... (expires {ExpiresAt})",
            userId, tokenPrefix, expiresAt);

        return new PatCreationResult
        {
            Success = true,
            Token = token,  // ⚠️ Only time plaintext token is returned!
            TokenId = pat.Id,
            Name = name,
            Scopes = scopes,
            CreatedAt = pat.CreatedAt,
            ExpiresAt = expiresAt
        };
    }

    public async Task<PatValidationResult> ValidateTokenAsync(
        string token, string? ipAddress)
    {
        // 1. Hash incoming token
        var tokenHash = PatGenerator.HashToken(token);

        // 2. Lookup in database
        var pat = await _db.PersonalAccessTokens
            .FirstOrDefaultAsync(p => p.TokenHash == tokenHash);

        if (pat == null)
        {
            _logger.LogWarning("PAT validation failed: token not found");
            return new PatValidationResult
            {
                IsValid = false,
                ErrorMessage = "Invalid token"
            };
        }

        // 3. Check if revoked
        if (pat.RevokedAt.HasValue)
        {
            _logger.LogWarning(
                "PAT validation failed: token {TokenPrefix} was revoked on {RevokedAt}",
                pat.TokenPrefix, pat.RevokedAt);
            return new PatValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token has been revoked"
            };
        }

        // 4. Check if expired
        if (pat.ExpiresAt < DateTime.UtcNow)
        {
            _logger.LogWarning(
                "PAT validation failed: token {TokenPrefix} expired on {ExpiresAt}",
                pat.TokenPrefix, pat.ExpiresAt);
            return new PatValidationResult
            {
                IsValid = false,
                ErrorMessage = "Token has expired"
            };
        }

        // 5. Update last used
        pat.LastUsedAt = DateTime.UtcNow;
        pat.LastUsedIp = ipAddress;
        await _db.SaveChangesAsync();

        // 6. Parse scopes
        var scopes = JsonSerializer.Deserialize<string[]>(pat.Scopes) ?? Array.Empty<string>();

        _logger.LogTrace(
            "PAT validation succeeded for user {UserId}, token {TokenPrefix}",
            pat.UserId, pat.TokenPrefix);

        return new PatValidationResult
        {
            IsValid = true,
            UserId = pat.UserId,
            TokenId = pat.Id,
            Scopes = scopes
        };
    }

    public async Task<IEnumerable<PatListItem>> GetUserTokensAsync(int userId)
    {
        return await _db.PersonalAccessTokens
            .Where(p => p.UserId == userId)
            .OrderByDescending(p => p.CreatedAt)
            .Select(p => new PatListItem
            {
                Id = p.Id,
                Name = p.Name,
                TokenPrefix = p.TokenPrefix,
                Scopes = JsonSerializer.Deserialize<string[]>(p.Scopes),
                CreatedAt = p.CreatedAt,
                ExpiresAt = p.ExpiresAt,
                LastUsedAt = p.LastUsedAt,
                IsRevoked = p.RevokedAt.HasValue,
                IsExpired = p.ExpiresAt < DateTime.UtcNow
            })
            .ToListAsync();
    }

    public async Task<bool> RevokeTokenAsync(Guid tokenId, int userId)
    {
        var pat = await _db.PersonalAccessTokens
            .FirstOrDefaultAsync(p => p.Id == tokenId && p.UserId == userId);

        if (pat == null)
        {
            return false;
        }

        pat.RevokedAt = DateTime.UtcNow;
        await _db.SaveChangesAsync();

        _logger.LogInformation(
            "PAT {TokenPrefix} revoked by user {UserId}",
            pat.TokenPrefix, userId);

        return true;
    }

    public async Task<PatCreationResult> RotateTokenAsync(Guid tokenId, int userId)
    {
        // 1. Get old token
        var oldPat = await _db.PersonalAccessTokens
            .FirstOrDefaultAsync(p => p.Id == tokenId && p.UserId == userId);

        if (oldPat == null)
        {
            throw new InvalidOperationException("Token not found");
        }

        // 2. Revoke old token
        oldPat.RevokedAt = DateTime.UtcNow;

        // 3. Create new token with same properties
        var scopes = JsonSerializer.Deserialize<string[]>(oldPat.Scopes) ?? Array.Empty<string>();
        var daysRemaining = (oldPat.ExpiresAt - DateTime.UtcNow).Days;

        var newToken = await CreateTokenAsync(
            userId,
            oldPat.Name,
            scopes,
            Math.Max(1, daysRemaining));

        _logger.LogInformation(
            "PAT {OldPrefix} rotated to {NewPrefix} for user {UserId}",
            oldPat.TokenPrefix, PatGenerator.GetTokenPrefix(newToken.Token!), userId);

        return newToken;
    }
}
```

---

## API Endpoints

### TokensController

```csharp
[ApiController]
[Route("api/[controller]")]
[Authorize]  // Require Keycloak JWT for token management
public class TokensController : ControllerBase
{
    private readonly IPatService _patService;
    private readonly ILogger<TokensController> _logger;

    // POST /api/tokens - Create new PAT
    [HttpPost]
    public async Task<ActionResult<PatCreationResult>> CreateToken(
        [FromBody] CreatePatRequest request)
    {
        // Get user ID from JWT claims
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized("Invalid user claims");
        }

        var result = await _patService.CreateTokenAsync(
            userId,
            request.Name,
            request.Scopes,
            request.ExpiresInDays);

        return Ok(result);
    }

    // GET /api/tokens - List user's PATs
    [HttpGet]
    public async Task<ActionResult<IEnumerable<PatListItem>>> GetTokens()
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var tokens = await _patService.GetUserTokensAsync(userId);
        return Ok(tokens);
    }

    // DELETE /api/tokens/{id} - Revoke PAT
    [HttpDelete("{id}")]
    public async Task<ActionResult> RevokeToken(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        var success = await _patService.RevokeTokenAsync(id, userId);
        if (!success)
        {
            return NotFound();
        }

        return NoContent();
    }

    // POST /api/tokens/{id}/rotate - Rotate PAT
    [HttpPost("{id}/rotate")]
    public async Task<ActionResult<PatCreationResult>> RotateToken(Guid id)
    {
        var userIdClaim = User.FindFirst(ClaimTypes.NameIdentifier)?.Value;
        if (userIdClaim == null || !int.TryParse(userIdClaim, out var userId))
        {
            return Unauthorized();
        }

        try
        {
            var result = await _patService.RotateTokenAsync(id, userId);
            return Ok(result);
        }
        catch (InvalidOperationException ex)
        {
            return NotFound(new { error = ex.Message });
        }
    }
}

public record CreatePatRequest(
    string Name,
    string[] Scopes,
    int ExpiresInDays = 90
);
```

---

## Security Considerations

### ✅ Best Practices Implemented

1. **Token Hashing**
   - Use SHA-256 (fast, sufficient for random tokens)
   - Store only hash, never plaintext
   - 64-character hex string

2. **Entropy**
   - 240 bits of cryptographically secure randomness
   - Prevents brute force attacks
   - Use `RandomNumberGenerator` (not `Random`)

3. **One-Time Display**
   - Token shown in plaintext ONLY during creation
   - Cannot be retrieved later
   - Forces secure storage by user

4. **Expiration**
   - Default: 90 days
   - Maximum: 1 year (365 days)
   - Automatic cleanup of expired tokens

5. **Revocation**
   - Soft delete (set `RevokedAt`)
   - Immediate invalidation
   - Audit trail preserved

6. **Scoping**
   - Principle of least privilege
   - Token has only necessary permissions
   - Separate read/write/admin scopes

7. **Audit Trail**
   - Track creation IP
   - Track last used IP and timestamp
   - Enables security monitoring

8. **Rate Limiting** (recommended)
   - Limit token creation: 10 per user
   - Limit failed validations: 5/min per IP
   - Prevent brute force attempts

### ⚠️ Risks and Mitigations

| Risk | Impact | Mitigation |
|------|--------|------------|
| Token theft | Full account access | Short expiration, revocation, IP tracking |
| Token in logs | Exposure | Prefix-based detection, log scrubbing |
| Token in VCS | Exposure | `.gitignore` patterns, secret scanning |
| Brute force | Token guessing | 240-bit entropy, rate limiting |
| Replay attacks | Unauthorized access | HTTPS only, token expiration |

---

## Usage Examples

### 1. Generate Token (User via Web UI)

```bash
# User logs in with Keycloak, then:
curl -X POST http://127.0.0.1:5001/api/tokens \
  -H "Authorization: Bearer <keycloak-jwt>" \
  -H "Content-Type: application/json" \
  -d '{
    "name": "Claude AI Agent",
    "scopes": ["mcp:read", "mcp:write"],
    "expiresInDays": 90
  }'

# Response (SAVE THIS TOKEN!):
{
  "token": "mcppat_a7b9d8e3f2c1d0e9f8a7b6c5d4e3f2a1b0c9d8e7f6",
  "tokenId": "550e8400-e29b-41d4-a716-446655440000",
  "name": "Claude AI Agent",
  "scopes": ["mcp:read", "mcp:write"],
  "createdAt": "2025-10-28T10:00:00Z",
  "expiresAt": "2026-01-26T10:00:00Z",
  "warning": "Save this token now. You won't be able to see it again!"
}
```

### 2. Use Token (AI Agent)

```bash
# AI Agent uses PAT for MCP API
curl -X POST http://127.0.0.1:5001/mcp \
  -H "Authorization: Bearer mcppat_a7b9d8e3f2c1d0e9f8a7b6c5d4e3f2a1b0c9d8e7f6" \
  -H "Content-Type: application/json" \
  -d '{
    "jsonrpc": "2.0",
    "method": "tools/call",
    "params": {
      "name": "get_all"
    },
    "id": 1
  }'
```

### 3. List Tokens

```bash
curl http://127.0.0.1:5001/api/tokens \
  -H "Authorization: Bearer <keycloak-jwt>"

# Response:
[
  {
    "id": "550e8400-e29b-41d4-a716-446655440000",
    "name": "Claude AI Agent",
    "tokenPrefix": "mcppat_a7",
    "scopes": ["mcp:read", "mcp:write"],
    "createdAt": "2025-10-28T10:00:00Z",
    "expiresAt": "2026-01-26T10:00:00Z",
    "lastUsedAt": "2025-10-28T15:30:00Z",
    "isRevoked": false,
    "isExpired": false
  }
]
```

### 4. Revoke Token

```bash
curl -X DELETE http://127.0.0.1:5001/api/tokens/550e8400-e29b-41d4-a716-446655440000 \
  -H "Authorization: Bearer <keycloak-jwt>"

# Response: 204 No Content
```

---

## Testing Strategy

### Unit Tests

```csharp
[Fact]
public void Should_GenerateValidToken()
{
    var token = PatGenerator.GenerateToken();

    token.Should().StartWith("mcppat_");
    token.Should().HaveLength(47); // 7 (prefix) + 40 (random)
    token.Should().MatchRegex("^mcppat_[A-Za-z0-9]+$");
}

[Fact]
public void Should_HashTokenConsistently()
{
    var token = "mcppat_test123";
    var hash1 = PatGenerator.HashToken(token);
    var hash2 = PatGenerator.HashToken(token);

    hash1.Should().Be(hash2);
    hash1.Should().HaveLength(64); // SHA-256 hex
}

[Fact]
public async Task Should_ValidateValidToken()
{
    // Arrange
    var token = PatGenerator.GenerateToken();
    var hash = PatGenerator.HashToken(token);

    await _db.PersonalAccessTokens.AddAsync(new PersonalAccessToken
    {
        TokenHash = hash,
        UserId = 1,
        ExpiresAt = DateTime.UtcNow.AddDays(30)
    });
    await _db.SaveChangesAsync();

    // Act
    var result = await _patService.ValidateTokenAsync(token, "127.0.0.1");

    // Assert
    result.IsValid.Should().BeTrue();
    result.UserId.Should().Be(1);
}

[Fact]
public async Task Should_RejectExpiredToken()
{
    // Arrange
    var token = PatGenerator.GenerateToken();
    var hash = PatGenerator.HashToken(token);

    await _db.PersonalAccessTokens.AddAsync(new PersonalAccessToken
    {
        TokenHash = hash,
        UserId = 1,
        ExpiresAt = DateTime.UtcNow.AddDays(-1) // Expired
    });
    await _db.SaveChangesAsync();

    // Act
    var result = await _patService.ValidateTokenAsync(token, "127.0.0.1");

    // Assert
    result.IsValid.Should().BeFalse();
    result.ErrorMessage.Should().Contain("expired");
}
```

### Integration Tests

```csharp
[Fact]
public async Task Should_AuthenticateWithPAT_AndAccessMcpTools()
{
    // Arrange - Create PAT
    var patResult = await _patService.CreateTokenAsync(1, "Test", new[] { "mcp:read" }, 90);
    var mcpClient = new McpClientHelper(patResult.Token!);

    // Act - Call MCP tool
    var tools = await mcpClient.ListToolsAsync();

    // Assert
    tools.Should().NotBeEmpty();
}

[Fact]
public async Task Should_EnforceScopes_WhenUsingPAT()
{
    // Arrange - Create read-only PAT
    var patResult = await _patService.CreateTokenAsync(1, "ReadOnly", new[] { "mcp:read" }, 90);
    var mcpClient = new McpClientHelper(patResult.Token!);

    // Act - Try to call write operation
    var result = await mcpClient.CallToolAsync("create", new Dictionary<string, object?>
    {
        ["request"] = new { name = "Test", email = "test@example.com" }
    });

    // Assert
    result.IsError.Should().Be(true);
    // Should fail due to insufficient scopes
}
```

---

## Migration Path

### Phase 1: Database Setup (Week 1)
- [ ] Create `PersonalAccessTokens` table
- [ ] Add indexes
- [ ] Add foreign key to Users
- [ ] Seed test data

### Phase 2: Core Services (Week 1-2)
- [ ] Implement `PatGenerator` utility
- [ ] Implement `IPatService` and `PatService`
- [ ] Add unit tests (token generation, hashing, validation)

### Phase 3: Authentication Integration (Week 2)
- [ ] Create `PatAuthenticationHandler`
- [ ] Register multi-scheme authentication
- [ ] Update Program.cs
- [ ] Test authentication flow

### Phase 4: API Endpoints (Week 2-3)
- [ ] Create `TokensController`
- [ ] Add create/list/revoke/rotate endpoints
- [ ] Add authorization checks
- [ ] Add integration tests

### Phase 5: Documentation & UI (Week 3)
- [ ] Add API documentation (Swagger)
- [ ] Create user guide for PAT management
- [ ] Build Web UI for token management (optional)
- [ ] Add monitoring/logging

### Phase 6: Production Hardening (Week 4)
- [ ] Add rate limiting
- [ ] Implement token cleanup job (expired tokens)
- [ ] Add security headers
- [ ] Penetration testing
- [ ] Performance testing

---

## Comparison: Before vs After

### Before (Password Grant)

```bash
# AI Agent must store user credentials
export USERNAME="alice@example.com"
export PASSWORD="alice123"

# Get token before every session
TOKEN=$(curl -X POST http://127.0.0.1:8080/realms/mcppoc-realm/protocol/openid-connect/token \
  -d "grant_type=password" \
  -d "client_id=mcppoc-api" \
  -d "username=$USERNAME" \
  -d "password=$PASSWORD" | jq -r .access_token)

# Use token (expires in 5 minutes)
curl -H "Authorization: Bearer $TOKEN" http://127.0.0.1:5001/mcp
```

**Problems:**
- ❌ Stores user password
- ❌ Token expires quickly (5 min)
- ❌ Requires Keycloak availability
- ❌ Cannot revoke without changing password

### After (PAT)

```bash
# AI Agent uses long-lived PAT (generated once by user)
export PAT="mcppat_a7b9d8e3f2c1d0e9f8a7b6c5d4e3f2a1b0c9d8e7f6"

# Use PAT directly (valid for 90 days)
curl -H "Authorization: Bearer $PAT" http://127.0.0.1:5001/mcp
```

**Benefits:**
- ✅ No password storage
- ✅ Long-lived (90 days default)
- ✅ No Keycloak dependency
- ✅ Instantly revocable
- ✅ Scoped permissions
- ✅ Audit trail

---

## Conclusion

This PAT system provides:

1. **AI Agent Compatibility** - No interactive OAuth required
2. **Security** - Hashed storage, scopes, expiration, revocation
3. **Auditability** - Track creation, usage, and revocation
4. **User Control** - Users generate and manage their own tokens
5. **Backward Compatibility** - Existing JWT auth still works
6. **Industry Standard** - Follows GitHub/GitLab/Azure patterns

**Status:** Ready for implementation (Phase 5 of MCP POC)

---

**Next Steps:**
1. Review this design with team
2. Create database migration
3. Implement core services
4. Add integration tests
5. Update MCP documentation
