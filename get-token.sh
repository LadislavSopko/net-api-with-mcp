#!/bin/bash

# Get token from Keycloak for testing
# Usage: ./get-token.sh admin admin123

USERNAME=${1:-admin}
PASSWORD=${2:-admin123}

TOKEN_RESPONSE=$(curl -s -X POST \
  http://127.0.0.1:8080/realms/mcppoc-realm/protocol/openid-connect/token \
  -H "Content-Type: application/x-www-form-urlencoded" \
  -d "client_id=mcppoc-api" \
  -d "grant_type=password" \
  -d "username=$USERNAME" \
  -d "password=$PASSWORD")

echo "$TOKEN_RESPONSE" | grep -o '"access_token":"[^"]*"' | cut -d'"' -f4
