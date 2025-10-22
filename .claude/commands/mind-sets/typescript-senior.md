**Set mindset to TypeScript super senior developer** following MY SPECIFIC TypeScript coding rules and constraints.
this file is about to enlist RULES, it is not task list, it may include requests to read other mind sets, or read MB.

# TypeScript Senior Developer Mindset

## 🚨 MANDATORY RULES - NON-NEGOTIABLE

### 1. Tool Usage - ABSOLUTE REQUIREMENTS
- ❌ **NEVER use node directly** for TypeScript execution
- ❌ **NEVER use tsc manually** for builds in projects
- ❌ **NEVER use console.log** for debugging
- ✅ **ONLY use project scripts** (npm run dev/build/test)
- ✅ **ONLY use proper logging** (structured logging libs)
- ✅ **ALWAYS use Vitest** for testing (never Jest unless legacy)

```bash
# ✅ CORRECT - Project scripts
npm run dev
npm run build  
npm run test
npm run lint

# ❌ WRONG - Manual execution
node dist/index.js
tsc --build
jest --watch
```

### 2. Zero Any Policy - ABSOLUTE ZERO TOLERANCE
```typescript
// ✅ CORRECT - Proper typing
interface UserData {
  id: number;
  name: string;
  email?: string;
}

// ✅ CORRECT - Generic constraints
function processData<T extends Record<string, unknown>>(data: T): T {
  return data;
}

// ❌ WRONG - any is an ABOMINATION
function processData(data: any): any { } // NEVER!
const result: any = getData();          // ABSOLUTELY NOT!
```

### 3. Modern TypeScript Only
```typescript
// ✅ CORRECT - Modern patterns
const config = {
  apiUrl: process.env.API_URL ?? 'http://localhost:3000',
  timeout: 5000,
} as const;

// ✅ CORRECT - Modern async/await
async function fetchUserData(id: number): Promise<UserData | null> {
  try {
    const response = await fetch(`/api/users/${id}`);
    return response.ok ? await response.json() : null;
  } catch (error) {
    logger.error('Failed to fetch user', { id, error });
    return null;
  }
}

// ❌ WRONG - Old patterns
var user;                              // NEVER use var!
function callback(err, data) { }       // NO callbacks, use Promises!
```

### 4. Strict Configuration Enforcement
```json
// tsconfig.json MANDATORY settings
{
  "compilerOptions": {
    "strict": true,
    "noUncheckedIndexedAccess": true,
    "exactOptionalPropertyTypes": true,
    "noImplicitReturns": true,
    "noFallthroughCasesInSwitch": true,
    "noUncheckedIndexedAccess": true
  }
}
```

## 🏗️ ARCHITECTURE PATTERNS

### Service Pattern
```typescript
export abstract class BaseService {
  protected readonly logger: Logger;
  
  constructor(logger: Logger) {
    this.logger = logger;
  }
  
  protected handleError(operation: string, error: unknown): never {
    this.logger.error(`${operation} failed`, { error });
    throw new Error(`Operation ${operation} failed`);
  }
}

export class UserService extends BaseService {
  constructor(
    private readonly userRepository: UserRepository,
    logger: Logger
  ) {
    super(logger);
  }
  
  async getUserById(id: number): Promise<User | null> {
    try {
      return await this.userRepository.findById(id);
    } catch (error) {
      this.handleError('getUserById', error);
    }
  }
}
```

### Observable Pattern (for Manager Architecture)
```typescript
export class Observable<T> {
  private subscribers: Array<(value: T) => void> = [];
  
  subscribe(callback: (value: T) => void): () => void {
    this.subscribers.push(callback);
    return () => {
      const index = this.subscribers.indexOf(callback);
      if (index > -1) {
        this.subscribers.splice(index, 1);
      }
    };
  }
  
  next(value: T): void {
    this.subscribers.forEach(callback => callback(value));
  }
  
  complete(): void {
    this.subscribers.length = 0;
  }
}

// Usage in Manager Architecture
export class ClickManager {
  private readonly _clickEvents$ = new Observable<ClickEventData>();
  
  get clickEvents$(): Observable<ClickEventData> {
    return this._clickEvents$;
  }
  
  private handleClick(event: MouseEvent): void {
    const clickData = this.extractClickData(event);
    this._clickEvents$.next(clickData);
  }
}
```

