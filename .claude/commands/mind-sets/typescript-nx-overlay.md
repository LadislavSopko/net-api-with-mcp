# TypeScript Nx Workspace Overlay

**This is an OVERLAY that builds on typescript-senior.md base mindset**

## 🚨 NX WORKSPACE MANDATORY RULES

### 1. Nx Commands - ABSOLUTE REQUIREMENTS
- ❌ **NEVER use npm/yarn commands** for builds/tests/lints
- ❌ **NEVER use tsc directly** in Nx workspace
- ❌ **NEVER bypass Nx caching** without reason
- ✅ **ONLY use npx nx commands** for all operations
- ✅ **ALWAYS leverage Nx affected** for efficiency

```bash
# ✅ CORRECT - Nx commands
npx nx build my-app
npx nx test my-lib
npx nx lint my-app
npx nx serve my-app

# ✅ CORRECT - Affected commands (performance!)
npx nx affected:build
npx nx affected:test
npx nx affected:lint

# ❌ WRONG - Direct commands
npm run build        # NEVER!
tsc --build          # ABSOLUTELY NOT!
npm test             # FORBIDDEN!
```

### 2. Project Generation Rules
- ❌ **NEVER manually create** projects/libraries
- ❌ **NEVER copy-paste** configurations
- ✅ **ONLY use Nx generators** for consistency
- ✅ **ALWAYS follow Nx conventions** for structure

```bash
# ✅ CORRECT - Nx generators
npx nx generate @nx/js:library my-lib
npx nx generate @nx/angular:application my-app
npx nx generate @nx/js:lib shared-utils --buildable
npx nx generate @nx/js:lib ui-components --publishable

# Generate with specific options
npx nx g @nx/js:lib data-access --directory=libs/shared
```

## 📦 NX WORKSPACE STRUCTURE PATTERNS

### Workspace Configuration
```json
// nx.json
{
  "extends": "nx/presets/npm.json",
  "tasksRunnerOptions": {
    "default": {
      "runner": "nx/tasks-runners/default",
      "options": {
        "cacheableOperations": ["build", "lint", "test", "e2e"]
      }
    }
  },
  "targetDefaults": {
    "build": {
      "dependsOn": ["^build"],
      "inputs": ["production", "^production"]
    },
    "test": {
      "inputs": ["default", "^production", "{workspaceRoot}/jest.preset.js"]
    }
  }
}
```

### Project Structure
```
workspace/
├── apps/
│   ├── web-app/
│   │   ├── src/
│   │   ├── project.json
│   │   └── tsconfig.json
│   └── api/
├── libs/
│   ├── shared/
│   │   ├── data-access/
│   │   ├── ui-components/
│   │   └── utils/
│   └── feature/
│       └── user-management/
├── nx.json
├── package.json
└── tsconfig.base.json
```

## 🎯 PROJECT CONFIGURATION PATTERNS

### Library project.json
```json
{
  "name": "shared-data-access",
  "sourceRoot": "libs/shared/data-access/src",
  "projectType": "library",
  "targets": {
    "build": {
      "executor": "@nx/js:tsc",
      "outputs": ["{options.outputPath}"],
      "options": {
        "outputPath": "dist/libs/shared/data-access",
        "main": "libs/shared/data-access/src/index.ts",
        "tsConfig": "libs/shared/data-access/tsconfig.lib.json",
        "assets": ["libs/shared/data-access/*.md"]
      }
    },
    "test": {
      "executor": "@nx/jest:jest",
      "outputs": ["{workspaceRoot}/coverage/libs/shared/data-access"],
      "options": {
        "jestConfig": "libs/shared/data-access/jest.config.ts",
        "passWithNoTests": true
      }
    },
    "lint": {
      "executor": "@nx/linter:eslint",
      "outputs": ["{options.outputFile}"],
      "options": {
        "lintFilePatterns": ["libs/shared/data-access/**/*.ts"]
      }
    }
  }
}
```

### Application project.json
```json
{
  "name": "web-app",
  "sourceRoot": "apps/web-app/src",
  "projectType": "application",
  "targets": {
    "build": {
      "executor": "@nx/webpack:webpack",
      "outputs": ["{options.outputPath}"],
      "options": {
        "outputPath": "dist/apps/web-app",
        "index": "apps/web-app/src/index.html",
        "main": "apps/web-app/src/main.ts",
        "polyfills": "apps/web-app/src/polyfills.ts",
        "tsConfig": "apps/web-app/tsconfig.app.json"
      }
    },
    "serve": {
      "executor": "@nx/webpack:dev-server",
      "options": {
        "buildTarget": "web-app:build"
      }
    }
  }
}
```

## 🔗 DEPENDENCY MANAGEMENT

### TypeScript Path Mapping
```json
// tsconfig.base.json
{
  "compilerOptions": {
    "paths": {
      "@workspace/shared/data-access": ["libs/shared/data-access/src/index.ts"],
      "@workspace/shared/ui": ["libs/shared/ui/src/index.ts"],
      "@workspace/feature/auth": ["libs/feature/auth/src/index.ts"]
    }
  }
}
```

