§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[VISION]
@mission::POCMCPIntegration{AspNetCore+MCP}
@goal::ProveDirectControllerToMCPToolsIntegration
@scope::MinimalViableTest{¬production}

[PROBLEM]
!question::CanControllersBeDirectMCPTools{¬bridgeLibrary}
@hypothesis::McpServerToolType+McpServerTool→autoDiscovery
@testSubject::WithToolsFromAssembly{scanControllers}

[USER_GOALS]
@developer::UnderstandIntegrationPattern
@developer::AvoidUnnecessaryAbstractions
@decision::BridgeLibraryNeeded||DirectAttributesSufficient

[SUCCESS_CRITERIA]
✓discovery::MCPToolsFound{#3:GetById+GetAll+Create}
✓invocation::ToolsCallable&ReturnCorrectData
✓coexistence::HTTPEndpoints&MCPEndpoints{parallel}
✓di::DependencyInjectionWorks{IUserService+ILogger}
✗nonExposure::DeleteEndpoint{¬McpServerTool→¬exposed}

[VALUE]
@timeSaved::AvoidPrematureAbstraction
@clarity::UnderstandSDKCapabilities
@direction::InformsArchitectureDecisions
