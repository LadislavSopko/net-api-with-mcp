# TypeScript npm Workspaces Overlay

**This is an OVERLAY that builds on typescript-senior.md base mindset**

## 🚨 NPM WORKSPACES MANDATORY RULES

### 1. Workspace Commands - ABSOLUTE REQUIREMENTS
- ❌ **NEVER use global npm commands** in workspace root
- ❌ **NEVER use cd to navigate** between packages
- ✅ **ONLY use workspace-aware commands** from root
- ✅ **ALWAYS use --workspace flag** for specific packages

```bash
# ✅ CORRECT - Workspace commands from root
npm run build --workspaces
npm run test --workspaces --if-present
npm run lint --workspace=@mermaid-sync/core
npm install lodash --workspace=mermaid-editor-v2

# ❌ WRONG - Manual navigation
cd mermaid-sync && npm run build    # NEVER!
cd ../mermaid-model && npm test     # ABSOLUTELY NOT!
```

### 2. Package Management Rules
- ❌ **NEVER install dependencies** in individual packages
- ❌ **NEVER run npm install** inside workspace packages  
- ✅ **ALWAYS install from root** using workspace flags
- ✅ **SHARED dependencies go in root** package.json

```bash
# ✅ CORRECT - From workspace root
npm install @types/node --save-dev                    # Shared dev dependency
npm install mermaid --workspace=mermaid-editor-v2     # Package-specific
npm install --workspaces                              # Install all

# ❌ WRONG - Inside packages
cd mermaid-sync && npm install lodash   # FORBIDDEN!
```

## 📦 WORKSPACE STRUCTURE PATTERNS

### Root package.json Structure
```json
{
  "name": "project-monorepo",
  "private": true,
  "workspaces": [
    "packages/*",
    "apps/*"
  ],
  "scripts": {
    "build": "npm run build --workspaces --if-present",
    "test": "npm run test --workspaces --if-present", 
    "lint": "npm run lint --workspaces --if-present",
    "clean": "npm run clean --workspaces --if-present",
    "btlt": "npm run build && npm run lint && npm run test"
  },
  "devDependencies": {
    "typescript": "^5.8.3",
    "@types/node": "^20.0.0",
    "vitest": "^1.0.0",
    "eslint": "^8.0.0"
  }
}
```

### Package Structure
```json
// Individual package package.json
{
  "name": "@project/core",
  "version": "1.0.0",
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
  },
  "scripts": {
    "build": "tsc -b",
    "test": "vitest run",
    "lint": "eslint src --ext .ts",
    "clean": "rimraf dist"
  },
  "dependencies": {
    // Only package-specific dependencies
  }
}
```

## 🔧 TYPESCRIPT COMPOSITE BUILD SETUP

### Root tsconfig.json
```json
{
  "files": [],
  "references": [
    { "path": "./packages/parser" },
    { "path": "./packages/model" },
    { "path": "./packages/sync" },
    { "path": "./apps/editor" }
  ],
  "compilerOptions": {
    "composite": true,
    "declaration": true,
    "declarationMap": true
  }
}
```

### Package tsconfig.json
```json
{
  "extends": "../../tsconfig.base.json",
  "compilerOptions": {
    "composite": true,
    "rootDir": "./src",
    "outDir": "./dist/types",
    "declarationDir": "./dist/types"
  },
  "references": [
    { "path": "../model" }  // Internal workspace dependency
  ],
  "include": ["src/**/*"],
  "exclude": ["dist", "node_modules", "**/*.test.ts"]
}
```

## 🔗 INTER-PACKAGE DEPENDENCIES

### Workspace Dependencies
```json
// In package.json
{
  "dependencies": {
    "@project/model": "*",      // Workspace version
    "@project/parser": "^1.0.0" // Specific version
  }
}
```

### TypeScript Path Mapping (for development)
```json
// tsconfig.base.json
{
  "compilerOptions": {
    "paths": {
      "@project/model": ["./packages/model/src"],
      "@project/parser": ["./packages/parser/src"],
      "@project/sync": ["./packages/sync/src"]
    }
  }
}
```

