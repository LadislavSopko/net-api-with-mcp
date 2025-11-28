#!/bin/bash
# Publish Zero.Mcp.Extensions to NuGet.org
# Usage: ./publish-nuget.sh

set -e

SCRIPT_DIR="$(cd "$(dirname "$0")" && pwd)"
API_KEY_FILE="$SCRIPT_DIR/.nuget-api-key"
NUPKG_DIR="$SCRIPT_DIR/nupkg"
PROJECT="$SCRIPT_DIR/src/Zero.Mcp.Extensions/Zero.Mcp.Extensions.csproj"

# Check API key file exists
if [ ! -f "$API_KEY_FILE" ]; then
    echo "Error: API key file not found: $API_KEY_FILE"
    echo "Create the file with your NuGet.org API key"
    exit 1
fi

API_KEY=$(cat "$API_KEY_FILE" | tr -d '[:space:]')

if [ -z "$API_KEY" ]; then
    echo "Error: API key file is empty"
    exit 1
fi

# Clean and pack
echo "Building and packing..."
rm -rf "$NUPKG_DIR"
dotnet pack "$PROJECT" -c Release -o "$NUPKG_DIR"

# Find the package
NUPKG=$(find "$NUPKG_DIR" -name "*.nupkg" ! -name "*.snupkg" | head -1)

if [ -z "$NUPKG" ]; then
    echo "Error: No .nupkg file found"
    exit 1
fi

echo ""
echo "Package: $NUPKG"
echo ""

# Push to NuGet
echo "Pushing to NuGet.org..."
dotnet nuget push "$NUPKG" --api-key "$API_KEY" --source https://api.nuget.org/v3/index.json --skip-duplicate

echo ""
echo "Done! Package published to NuGet.org"
echo "View at: https://www.nuget.org/packages/Zero.Mcp.Extensions/"