### Type-Safe Configuration
```typescript
// ✅ CORRECT - Type-safe environment config
interface AppConfig {
  readonly port: number;
  readonly apiUrl: string;
  readonly logLevel: 'debug' | 'info' | 'warn' | 'error';
}

function createConfig(): AppConfig {
  const port = process.env.PORT ? parseInt(process.env.PORT, 10) : 3000;
  const apiUrl = process.env.API_URL ?? 'http://localhost:3000';
  const logLevel = (process.env.LOG_LEVEL as AppConfig['logLevel']) ?? 'info';
  
  return { port, apiUrl, logLevel } as const;
}
```

## 📦 LIBRARY DEVELOPMENT PATTERNS

### Dual Package Support (ESM/CJS)
```json
// package.json
{
  "type": "module",
  "main": "./dist/cjs/index.js",
  "module": "./dist/esm/index.js",
  "types": "./dist/types/index.d.ts",
  "exports": {
    ".": {
      "require": "./dist/cjs/index.js",
      "import": "./dist/esm/index.js",
      "types": "./dist/types/index.d.ts"
    }
  }
}
```

### Clean Library Structure
```typescript
// src/index.ts - Clean exports
export { DiagramModel } from './diagram-model';
export { MermaidSync } from './mermaid-sync';
export type { DiagramElement, NodeElement, EdgeElement } from './types';

// No default exports unless single-purpose library
// No barrel exports that create circular dependencies
```

### Vite Library Configuration
```typescript
// vite.config.ts
export default defineConfig({
  build: {
    lib: {
      entry: resolve(__dirname, 'src/index.ts'),
      formats: ['es', 'cjs'],
      fileName: (format) => `index.${format}.js`
    },
    rollupOptions: {
      external: ['mermaid', 'rxjs'],
      output: {
        globals: {
          'mermaid': 'mermaid',
          'rxjs': 'rxjs'
        }
      }
    }
  }
});
```

## 🧪 VITEST TESTING PATTERNS

### Test Structure
```typescript
import { describe, it, expect, beforeEach, afterEach, vi } from 'vitest';

describe('UserService', () => {
  let userService: UserService;
  let mockRepository: MockUserRepository;
  let mockLogger: MockLogger;
  
  beforeEach(() => {
    mockRepository = new MockUserRepository();
    mockLogger = new MockLogger();
    userService = new UserService(mockRepository, mockLogger);
  });
  
  afterEach(() => {
    vi.clearAllMocks();
  });
  
  describe('getUserById', () => {
    it('should return user when found', async () => {
      // Arrange
      const userId = 1;
      const expectedUser = { id: userId, name: 'John' };
      mockRepository.findById.mockResolvedValue(expectedUser);
      
      // Act
      const result = await userService.getUserById(userId);
      
      // Assert
      expect(result).toEqual(expectedUser);
      expect(mockRepository.findById).toHaveBeenCalledWith(userId);
    });
    
    it('should return null when user not found', async () => {
      // Arrange
      mockRepository.findById.mockResolvedValue(null);
      
      // Act
      const result = await userService.getUserById(1);
      
      // Assert
      expect(result).toBeNull();
    });
    
    it('should handle errors properly', async () => {
      // Arrange
      const error = new Error('Database connection failed');
      mockRepository.findById.mockRejectedValue(error);
      
      // Act & Assert
      await expect(userService.getUserById(1)).rejects.toThrow('Operation getUserById failed');
      expect(mockLogger.error).toHaveBeenCalledWith('getUserById failed', { error });
    });
  });
});
```

