§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[VISION]
@mission::POCMCPIntegration{AspNetCore+MCP+OAuth2}
@goal::ProveDirectControllerToMCPToolsIntegration+SecureWithAuth
@scope::MinimalViableTest+ProductionReadySecurity{¬full-production}

[PROBLEM]
!question1::CanControllersBeDirectMCPTools{¬bridgeLibrary}✓
!question2::HowSecureMCPEndpoint{endpoint||per-tool}✓
@hypothesis::McpServerToolType+McpServerTool→autoDiscovery✓
@hypothesis2::HybridAuth{endpoint+metadata}→BestApproach✓
@testSubject::WithToolsFromAssembly{scanControllers}

[USER_GOALS]
@developer::UnderstandIntegrationPattern✓
@developer::AvoidUnnecessaryAbstractions✓
@developer::SecureMCPEndpoint{production-ready}✓
@decision::BridgeLibraryNeeded||DirectAttributesSufficient→Direct✓
@decision2::EndpointAuth||PerToolAuth||Hybrid→Hybrid✓

[SUCCESS_CRITERIA]
✓discovery::MCPToolsFound{#3:GetById+GetAll+Create}
✓invocation::ToolsCallable&ReturnCorrectData
✓coexistence::HTTPEndpoints&MCPEndpoints{parallel}
✓di::DependencyInjectionWorks{IUserService+ILogger}
✓authentication::KeycloakOAuth2{JWT+Bearer}
✓authorization::HybridApproach{endpoint+metadata}
✓performance::FastTokens{DNS:127.0.0.1→39ms}
✓testing::FlexibleFixture{on-demand+cache+roles}
✗nonExposure::DeleteEndpoint{¬McpServerTool→¬exposed}

[VALUE]
@timeSaved::AvoidPrematureAbstraction
@clarity::UnderstandSDKCapabilities
@direction::InformsArchitectureDecisions
@security::ProductionReady{OAuth2+JWT+Keycloak}
@flexibility::ReadyForRoleBasedAuth{metadata:collected}
@performance::OptimizedTests{cache+127.0.0.1}
