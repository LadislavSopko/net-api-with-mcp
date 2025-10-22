§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[STACK]
@runtime::.NET§9.0.300
@framework::AspNetCore§9.0
@protocol::MCP§0.4.0-preview.3
@format::SLNX{modern:json-based}

[STRUCTURE]
/
├─net-api-with-mcp.slnx{solution:modern}
├─Directory.Packages.props{cpm:enabled}
├─Directory.Build.props{common:props}
├─Version.props{versioning}
├─global.json{sdk:9.0.300}
├─src/
│ └─McpPoc.Api/
│   ├─McpPoc.Api.csproj
│   ├─Program.cs
│   ├─Properties/launchSettings.json
│   ├─Models/User.cs
│   ├─Services/{IUserService+UserService}.cs
│   └─Controllers/UsersController.cs
├─mem-bank-mbel5/{README+5cores}.md
├─3rdp/csharp-sdk/{localMCP:available¬used}
└─tasks/mcp-controller-test-prompt.md

[DEPENDENCIES]
@nuget::ModelContextProtocol§0.4.0-preview.3{prerelease}
@nuget::Microsoft.AspNetCore.OpenApi§9.0.0
@nuget::Swashbuckle.AspNetCore§7.2.0

[CPM_CONFIG]
@file::Directory.Packages.props
@mode::CentralPackageManagement{enabled}
@versions::ManageAllInOnePlace

[BUILD_CONFIG]
@props::Directory.Build.props→Version.props
@author::Ladislav-Sopko
@company::0ics-srl
@langVersion::latest

[COMMANDS]
@build::dotnet-build
@run::dotnet-run{project:src/McpPoc.Api}
@test::curl{http://localhost:5001}

[ENDPOINTS]
@http::http://localhost:5001/api/users
@mcp::http://localhost:5001/mcp
@swagger::http://localhost:5001/swagger{dev:only}

[PORT_CONFIG]
@port::5001{http:¬https}
@reason::POC{simplicity}
