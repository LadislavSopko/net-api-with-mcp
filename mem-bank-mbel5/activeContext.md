§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[FOCUS]
✅phase7::ToolNaming+McpContext{COMPLETE:9-blocks:131-tests}✅!
@version::2.1.0{released}
@status::Ready-for-next-phase

✅upgrade::.NET10{9.0→10.0:complete}✅
✅phase5::LibraryExtraction{Zero.Mcp.Extensions→NuGet}✅
✅phase7::ToolNaming+McpContext{COMPLETE}✅!
⏸phase6::RoleBasedToolFiltering{paused:ready-to-resume}

[RECENT]
>implemented::Phase7-TDDAB{9-blocks:all-complete}✅
>created::ToolNamingConvention{enum:MethodOnly|ControllerPrefix}✅
>created::ToolNameGenerator{GenerateName+GetControllerPrefix+ToSnakeCase}✅
>created::IMcpRequestContext{IsMcpCall+GetHeader+Headers}✅
>created::McpRequestContext{impl:path-based-fallback}✅
>created::UseZeroMcpMarking{middleware:x-mcp-call-injection}✅
>added::[McpServerTool(Name)]{explicit-naming-priority}✅
>bumped::Version{2.0.0→2.1.0}✅
>verified::NuGet-pack{Zero.Mcp.Extensions.2.1.0.nupkg}✅

[PHASE7_COMPLETE]
@feature1::ToolNamingConvention{
  problem::GenericControllers→DuplicateToolNames
  solution::ControllerPrefix{products_get_by_id}
  priority::[McpServerTool(Name)]>Convention
  default::MethodOnly{backward-compatible}
  status::COMPLETE✅
}

@feature2::IMcpRequestContext{
  problem::NeedHeaderAccess+McpCallDetection
  solution::Interface{IsMcpCall+GetHeader+Headers}
  header::x-mcp-call{auto-injected:middleware}
  lifetime::Scoped
  limitation::MCP-SDK{HttpContext:not-flowed-to-tool-scopes}
  workaround::Path-based-fallback{/mcp→IsMcpCall:true}
  status::COMPLETE✅
}

[PHASE7_TDDAB_RESULTS]
@blocks::9/9✅
@tests::131-total{75-unit+56-E2E}✅
@loc::~350
@version::2.1.0✅

@block1::Options{enum+defaults:4-tests}✅
@block2::ToolNameGenerator{core-logic:7-tests}✅
@block3::BuilderIntegration{7-tests}✅
@block4::IMcpRequestContext{interface+impl:7-tests}✅
@block5::Middleware{x-mcp-call:3-tests}✅
@block6::Registration{2-tests}✅
@block7::E2E-ToolNaming{4-tests}✅
@block8::E2E-McpContext{4-tests}✅
@block9::VersionBump{2.1.0+pack}✅

[NEW_FILES_CREATED]
@src::Zero.Mcp.Extensions/{
  ToolNamingConvention.cs::enum{MethodOnly|ControllerPrefix}
  ToolNameGenerator.cs::static{GenerateName+ToSnakeCase+GetControllerPrefix}
  IMcpRequestContext.cs::interface
  McpRequestContext.cs::impl{path-based-fallback}
}

@tests::Zero.Mcp.Extensions.Tests/{
  ZeroMcpOptionsTests.cs::4-tests
  ToolNameGeneratorTests.cs::7-tests
  McpRequestContextTests.cs::7-tests
  McpMiddlewareTests.cs::3-tests
}

@tests::McpPoc.Api.Tests/{
  ToolNamingTests.cs::4-tests{E2E}
  McpRequestContextE2ETests.cs::4-tests{E2E}
}

[DECISIONS]
@naming-default::MethodOnly{backward-compatible}
@naming-prefix::ControllerPrefix{removes-Controller-suffix+snake_case}
@context-lifetime::Scoped
@mcp-header::x-mcp-call{auto-injected:middleware}
@attribute-priority::[McpServerTool(Name)]>Convention{always}
@sdk-limitation::HttpContext{not-flowed:documented+path-fallback}

[NEXT]
?phase6::RoleBasedToolFiltering{ready-to-resume}
?phase8::AdvancedAuth{custom-requirements+conditional-policies}
?production::Deploy{when-ready}

[SDK_STATUS]
!hack-still-needed::MarshalResult{ActionResult<T>:unwrapping}✅
@sdk-version::0.6.0-preview.1
@sdk-limitation::HttpContext{not-flowed-to-tool-invocation-scopes}!
@workaround::Path-based-fallback{/mcp:StartsWith}
