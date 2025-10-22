#!/bin/bash
set -e

# Function to check if database exists
database_exists() {
    psql -U "$POSTGRES_USER" -d postgres -tAc "SELECT 1 FROM pg_database WHERE datname = '$1'" | grep -q 1
}

# Create keycloak database if it doesn't exist
if ! database_exists "keycloak"; then
    echo "Creating keycloak database..."
    psql -U "$POSTGRES_USER" -d postgres <<-EOSQL
        CREATE DATABASE keycloak;
        GRANT ALL PRIVILEGES ON DATABASE keycloak TO $POSTGRES_USER;
EOSQL
    echo "Keycloak database created successfully."
else
    echo "Keycloak database already exists."
fi