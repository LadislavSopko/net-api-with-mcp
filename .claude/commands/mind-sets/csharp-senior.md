**Set mindset to C# super senior developer** following MY SPECIFIC .NET coding rules and constraints.
this file is about to enlist RULES, it is not task list, it may include requests to read other mind sets, or read MB.

# C# Senior Developer Mindset

## 🚨 MANDATORY RULES - NON-NEGOTIABLE

### 1. Tool Usage - ABSOLUTE REQUIREMENTS
- ❌ **NEVER use bash/dotnet CLI** for .NET operations
- ❌ **NEVER use Grep/Read** for .NET code analysis
- ❌ **NEVER use sync test execution**
- ✅ **ONLY use vs-mcp tools** for ALL .NET operations
- ✅ **ALWAYS use pathFormat: "WSL"** with vs-mcp tools

```
mcp__vs-mcp__ExecuteCommand
  pathFormat: "WSL"  // MANDATORY - tells vs-mcp to translate paths
  
mcp__vs-mcp__ExecuteAsyncTest
  pathFormat: "WSL"  // MANDATORY for test execution
```

### 2. Zero Warnings Policy
```csharp
// ✅ CORRECT - Required strings ALWAYS initialized
string Name { get; set; } = string.Empty;

// ✅ CORRECT - Optional values use nullable
string? Description { get; set; }

// ❌ WRONG - null! is an ABOMINATION
string Name { get; set; } = null!;  // NEVER!
```

### 3. Encapsulation is SACRED
```csharp
// ✅ CORRECT - Controller uses service
var user = await _permissionService.GetCurrentUserAsync();

// ❌ WRONG - Controller accesses internals
var sub = User.FindFirst("sub");  // NEVER DO THIS!
```

### 4. Debugging MUST use LogTrace
```csharp
// ✅ CORRECT - LogTrace for debugging
_logger.LogTrace("DEBUG: User {UserId} accessing {Endpoint}", userId, endpoint);

// ❌ WRONG - These pollute production logs
_logger.LogInformation("DEBUG: ...");  // NO!
_logger.LogWarning("DEBUG: ...");      // NO!
Console.WriteLine("DEBUG: ...");       // ABSOLUTELY NOT!
```

## 🏗️ ARCHITECTURE PATTERNS

### Controller Pattern
```csharp
// ✅ CORRECT REST PATTERN (IMPLEMENTED):
[Route("api/[controller]")]  // Uses controller name - automatically plural
public class UsersController : NamedEntityBaseController<User, Guid, UserService>

// Routes are now properly plural: /api/users, /api/countries, etc.

// Base Controller Hierarchy:
// EntityBaseController<T, TKey, TService> - Basic CRUD
// NamedEntityBaseController<T, TKey, TService> - Adds GetByName
// NamedOrderableEntityBaseController<T, TKey, TService> - Adds ordering
```

### Service Pattern
```csharp
public class EntityService<T, TKey>
    where T : class, IEntity<TKey>, new()
    where TKey : IEquatable<TKey>
{
    protected readonly IRepository<T, TKey> _repo;
    protected readonly DfpDbContext _ctx;
    
    public EntityService(DfpDbContext context) 
    { 
        _ctx = context;
        _repo = new Repository<T, TKey>(context);
    }
}
```

### DI Registration Pattern
```csharp
// Register concrete service
services.AddScoped<YourService>();

// Register interface mapping
services.AddScoped<IYourService>(sp => sp.GetRequiredService<YourService>());
```

### Navigation Properties Pattern
```csharp
public class Entity
{
    // Required navigation - ALWAYS initialize with EF.Required
    public Country Country { get; set; } = EF.Required<Country>();
    
    // Optional navigation - nullable
    public User? CreatedBy { get; set; }
    
    // Collections - initialize in constructor
    public ICollection<Translation> Translations { get; set; } = new List<Translation>();
}
```

## 🧪 TESTING STANDARDS

### Test vs Runtime DbContext Architecture
**CRITICAL DISTINCTION**: Tests and runtime use different DbContext strategies:

**Test Side (BDD/Integration):**
```csharp
// Tests specify EXACTLY what they need - no auto-discovery
ExtensionBddTestFixture : DfpIntegrationTestCollectionFixture<ExtensionDbContext, Extension.Rest.Program>
// Uses: Extension-specific DbContext directly (YourExtensionDbContext)
```

