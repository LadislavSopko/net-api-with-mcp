§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[ARCHITECTURE]
@pattern::MinimalAPI+Controllers+MCPServer
@style::LayeredArchitecture{Controllers→Services→Data}
@transport::HTTPServer{AspNetCore+MCPEndpoint}

[COMPONENTS]
@model::User{Id+Name+Email+CreatedAt}
@service::IUserService{GetById+GetAll+Create}
@controller::UsersController{[McpServerToolType]}
@mcp::MCPServer{httpTransport:/mcp}

[DATA_FLOW]
MCPClient→/mcp→MCPServer→ToolDiscovery→ControllerMethod→Service→Response
HTTPClient→/api/users→AspNetCore→ControllerMethod→Service→Response

[KEY_PATTERNS]
@di::Constructor{IUserService+ILogger}
@attributes::[McpServerToolType]{class}+[McpServerTool]{methods}
@coexistence::[HttpGet]+[McpServerTool]{sameMethod}
@returnTypes::ActionResult<T>{wrappedResponse}

[DISCOVERY_MECHANISM]
@registration::AddMcpServer→WithHttpServerTransport→WithToolsFromAssembly
@scan::Assembly{seek:[McpServerToolType]}
@expose::Methods{filter:[McpServerTool]}
@schema::AutoGenerate{fromMethodSignature+[Description]}

[INTEGRATION_POINTS]
@endpoint::MapMcp{path:/mcp}
@endpoint::MapControllers{path:/api/*}
@swagger::SwaggerUI{development:only}

[TEST_STRATEGY]
@verify::ToolsList{POST:/mcp{method:tools/list}}
@verify::ToolCall{POST:/mcp{method:tools/call}}
@verify::HTTPStillWorks{GET:/api/users}
@verify::DIWorks{servicesInjected}
