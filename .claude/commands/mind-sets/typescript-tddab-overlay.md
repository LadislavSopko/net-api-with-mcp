# TypeScript TDDAB Overlay

**This is an OVERLAY that builds on typescript-senior.md base mindset**

## 🚨 TDDAB MANDATORY RULES FOR TYPESCRIPT

### 1. Test-First Development - ABSOLUTE REQUIREMENTS
- ❌ **NEVER write implementation code** before tests
- ❌ **NEVER skip the RED phase** (failing tests)
- ❌ **NEVER write multiple features** in one TDDAB
- ✅ **ALWAYS write FAILING tests first** (RED)
- ✅ **THEN write minimal code** to make tests pass (GREEN)
- ✅ **ALWAYS verify atomic deployment** works (VERIFY)

```typescript
// ✅ CORRECT - Test First (RED Phase)
describe('UserService', () => {
  it('should create user with valid data', async () => {
    // Arrange
    const userData = { name: 'John', email: 'john@example.com' };
    
    // Act
    const result = await userService.createUser(userData);
    
    // Assert
    expect(result).toEqual({
      id: expect.any(String),
      name: 'John',
      email: 'john@example.com',
      createdAt: expect.any(Date)
    });
  });
});

// Then implement UserService to make test pass (GREEN Phase)
```

### 2. Atomic Block Structure - NON-NEGOTIABLE
```typescript
// ✅ CORRECT - Single responsibility TDDAB
// TDDAB: User Registration Feature
export class UserRegistrationService {
  async registerUser(userData: UserRegistrationData): Promise<User> {
    // Implementation focused on ONLY user registration
  }
}

// ❌ WRONG - Multiple responsibilities
export class UserService {
  async registerUser(userData: UserRegistrationData): Promise<User> { }
  async loginUser(credentials: LoginData): Promise<AuthResult> { }  // Different TDDAB!
  async updateProfile(userId: string, data: ProfileData): Promise<User> { }  // Different TDDAB!
}
```

## 🧪 VITEST TDDAB TESTING PATTERNS

### RED Phase - Failing Tests First
```typescript
import { describe, it, expect, beforeEach, vi } from 'vitest';

// TDDAB: Email Validation Service
describe('EmailValidationService', () => {
  let emailValidator: EmailValidationService;
  
  beforeEach(() => {
    emailValidator = new EmailValidationService();
  });
  
  // RED Phase - These tests will FAIL initially
  describe('validateEmail', () => {
    it('should return true for valid email format', () => {
      // This will fail until we implement validateEmail
      expect(emailValidator.validateEmail('user@example.com')).toBe(true);
    });
    
    it('should return false for invalid email format', () => {
      expect(emailValidator.validateEmail('invalid-email')).toBe(false);
    });
    
    it('should return false for empty email', () => {
      expect(emailValidator.validateEmail('')).toBe(false);
    });
    
    it('should return false for null email', () => {
      expect(emailValidator.validateEmail(null as any)).toBe(false);
    });
  });
});

// Run tests: npm run test
// Expected: ALL TESTS FAIL (RED Phase confirmed)
```

### GREEN Phase - Minimal Implementation
```typescript
// After RED phase confirmed, write minimal code to pass tests
export class EmailValidationService {
  validateEmail(email: string): boolean {
    if (!email || typeof email !== 'string') {
      return false;
    }
    
    const emailRegex = /^[^\s@]+@[^\s@]+\.[^\s@]+$/;
    return emailRegex.test(email);
  }
}

// Run tests: npm run test
// Expected: ALL TESTS PASS (GREEN Phase confirmed)
```

### VERIFY Phase - Integration Testing
```typescript
// Verify atomic deployment with integration tests
describe('EmailValidationService Integration', () => {
  it('should integrate with user registration flow', async () => {
    const emailValidator = new EmailValidationService();
    const userService = new UserRegistrationService(emailValidator);
    
    const validUser = {
      name: 'John Doe',
      email: 'john@example.com'
    };
    
    // Should not throw error with valid email
    await expect(userService.registerUser(validUser)).resolves.toBeTruthy();
    
    const invalidUser = {
      name: 'Jane Doe', 
      email: 'invalid-email'
    };
    
    // Should reject with invalid email
    await expect(userService.registerUser(invalidUser)).rejects.toThrow('Invalid email format');
  });
});
```

## 📦 TDDAB PLANNING STRUCTURE

### TDDAB Block Definition
```typescript
/**
 * TDDAB: User Authentication Service
 * 
 * Scope: Handle user login/logout operations
 * Dependencies: UserRepository, TokenService, HashingService
 * Exports: AuthenticationService, AuthResult interface
 * 
 * Test Coverage: 100% - All paths tested
 * Atomic: Can be deployed independently
 */
```

### TDDAB Implementation Pattern
```typescript
// 1. Define interfaces first (contract-driven)
export interface AuthenticationService {
  login(credentials: LoginCredentials): Promise<AuthResult>;
  logout(token: string): Promise<void>;
  validateToken(token: string): Promise<boolean>;
}

export interface AuthResult {
  success: boolean;
  token?: string;
  user?: User;
  error?: string;
}

export interface LoginCredentials {
  email: string;
  password: string;
}

// 2. Write comprehensive tests (RED Phase)
// 3. Implement to pass tests (GREEN Phase) 
// 4. Verify integration works (VERIFY Phase)
```

## 🔄 TDDAB WORKFLOW COMMANDS