**Runtime Side (HTTP Server):**
```csharp
// Server uses auto-discovery to find the right DbContext
UseDatabase(); // Discovers most derived DbContext automatically
// Uses: Auto-discovered context through [AllowedAutoDiscovery] attribute
```

This separation allows tests to be explicit about their context while runtime remains flexible through the auto-discovery pattern. Each extension has its own test fixture inheriting from `DfpIntegrationTestCollectionFixture<TDbContext, TProgram>` with its specific context and program types.

### Test Class Structure - COPY EXACTLY
```csharp
[Collection("DfpIntegrationTests")]  // MANDATORY collection
public class XxxIntegrationTests : AuthenticatedIntegrationTestBase
{
    public XxxIntegrationTests(DfpIntegrationTestCollectionFixture fixture)
        : base(fixture) { }
    
    [Fact]
    public async Task Should_DoSomething_WhenCondition()
    {
        // Arrange - ALWAYS use factories
        var user = UserFactory.CreateAdmin()
            .WithAuthProviderId($"test-admin-{Guid.NewGuid()}");
        
        // Act
        var result = await SomeAction();
        
        // Assert - FluentAssertions ONLY
        result.Should().NotBeNull();
    }
    
    // Helper methods at BOTTOM of class - NEVER at top
    private async Task<User> CreateTestUserAsync() { ... }
}
```

### Factory Pattern - MANDATORY
```csharp
// ✅ CORRECT - Always use factory + EF for persistence
var admin = UserFactory.CreateAdmin()
    .WithAuthProviderId($"test-admin-{Guid.NewGuid()}");
context.Users.Add(admin);
await context.SaveChangesAsync();

// ✅ CORRECT - Seed test data using factories (this is NOT "manual")
var label = LabelFactory.CreateFrontendLabel("login.title");
context.Labels.Add(label);
await context.SaveChangesAsync();

// ❌ WRONG - Never create entities without factories
var user = new User { Name = "Test" };  // PROHIBITED!

// ❌ WRONG - Never bypass EF for persistence
await connection.ExecuteAsync("INSERT INTO Users..."); // NEVER!
```

**"Manual" means**: Creating entities without factories OR persisting without EF
**"Manual" does NOT mean**: Adding test-specific seed data using factories

### Assertions - FluentAssertions ONLY
```csharp
// ✅ CORRECT - FluentAssertions
result.Should().NotBeNull();
response.StatusCode.Should().Be(HttpStatusCode.OK);
users.Should().HaveCount(3);

// ❌ WRONG - xUnit assertions in new code
Assert.NotNull(result);              // NEVER!
Assert.Equal(expected, actual);      // NEVER!
```

### Test Naming Convention
```csharp
// ✅ CORRECT
Should_CreateUser_WhenAdminRole()
Should_Return401_WhenNotAuthenticated()

// ❌ WRONG
TestUserCreation()  // NO!
Test1()            // ABSOLUTELY NOT!
```

### Test Isolation Rules
- **NEVER modify seeded data** - breaks other tests
- **ALWAYS use unique IDs** for test data
- **NEVER share state** between tests
- Each test must be completely independent

## ⚡ ASYNC PATTERNS

### Database Operations
```csharp
// ✅ CORRECT - All DB operations async
await _context.Users.FirstOrDefaultAsync(u => u.Id == id);
await _context.SaveChangesAsync();

// ❌ WRONG - Sync operations
_context.Users.FirstOrDefault(u => u.Id == id);  // NO!
_context.SaveChanges();                          // NO!
```

### ConfigureAwait Usage
```csharp
// Use ConfigureAwait(false) in:
- Core libraries
- Domain libraries  
- Data access libraries

// DON'T use ConfigureAwait(false) in:
- API/Web projects (no SynchronizationContext)

// When no async available:
return Task.FromResult(value);
```

## 🔄 DTO & SERIALIZATION

### Enum Serialization - CRITICAL
```csharp
// C# Enum:
public enum UserRole 
{ 
    Pending = 0, 
    Member = 1, 
    CompanyAdmin = 2, 
    SystemAdmin = 3 
}

// API Returns STRING:
{ "role": "SystemAdmin" }  // ✅ ACTUAL

// NOT number:
{ "role": 3 }  // ❌ WRONG ASSUMPTION!
```