### Import Restrictions
```json
// .eslintrc.json
{
  "rules": {
    "@nx/enforce-module-boundaries": [
      "error",
      {
        "allow": [],
        "depConstraints": [
          {
            "sourceTag": "scope:shared",
            "onlyDependOnLibsWithTags": ["scope:shared"]
          },
          {
            "sourceTag": "type:feature", 
            "onlyDependOnLibsWithTags": ["type:feature", "type:ui", "type:util", "type:data-access"]
          }
        ]
      }
    ]
  }
}
```

## 🏷️ TAGGING STRATEGY

### Project Tags
```json
// libs/shared/ui/project.json
{
  "tags": ["scope:shared", "type:ui"],
}

// libs/feature/user/project.json  
{
  "tags": ["scope:user", "type:feature"],
}

// apps/admin-app/project.json
{
  "tags": ["scope:admin", "type:app"],
}
```

### Tag-Based Commands
```bash
# Build only shared libraries
npx nx run-many --target=build --projects=tag:scope:shared

# Test all feature libraries
npx nx run-many --target=test --projects=tag:type:feature

# Lint UI components
npx nx run-many --target=lint --projects=tag:type:ui
```

## 🧪 TESTING PATTERNS

### Jest Configuration
```typescript
// jest.preset.js
const { getJestProjects } = require('@nx/jest');

module.exports = {
  projects: getJestProjects()
};
```

### Library Jest Config
```typescript
// libs/shared/utils/jest.config.ts
export default {
  displayName: 'shared-utils',
  preset: '../../../jest.preset.js',
  testEnvironment: 'node',
  transform: {
    '^.+\\.[tj]s$': ['ts-jest', { tsconfig: '<rootDir>/tsconfig.spec.json' }],
  },
  moduleFileExtensions: ['ts', 'js', 'html'],
  coverageDirectory: '../../../coverage/libs/shared/utils',
  coverageReporters: ['html', 'lcov', 'text'],
  coverageThreshold: {
    global: {
      branches: 80,
      functions: 80, 
      lines: 80,
      statements: 80
    }
  }
};
```

## 🚀 BUILD AND DEPLOYMENT

### Buildable Libraries
```bash
# Generate buildable library
npx nx generate @nx/js:library my-lib --buildable

# Build library independently  
npx nx build my-lib

# Build all buildable libraries
npx nx run-many --target=build --projects=tag:type:lib
```

### Publishable Libraries
```bash
# Generate publishable library
npx nx generate @nx/js:library my-public-lib --publishable --importPath=@company/my-lib

# Build for publishing
npx nx build my-public-lib

# Publish to npm
npm publish dist/libs/my-public-lib
```

### Affected Builds
```bash
# Build only what changed
npx nx affected:build

# Build affected since specific commit
npx nx affected:build --base=origin/main

# Build affected with specific configuration
npx nx affected:build --configuration=production
```

## 📊 NX CACHING AND PERFORMANCE

### Cache Configuration
```json
// nx.json
{
  "tasksRunnerOptions": {
    "default": {
      "runner": "nx/tasks-runners/default",
      "options": {
        "cacheableOperations": ["build", "lint", "test", "e2e"],
        "parallel": 3,
        "cacheDirectory": "tmp/nx-cache"
      }
    }
  }
}
```

### Distributed Task Execution
```bash
# Enable Nx Cloud for distributed caching
npx nx connect-to-nx-cloud

# Run with distributed execution
npx nx affected:build --parallel --maxParallel=8
```

## 🔧 WORKSPACE MAINTENANCE

### Dependency Updates
```bash
# Update Nx
npx nx migrate @nx/workspace@latest

# Apply migrations
npx nx migrate --run-migrations

# Update specific plugin
npx nx migrate @nx/angular@latest
```

### Workspace Analysis
```bash
# Visualize project dependencies
npx nx graph

# Show affected projects
npx nx affected --target=build --dry-run

# Workspace information
npx nx list
npx nx show project my-app
```

## 📋 NX COMMANDS REFERENCE

```bash
# Project Generation
npx nx generate @nx/js:library <name>              # Generate library
npx nx generate @nx/js:application <name>          # Generate application
npx nx generate @nx/workspace:move --project=<name> --destination=<path>

# Build & Test
npx nx build <project>                             # Build specific project
npx nx test <project>                              # Test specific project
npx nx lint <project>                              # Lint specific project
npx nx serve <project>                             # Serve application

# Multiple Projects
npx nx run-many --target=build --projects=app1,app2
npx nx run-many --target=test --all               # Run target for all projects
npx nx run-many --target=lint --projects=tag:scope:shared

# Affected Commands
npx nx affected:build                              # Build affected projects
npx nx affected:test                               # Test affected projects
npx nx affected --target=build --base=main~1      # Compare to specific base

# Workspace
npx nx graph                                       # Visualize dependencies
npx nx list                                        # List installed plugins
npx nx show project <name>                         # Show project details
npx nx format:check                                # Check formatting
npx nx format:write                                # Fix formatting
```

## 🚫 NX DON'Ts

- ❌ Don't use npm scripts for builds in Nx workspace
- ❌ Don't create projects manually (use generators)
- ❌ Don't ignore dependency boundaries
- ❌ Don't skip tagging strategy
- ❌ Don't bypass Nx caching without good reason
- ❌ Don't use relative imports between projects
- ❌ Don't mix different testing frameworks without reason
- ❌ Don't ignore affected commands for performance

This overlay ensures efficient Nx workspace usage with proper project organization, dependency management, and build optimization.