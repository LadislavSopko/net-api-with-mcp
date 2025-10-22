§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[STACK]
@runtime::.NET§9.0.300
@framework::AspNetCore§9.0
@protocol::MCP§0.4.0-preview.3
@format::SLNX{modern:json-based}
@logging::Serilog{file:logs/mcppoc-.log}

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
│   ├─Program.cs{MCP:custom-extension}
│   ├─Properties/launchSettings.json
│   ├─Models/User.cs
│   ├─Services/{IUserService+UserService}.cs
│   ├─Extensions/McpServerBuilderExtensions.cs{170lines:key-solution}
│   ├─Controllers/UsersController.cs{[McpServerToolType]}
│   └─Filters/ActionResultUnwrapperFilter.cs{obsolete:can-delete}
├─tests/
│ └─McpPoc.Api.Tests/
│   ├─McpPoc.Api.Tests.csproj{xunit+WebApplicationFactory}
│   ├─McpApiFixture.cs{test-server}
│   ├─McpClientHelper.cs{SDK:HttpClientTransport}
│   ├─McpToolDiscoveryTests.cs{5/5:pass}✓
│   ├─HttpCoexistenceTests.cs{3/3:pass}✓
│   ├─McpToolInvocationTests.cs{5/5:pass}✓
│   └─ActionResultSerializationTest.cs{2/2:pass}✓
├─mem-bank-mbel5/{README+5cores}.md
├─3rdp/csharp-sdk/{localMCP:for-introspection}
└─tasks/tddab-mcp-integration-tests.md{plan:documented}

[DEPENDENCIES]
@nuget::ModelContextProtocol§0.4.0-preview.3{prerelease}
@nuget::Microsoft.AspNetCore.OpenApi§9.0.0
@nuget::Swashbuckle.AspNetCore§7.2.0
@nuget::Serilog.AspNetCore{file-logging}
@test::xunit§2.9.2
@test::FluentAssertions§7.0.0
@test::Microsoft.AspNetCore.Mvc.Testing§9.0.0

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
@build::dotnet-build{src/McpPoc.Api}
@run::dotnet-run{project:src/McpPoc.Api}
@test::dotnet-test{tests/McpPoc.Api.Tests→15/15:pass}✓

[ENDPOINTS]
@http::http://localhost:5001/api/users{GET+POST+DELETE}
@mcp::http://localhost:5001/mcp{tools/list+tools/call}
@swagger::http://localhost:5001/swagger{dev:only}

[PORT_CONFIG]
@port::5001{http:¬https}
@reason::POC{simplicity}

[POC_FINDINGS]
✅discovery::MCPDiscoversControllers{[McpServerToolType]+CustomExtension}
✅naming::snake_case{explicit:ConvertToSnakeCase}
✅coexistence::HTTPandMCP{both-work-simultaneously}
✅di::DependencyInjection{IUserService:properly-injected}
✅selective::DeleteNotExposed{¬[McpServerTool]:works}
✅serialization::ActionResultUnwrapping{custom-marshaller:works}!

[SDK_BEHAVIORS]
@naming::snake_case_lower{manual:JsonNamingPolicy.ConvertName}
@nullable::IsError{null=noError¬false}
@types::ContentBlock{cast-to:TextContentBlock}
@async::RemoveAsyncSuffix{manual:in-ConvertToSnakeCase}
@serialization::typeof(object){breaks:ActionResult<T>}
@marshalling::MarshalResult{intercept:before-serialize}!

[SOLUTION_DETAILS]
@file::Extensions/McpServerBuilderExtensions.cs
@method::WithToolsFromAssemblyUnwrappingActionResult
@key::MarshalResult{delegate:UnwrapActionResult}
@pattern::AIFunctionFactory.Create{manual:vs-WithToolsFromAssembly}
@naming::Name{property:explicit-snake_case}
@unwrapping::ActionResult<T>→Result→Value→actual-data

[CRITICAL_LEARNINGS]
!RootCause::MarshallerBroken{new-ValueTask:loses-value}!
!Fix::ValueTaskFromResult{proper-construction:preserves-value}!
!CallToolFilter::TooLate{post-marshalling→useless}
!TwoFixes::OneDelegate{marshaller+unwrapping:combined}
!ToolNames::Explicit{Name-property¬SerializerOptions}
!ActionResult::Unwrap{Result→IActionResult→Value}