## 🧪 WORKSPACE TESTING PATTERNS

### Vitest Workspace Configuration
```typescript
// vitest.workspace.ts
import { defineWorkspace } from 'vitest/config';

export default defineWorkspace([
  './packages/*/vitest.config.ts',
  './apps/*/vitest.config.ts'
]);
```

### Package Test Configuration
```typescript
// packages/core/vitest.config.ts
import { defineConfig } from 'vitest/config';

export default defineConfig({
  test: {
    environment: 'node',
    globals: true,
    coverage: {
      reporter: ['text', 'json', 'html'],
      exclude: ['dist/**', '**/*.test.ts']
    }
  },
  resolve: {
    alias: {
      '@project/model': '../model/src'
    }
  }
});
```

## 🚀 BUILD AND DEPLOYMENT

### Build Order Management
```bash
# ✅ CORRECT - TypeScript composite handles order
npm run build  # Builds in dependency order automatically

# Package-specific builds
npm run build --workspace=@project/model
npm run build --workspace=@project/sync  # Will build model first if needed
```

### Publishing Workflow
```bash
# Build all packages
npm run build --workspaces

# Test all packages  
npm run test --workspaces

# Publish specific package
npm publish --workspace=@project/core

# Publish all changed packages
npx lerna publish  # If using lerna for versioning
```

## 📋 WORKSPACE SCRIPTS PATTERNS

### Conditional Script Execution
```json
{
  "scripts": {
    "test": "npm run test --workspaces --if-present",
    "test:unit": "npm run test:unit --workspaces --if-present",
    "test:integration": "npm run test:integration --workspaces --if-present",
    "build:libs": "npm run build --workspace=@project/model --workspace=@project/sync",
    "dev": "npm run dev --workspace=editor-app"
  }
}
```

### Parallel Execution
```bash
# Run tests in parallel (npm 7+)
npm run test --workspaces

# Sequential execution when needed
npm run build --workspaces --serial
```

## 🔍 DEBUGGING AND DEVELOPMENT

### Development Workflow
```bash
# Install new dependency to specific package
npm install lodash --workspace=@project/core

# Remove dependency from specific package  
npm uninstall lodash --workspace=@project/core

# Update all workspace dependencies
npm update --workspaces

# Check workspace structure
npm ls --workspaces
```

### Link Local Development
```bash
# Automatic linking via workspaces (preferred)
# Dependencies with "*" version automatically link to local

# Manual linking if needed
npm link --workspace=@project/core
npm link @project/core --workspace=editor-app
```

## 🚫 NPM WORKSPACES DON'Ts

- ❌ Don't use yarn in npm workspace projects
- ❌ Don't mix package managers (npm + pnpm)
- ❌ Don't install dependencies inside workspace packages
- ❌ Don't use relative imports between packages (use package names)
- ❌ Don't publish workspace root (keep "private": true)
- ❌ Don't use different TypeScript versions across packages
- ❌ Don't create circular dependencies between packages

## 📊 WORKSPACE COMMANDS REFERENCE

```bash
# Package Management
npm install                                    # Install all workspace deps
npm install --workspaces                      # Explicit workspace install
npm install <pkg> --workspace=<name>         # Add dependency to workspace
npm uninstall <pkg> --workspace=<name>       # Remove from workspace

# Script Execution  
npm run <script> --workspaces                # Run in all workspaces
npm run <script> --workspaces --if-present  # Skip if script missing
npm run <script> --workspace=<name>         # Run in specific workspace

# Information
npm ls --workspaces                          # List all workspace packages
npm ls --workspace=<name>                    # List specific workspace deps
npm run --workspaces                         # List available scripts

# Build & Test
npm run build --workspaces                   # Build all packages
npm run test --workspaces --if-present      # Test all (skip if no test script)
npm run lint --workspaces                    # Lint all packages
```

This overlay ensures proper npm workspaces usage with TypeScript composite builds and maintains clean inter-package dependencies.