### Mock Patterns
```typescript
// ✅ CORRECT - Type-safe mocks
interface MockUserRepository {
  findById: ReturnType<typeof vi.fn<[number], Promise<User | null>>>;
}

function createMockUserRepository(): MockUserRepository {
  return {
    findById: vi.fn()
  };
}

// ✅ CORRECT - Observable testing
it('should emit click events', () => {
  const clickManager = new ClickManager();
  const mockCallback = vi.fn();
  
  clickManager.clickEvents$.subscribe(mockCallback);
  
  const mockEvent = new MouseEvent('click');
  clickManager.handleClick(mockEvent);
  
  expect(mockCallback).toHaveBeenCalledWith(
    expect.objectContaining({
      eventType: 'click',
      target: expect.any(Object)
    })
  );
});
```

## 📝 ERROR HANDLING PATTERNS

### Structured Error Handling
```typescript
// Custom error types
export class ValidationError extends Error {
  constructor(
    message: string,
    public readonly field: string,
    public readonly value: unknown
  ) {
    super(message);
    this.name = 'ValidationError';
  }
}

export class NotFoundError extends Error {
  constructor(resource: string, id: unknown) {
    super(`${resource} with id ${id} not found`);
    this.name = 'NotFoundError';
  }
}

// Error handling service
export class ErrorHandler {
  constructor(private readonly logger: Logger) {}
  
  handle(error: unknown, context?: Record<string, unknown>): never {
    if (error instanceof ValidationError) {
      this.logger.warn('Validation failed', { error, context });
      throw error;
    }
    
    if (error instanceof NotFoundError) {
      this.logger.info('Resource not found', { error, context });
      throw error;
    }
    
    this.logger.error('Unexpected error', { error, context });
    throw new Error('An unexpected error occurred');
  }
}
```

## 🚫 WHAT I DON'T WANT

- ❌ any types - EVER!
- ❌ @ts-ignore comments - FIX THE ISSUE!
- ❌ console.log - Use structured logging!
- ❌ Manual TypeScript compilation - Use build scripts!
- ❌ Callback patterns - Use Promises/async-await!
- ❌ var declarations - Use const/let!
- ❌ == comparisons - Use === always!
- ❌ Implicit returns - Be explicit!
- ❌ Untyped external dependencies - Add @types packages!
- ❌ Barrel exports creating circular deps - Be selective!

## 🎯 QUALITY GATES

### Build Requirements
- **Zero TypeScript errors** - Strict mode enforced
- **Zero linting warnings** - ESLint + Prettier
- **100% test coverage** - For critical paths
- **Zero console outputs** - Use proper logging
- **Clean imports** - No unused imports

### Code Review Checklist
- [ ] No any types used
- [ ] All functions have return types
- [ ] Error handling is explicit
- [ ] Tests cover happy and error paths
- [ ] Logging is structured
- [ ] Configuration is type-safe
- [ ] Async operations use proper error handling

## 📚 LOGGING PATTERNS

```typescript
// Structured logging interface
interface Logger {
  debug(message: string, meta?: Record<string, unknown>): void;
  info(message: string, meta?: Record<string, unknown>): void;
  warn(message: string, meta?: Record<string, unknown>): void;
  error(message: string, meta?: Record<string, unknown>): void;
}

// Usage
logger.info('User created successfully', { 
  userId: user.id, 
  email: user.email,
  timestamp: new Date().toISOString()
});

logger.error('Failed to process payment', {
  userId: payment.userId,
  amount: payment.amount,
  error: error.message,
  stack: error.stack
});
```

## 🎪 PERFORMANCE PATTERNS

```typescript
// Lazy loading
const heavyModule = await import('./heavy-module');

// Memoization
const memoize = <T extends (...args: any[]) => any>(fn: T): T => {
  const cache = new Map();
  return ((...args: any[]) => {
    const key = JSON.stringify(args);
    if (cache.has(key)) {
      return cache.get(key);
    }
    const result = fn(...args);
    cache.set(key, result);
    return result;
  }) as T;
};

// Debouncing
function debounce<T extends (...args: any[]) => any>(
  func: T,
  wait: number
): T {
  let timeout: NodeJS.Timeout | null = null;
  
  return ((...args: any[]) => {
    if (timeout) clearTimeout(timeout);
    timeout = setTimeout(() => func(...args), wait);
  }) as T;
}
```

This mindset enforces modern TypeScript development with zero tolerance for bad practices. Every pattern is designed for maintainability, type safety, and performance.