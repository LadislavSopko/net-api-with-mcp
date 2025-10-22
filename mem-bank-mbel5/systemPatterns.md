§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[ARCHITECTURE]
@pattern::MinimalAPI+Controllers+MCPServer+OAuth2
@style::LayeredArchitecture{Controllers→Services→Data}
@transport::HTTPServer{AspNetCore+MCPEndpoint}
@security::HybridAuthorization{endpoint+metadata}

[COMPONENTS]
@model::User{Id+Name+Email+CreatedAt}
@service::IUserService{GetById+GetAll+Create}
@controller::UsersController{[McpServerToolType]+[Authorize]}
@mcp::MCPServer{httpTransport:/mcp+RequireAuthorization}
@auth::Keycloak{OAuth2+OIDC+JWTBearer}

[DATA_FLOW]
MCPClient→/mcp{RequireAuthorization}→JWTValidation→MCPServer→ToolDiscovery→ControllerMethod→Service→Response
HTTPClient→/api/users{[Authorize]}→JWTValidation→AspNetCore→ControllerMethod→Service→Response

[KEY_PATTERNS]
@di::Constructor{IUserService+ILogger}
@attributes::[McpServerToolType]{class}+[McpServerTool]{methods}+[Authorize]{security}
@coexistence::[HttpGet]+[McpServerTool]{sameMethod}
@returnTypes::ActionResult<T>{wrappedResponse}
@auth::JWTBearer{ValidateAudience:false:Keycloak-azp}!

[SECURITY_ARCHITECTURE]
@layer1::EndpointAuth{
  MapMcp("/mcp").RequireAuthorization()
  →AllRequests:authenticated
}
@layer2::MetadataCollection{
  SDK→CollectAttributes:[Authorize]
  →ReadyForPerToolAuth
}
@layer3::HttpContext{
  User.Identity→Available
  Claims→Accessible
  →FineGrainedChecks:possible
}

[DISCOVERY_MECHANISM]
@registration::AddMcpServer→WithHttpTransport→WithToolsFromAssemblyUnwrappingActionResult
@scan::Assembly{seek:[McpServerToolType]}
@expose::Methods{filter:[McpServerTool]}
@schema::AutoGenerate{fromMethodSignature+[Description]}
@metadata::SDK{automatic:[Authorize]+custom-attributes}

[INTEGRATION_POINTS]
@endpoint::MapMcp{path:/mcp+RequireAuthorization}!
@endpoint::MapControllers{path:/api/*+[Authorize]}
@swagger::SwaggerUI{development:only+OAuth2}
@auth::JWTBearer{Keycloak:http://127.0.0.1:8080}

[TEST_STRATEGY]
@verify::ToolsList{POST:/mcp+Bearer-token}
@verify::ToolCall{POST:/mcp+Bearer-token}
@verify::HTTPStillWorks{GET:/api/users+Bearer-token}
@verify::DIWorks{servicesInjected}
@verify::Auth{401:no-token+200:valid-token}
@verify::Performance{DNS:127.0.0.1→39ms}

[TEST_FIXTURE_PATTERN]
@pattern::OnDemandAuthentication{
  GetAuthenticatedClientAsync():default
  GetAuthenticatedClientAsync(user,pass):role-based
  GetUnauthenticatedClient():401-tests
}
@cache::TokenCache{Dictionary<user,token>}
@benefit::NoConstructorDelay+Flexible+Fast