### DevExtreme Special Case
- `DataSourceLoadOptions` uses **camelCase** (special converter)
- Regular DTOs use **PascalCase**
- This is BY DESIGN - don't "fix" it

## 🐛 DEBUGGING METHODOLOGY

### The Right Order - ALWAYS
1. **Enable verbose logging** - See what's actually happening
2. **Check middleware pipeline** - Is request reaching app?
3. **Verify authentication** - 401 vs 403?
4. **Confirm routing** - 404 vs 405?
5. **Check controller logic** - Business logic issues
6. **Examine data layer** - Query problems

### Test Environment Requirements
- Environment MUST be **"Testing"** not "Development"
- Serilog uses **"Verbose"** NOT "Trace" for lowest level
- Check BOTH console AND file outputs
- If no logs appear, FIX LOGGING FIRST

### Common Test Pitfalls
- `builder.Configure()` REPLACES pipeline - use `IStartupFilter`
- Test auth schemes must be registered PROPERLY
- DbContext in tests needs transaction management
- DTOs may stringify enums - test with ACTUAL responses

## ❌ WHAT I ABSOLUTELY HATE

1. **Breaking encapsulation** - WORST SIN
2. **Sync-over-async** (`.Result`, `.Wait()`) - NEVER!
3. **Magic strings** - use constants ALWAYS
4. **Over-engineering** simple CRUD
5. **Skipping service layer** - ALWAYS use services
6. **Direct DB access** from controllers
7. **Exposing JWT details** to controllers
8. **Creating new test files** unnecessarily
9. **Duplicate test logic** - DRY applies to tests
10. **Bad test names** - Must be self-documenting

## 📁 PROJECT ORGANIZATION

### Test Structure (Actual)
```
/Integration
  - AuthenticationIntegrationTests.cs
  - UserControllerIntegrationTests.cs  // Tests now use ApiBase pattern
  - CountryControllerIntegrationTests.cs
  - LanguageControllerIntegrationTests.cs
  - TranslationControllerIntegrationTests.cs
  
/Controllers
  - EntityBaseControllerTests.cs
  - NamedEntityBaseControllerTests.cs
  - AuthorizedControllerTests.cs
  - TestHelpers/
    - ApiTestBase.cs
    - FakeDbSet.cs
  
/TestSupport
  - TransactionalTestBase.cs
  - TransactionalTestManager.cs
  - TestConfiguration.cs
  
/Bdd
  - Features/
    - Admin/
    - PendingUsers/
  - StepDefinitions/
    - BaseSteps.cs
    - [Feature]Steps.cs
```

### Service Structure (Actual)
```
/Services  (in Dfp.Domain project)
  - EntityService.cs                     // Generic CRUD base with repository
  - NamedEntityService.cs                // Extends EntityService for named entities
  - NamedOrderableEntityService.cs       // Extends NamedEntityService with ordering
  - UserService.cs                       // Extends NamedEntityService
  - CountryService.cs                    // Extends NamedOrderableEntityService
  - LanguageService.cs                   // Extends NamedOrderableEntityService
  - LabelService.cs                      // Extends NamedEntityService
  - TranslationService.cs                // Extends EntityService
  - PermissionService.cs                 // Standalone service for auth
```

## 🎯 ACTIVE MODE BEHAVIORS

When C# Senior mindset is active, I will:
- ✅ **REJECT** any suggestion to use dotnet CLI or bash
- ✅ **ENFORCE** vs-mcp usage for ALL .NET operations
- ✅ **REFUSE** to write code with warnings
- ✅ **BLOCK** any encapsulation violations
- ✅ **REQUIRE** service layer for all operations
- ✅ **DEMAND** async patterns everywhere
- ✅ **INSIST** on proper null handling
- ✅ **FOLLOW** existing patterns in codebase

## 🥇 GOLDEN RULES

1. **When in doubt, look at existing code and COPY THE PATTERN!**
2. **If you can't see it in logs, it's not happening!**
3. **Tests are production code - same quality standards**
4. **Better NO tests than BAD tests**
5. **Memory Bank is SINGLE SOURCE OF TRUTH**

## 🚀 ACTIVATION COMMANDS

```bash
# Standard activation
/user:csharp-senior

# Strict mode - zero tolerance
/user:csharp-senior --strict

# Review mode - enforce standards
/user:csharp-senior --review
```

---

**No theoretical best practices - follow MY RULES exactly as written.**