# Build stage - use specific digest for reproducibility
FROM mcr.microsoft.com/dotnet/sdk:10.0-preview AS build
WORKDIR /src

# Copy solution and build files (skip global.json to use container's SDK)
COPY ["net-api-with-mcp.slnx", "./"]
COPY ["Directory.Build.props", "./"]
COPY ["Directory.Packages.props", "./"]
COPY ["Version.props", "./"]

# Copy project files for restore
COPY ["src/McpPoc.Api/McpPoc.Api.csproj", "src/McpPoc.Api/"]
COPY ["src/Zero.Mcp.Extensions/Zero.Mcp.Extensions.csproj", "src/Zero.Mcp.Extensions/"]

# Copy 3rdp SDK project files (skip global.json to use container's SDK)
COPY ["3rdp/csharp-sdk/Directory.Build.props", "3rdp/csharp-sdk/"]
COPY ["3rdp/csharp-sdk/Directory.Packages.props", "3rdp/csharp-sdk/"]
COPY ["3rdp/csharp-sdk/src/ModelContextProtocol/ModelContextProtocol.csproj", "3rdp/csharp-sdk/src/ModelContextProtocol/"]
COPY ["3rdp/csharp-sdk/src/ModelContextProtocol.Core/ModelContextProtocol.Core.csproj", "3rdp/csharp-sdk/src/ModelContextProtocol.Core/"]
COPY ["3rdp/csharp-sdk/src/ModelContextProtocol.AspNetCore/ModelContextProtocol.AspNetCore.csproj", "3rdp/csharp-sdk/src/ModelContextProtocol.AspNetCore/"]

# Restore dependencies
RUN dotnet restore "src/McpPoc.Api/McpPoc.Api.csproj"

# Copy source code
COPY . .

# Build the application
WORKDIR "/src/src/McpPoc.Api"
RUN dotnet build "McpPoc.Api.csproj" -c Release -o /app/build --no-restore

# Publish stage - self-contained to avoid runtime version mismatch
FROM build AS publish
RUN dotnet publish "McpPoc.Api.csproj" -c Release -o /app/publish \
    --self-contained true \
    -r linux-x64 \
    /p:PublishSingleFile=false

# Runtime stage - use minimal base (no .NET runtime needed for self-contained)
FROM mcr.microsoft.com/dotnet/runtime-deps:10.0-preview AS final
WORKDIR /app

# Copy published application
COPY --from=publish /app/publish .

# Default port 80 - map externally to whatever you need
EXPOSE 80

ENV ASPNETCORE_URLS=http://+:80

ENTRYPOINT ["./McpPoc.Api"]
