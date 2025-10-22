# Docker Setup for MCP POC

This directory contains Docker configuration for the MCP POC development environment.

## Quick Start

1. Copy the example environment file:
   ```bash
   cp .env.example .env
   ```

2. Update `.env` with your desired PostgreSQL password (keep default values for development)

3. Start all services:
   ```bash
   docker-compose up -d
   ```

4. Check service health:
   ```bash
   docker-compose ps
   ```

5. Access the services:
   - API: http://localhost:5001
   - MCP Endpoint: http://localhost:5001/mcp
   - Swagger UI: http://localhost:5001/swagger
   - Keycloak Admin: http://localhost:8080/admin

## Services

### PostgreSQL
- **Image**: postgres:16-alpine
- **Container**: mcppoc_postgres
- **Port**: 5432 (default)
- **Database**: mcppoc_db
- **User**: mcppoc_user
- **Data**: Persisted in `mcppoc_postgres_data` volume

### Keycloak
- **Image**: quay.io/keycloak/keycloak:25.0.2
- **Container**: mcppoc_keycloak
- **Port**: 8080 (default)
- **Admin Console**: http://localhost:8080/admin
- **Admin User**: admin / admin
- **Database**: Uses PostgreSQL (keycloak database)
- **Realm**: mcppoc-realm (auto-imported)
- **Client**: mcppoc-api (configured for API)
- **Test Users**:
  - admin@mcppoc.com / admin123 (admin role)
  - user@mcppoc.com / user123 (user role)

### MCP POC API
- **Image**: Built from Dockerfile
- **Container**: mcppoc_api
- **Port**: 5001 (default)
- **HTTP API**: http://localhost:5001/api/users
- **MCP Endpoint**: http://localhost:5001/mcp
- **Swagger**: http://localhost:5001/swagger
- **Database**: Uses PostgreSQL (mcppoc_db)
- **Authentication**: Keycloak (JWT Bearer)

## Common Commands

```bash
# Start all services
docker-compose up -d

# Stop all services
docker-compose down

# View logs
docker-compose logs -f api
docker-compose logs -f postgres
docker-compose logs -f keycloak

# Access PostgreSQL CLI
docker-compose exec postgres psql -U mcppoc_user -d mcppoc_db

# Rebuild API container
docker-compose up -d --build api

# Backup database
docker-compose exec postgres pg_dump -U mcppoc_user mcppoc_db > backup.sql

# Restore database
docker-compose exec postgres psql -U mcppoc_user mcppoc_db < backup.sql

# Remove everything (including volumes)
docker-compose down -v
```

## Connection String

For .NET applications (localhost development):
```
Host=localhost;Port=5432;Database=mcppoc_db;Username=mcppoc_user;Password=<your_password>
```

For Docker services (container-to-container):
```
Host=postgres;Port=5432;Database=mcppoc_db;Username=mcppoc_user;Password=<your_password>
```

## Custom PostgreSQL Configuration

Place any custom PostgreSQL initialization scripts in `./postgres/` directory. They will be executed automatically when the container is first created.

## Keycloak Setup

The Keycloak realm configuration is automatically imported from `./keycloak/mcppoc-realm.json`.

To export realm configuration:
```bash
docker-compose exec keycloak /opt/keycloak/bin/kc.sh export --dir /tmp --realm mcppoc-realm
docker cp mcppoc_keycloak:/tmp/mcppoc-realm.json ./keycloak/
```

## Development Workflow

1. **Local Development** (without Docker):
   - Start only infrastructure: `docker-compose up -d postgres keycloak`
   - Run API locally: `dotnet run --project src/McpPoc.Api`
   - Use localhost connection string

2. **Full Docker Development**:
   - Start everything: `docker-compose up -d`
   - API runs in container
   - All services communicate via Docker network

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
│  - mcppoc_db (application data)     │
│  - keycloak (keycloak data)         │
└─────────────────────────────────────┘
```