### Development Workflow
```bash
# 1. RED Phase - Create failing tests
npm run test -- --watch AuthenticationService
# Expected: Tests fail

# 2. GREEN Phase - Implement to pass tests
npm run test -- --coverage AuthenticationService
# Expected: 100% test coverage, all tests pass

# 3. VERIFY Phase - Integration test
npm run test:integration
npm run build
# Expected: Clean build, integration tests pass

# 4. BTLT Verification
npm run btlt  # Build, TypeCheck, Lint, Test
# Expected: All operations succeed
```

### TDDAB Completion Checklist
```bash
# Before marking TDDAB complete:
□ npm run test -- --coverage    # 100% coverage required
□ npm run lint                  # Zero warnings
□ npm run build                 # Clean build
□ npm run test:integration      # Integration tests pass
□ npm run btlt                  # Full pipeline success
```

## 🧩 TDDAB ISOLATION PATTERNS

### Dependency Injection for Testing
```typescript
// ✅ CORRECT - Testable with dependency injection
export class UserRegistrationService {
  constructor(
    private emailValidator: EmailValidationService,
    private userRepository: UserRepository,
    private logger: Logger
  ) {}
  
  async registerUser(userData: UserData): Promise<User> {
    if (!this.emailValidator.validateEmail(userData.email)) {
      throw new ValidationError('Invalid email format');
    }
    
    return this.userRepository.create(userData);
  }
}

// Test with mocked dependencies
describe('UserRegistrationService', () => {
  let mockEmailValidator: EmailValidationService;
  let mockUserRepository: UserRepository;
  let mockLogger: Logger;
  let service: UserRegistrationService;
  
  beforeEach(() => {
    mockEmailValidator = { validateEmail: vi.fn() };
    mockUserRepository = { create: vi.fn() };
    mockLogger = { error: vi.fn(), info: vi.fn() };
    
    service = new UserRegistrationService(
      mockEmailValidator,
      mockUserRepository, 
      mockLogger
    );
  });
});
```

### Mock Management
```typescript
// ✅ CORRECT - Isolated mocking per TDDAB
const createMockUserRepository = (): jest.Mocked<UserRepository> => ({
  create: vi.fn(),
  findById: vi.fn(),
  update: vi.fn(),
  delete: vi.fn()
});

const createMockEmailValidator = (): jest.Mocked<EmailValidationService> => ({
  validateEmail: vi.fn()
});

// Use factory functions for consistent mocking
beforeEach(() => {
  mockUserRepository = createMockUserRepository();
  mockEmailValidator = createMockEmailValidator();
});
```

## 📊 TDDAB METRICS AND VERIFICATION

### Coverage Requirements
```json
// vitest.config.ts
export default defineConfig({
  test: {
    coverage: {
      provider: 'v8',
      reporter: ['text', 'json', 'html'],
      thresholds: {
        global: {
          branches: 100,    // TDDAB requires 100%
          functions: 100,   // TDDAB requires 100%
          lines: 100,       // TDDAB requires 100%
          statements: 100   // TDDAB requires 100%
        }
      },
      exclude: [
        'dist/**',
        'coverage/**',
        '**/*.config.ts',
        '**/*.test.ts'
      ]
    }
  }
});
```

### TDDAB Verification Script
```typescript
// scripts/verify-tddab.ts
import { execSync } from 'child_process';

async function verifyTDDAB(blockName: string): Promise<boolean> {
  try {
    // RED Phase verification
    console.log('🔴 Verifying RED Phase...');
    const testOutput = execSync('npm run test', { encoding: 'utf8' });
    
    // GREEN Phase verification  
    console.log('🟢 Verifying GREEN Phase...');
    const coverageOutput = execSync('npm run test -- --coverage', { encoding: 'utf8' });
    
    if (!coverageOutput.includes('100%')) {
      throw new Error('Coverage must be 100% for TDDAB');
    }
    
    // VERIFY Phase verification
    console.log('✅ Verifying VERIFY Phase...');
    execSync('npm run build');
    execSync('npm run lint');
    
    console.log(`✅ TDDAB ${blockName} verified successfully!`);
    return true;
    
  } catch (error) {
    console.error(`❌ TDDAB ${blockName} verification failed:`, error);
    return false;
  }
}
```

## 🚫 TDDAB DON'Ts FOR TYPESCRIPT

- ❌ Don't implement before writing failing tests
- ❌ Don't write tests that pass immediately 
- ❌ Don't skip coverage requirements (100%)
- ❌ Don't mix multiple features in one TDDAB
- ❌ Don't use any types (breaks test confidence)
- ❌ Don't bypass TypeScript strict mode
- ❌ Don't ignore integration testing
- ❌ Don't deploy without BTLT verification
- ❌ Don't use console.log in TDDAB code
- ❌ Don't create untestable code patterns

## 📋 TDDAB TYPESCRIPT COMMANDS

```bash
# TDDAB Development Cycle
npm run test:watch                    # RED Phase - watch failing tests
npm run test -- --coverage           # GREEN Phase - verify 100% coverage  
npm run test:integration             # VERIFY Phase - integration tests
npm run btlt                         # Complete verification

# TDDAB Quality Gates
npm run test -- --reporter=verbose   # Detailed test output
npm run lint -- --max-warnings=0    # Zero warnings required
npm run build -- --noEmitOnError    # Fail on TypeScript errors
npm run type-check                   # Strict TypeScript validation

# TDDAB Specific Testing
npm run test -- --testNamePattern="TDDAB.*UserAuth"  # Test specific TDDAB
npm run test -- --coverage --coverageReporters=text  # Coverage summary
```

This overlay ensures proper TDDAB methodology with TypeScript, maintaining atomic development blocks with comprehensive test coverage and verification.