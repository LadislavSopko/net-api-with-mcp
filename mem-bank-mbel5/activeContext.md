§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[FOCUS]
⚡phase7::ToolNaming+McpContext{TDDAB:9-blocks:31-tests}!
@status::Ready-to-implement{plan-created}
@version::2.0.0→2.1.0{after-implementation}

✅upgrade::.NET10{9.0→10.0:complete}✅
✅phase5::LibraryExtraction{Zero.Mcp.Extensions→NuGet}✅
⚡phase6::RoleBasedToolFiltering{paused:phase7-priority}

[RECENT]
>upgraded::.NET{9.0→10.0.100}✅
>replaced::Swashbuckle→Scalar.AspNetCore§2.12.11✅
>verified::SDK-hack{MarshalResult:still-needed}✅
>created::TDDAB-plan{tool-naming+mcp-context}!
@plan::tasks/tddab-tool-naming-and-mcp-context.md

[PHASE7_FEATURES]
@feature1::ToolNamingConvention{
  problem::GenericControllers→DuplicateToolNames
  solution::ControllerPrefix{products_get_by_id}
  priority::[McpServerTool(Name)]>Convention
  default::MethodOnly{backward-compatible}
}

@feature2::IMcpRequestContext{
  problem::NeedHeaderAccess+McpCallDetection
  solution::Interface{IsMcpCall+GetHeader+Headers}
  header::x-mcp-call{auto-injected:server-side}
  lifetime::Scoped
}

[PHASE7_TDDAB]
@blocks::9
@tests::31-new{97→128-total}
@loc::~290
@version::2.0.0→2.1.0

@block1::Options{enum+defaults:4-tests}
@block2::ToolNameGenerator{core-logic:6-tests}
@block3::BuilderIntegration{3-tests}
@block4::IMcpRequestContext{interface+impl:5-tests}
@block5::Middleware{x-mcp-call:3-tests}
@block6::Registration{2-tests}
@block7::E2E-ToolNaming{4-tests}
@block8::E2E-McpContext{4-tests}
@block9::VersionBump{2.1.0+pack}

[ARCHITECTURE]
@naming::{
  ToolNamingConvention::enum{MethodOnly|ControllerPrefix}
  ToolNameGenerator::static{GenerateName+GetControllerPrefix+ToSnakeCase}
  ZeroMcpOptions::+NamingConvention+ToolNameSeparator
}

@context::{
  IMcpRequestContext::interface{IsMcpCall+GetHeader+Headers}
  McpRequestContext::impl{IHttpContextAccessor+Items["__McpCall"]}
  Middleware::adds{Items["__McpCall"]+Header["x-mcp-call"]}
}

[DECISIONS]
@naming-default::MethodOnly{backward-compatible}
@naming-prefix::ControllerPrefix{removes-Controller-suffix+snake_case}
@context-lifetime::Scoped
@mcp-header::x-mcp-call{auto-injected:guaranteed}
@attribute-priority::[McpServerTool(Name)]>Convention{always}

[NEXT]
?ACT::Block1{ToolNamingConvention-enum+ZeroMcpOptions}
@command::ACT{to-start-implementation}

[SDK_STATUS]
!hack-still-needed::MarshalResult{ActionResult<T>:unwrapping}✅
@sdk-version::0.6.0-preview.1
@no-actionresult-support::confirmed{checked-2026-01-20}
