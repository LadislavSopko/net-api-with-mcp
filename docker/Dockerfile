# Build stage
FROM mcr.microsoft.com/dotnet/sdk:9.0 AS build
WORKDIR /src

# Copy solution and project files
COPY ["net-api-with-mcp.slnx", "./"]
COPY ["src/McpPoc.Api/McpPoc.Api.csproj", "src/McpPoc.Api/"]
COPY ["tests/McpPoc.Api.Tests/McpPoc.Api.Tests.csproj", "tests/McpPoc.Api.Tests/"]
COPY ["Directory.Build.props", "./"]
COPY ["Version.props", "./"]
COPY ["global.json", "./"]
COPY ["nuget.config", "./"]

# Restore dependencies
RUN dotnet restore "src/McpPoc.Api/McpPoc.Api.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/src/McpPoc.Api"
RUN dotnet build "McpPoc.Api.csproj" -c Release -o /app/build

# Publish stage
FROM build AS publish
RUN dotnet publish "McpPoc.Api.csproj" -c Release -o /app/publish /p:UseAppHost=false

# Runtime stage
FROM mcr.microsoft.com/dotnet/aspnet:9.0 AS final
WORKDIR /app

# Install curl for healthchecks
RUN apt-get update && apt-get install -y curl && rm -rf /var/lib/apt/lists/*

# Copy published application
COPY --from=publish /app/publish .

# Create logs directory
RUN mkdir -p /app/logs

# Expose port
EXPOSE 8080

# Set environment variables
ENV ASPNETCORE_URLS=http://+:8080
ENV ASPNETCORE_ENVIRONMENT=Development

# Entry point
ENTRYPOINT ["dotnet", "McpPoc.Api.dll"]
