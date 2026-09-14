§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[FOCUS]
@state::PLAN{approved:ready-for-j-develop}
@feature::01-upgrade-mcp-sdk-2
@branch::feature/laco/upgrade
@task-notes::tasks/01-upgrade-mcp-sdk-2/notes.md
@task-plan::tasks/01-upgrade-mcp-sdk-2/plan.md{9-blocks:57-red-lines:j-review-plan✓:cvm-parsePlan✓}
@junior-workflow::active{j-settings.md:2026-09-14}
@version::2.1.0{released}→?3.0.0{target}
@status::Phase8-Upgrade{PLAN-approved:DEVELOP-not-started}

⚡phase8::UpgradeToSdk2.2.0{DEVELOP:4/9-blocks:run-20260914-1620}
✅block01::migrate-test-stack-and-deps{COMPLETE:xunit.v3+AwesomeAssertions+NSubstitute}✅
✅phase7::ToolNaming+McpContext{COMPLETE:9-blocks:131-tests}✅
✅upgrade::.NET10{9.0→10.0:complete}✅
✅phase5::LibraryExtraction{Zero.Mcp.Extensions→NuGet}✅
✗phase6::RoleBasedToolFiltering{SUPERSEDED:SDK-AddAuthorizationFilters-covers-it}

[SESSION_2026-09-14]
>completed::block01{test-stack:xunit.v3§4.0.1+AwesomeAssertions§9.6.0+NSubstitute§6.2.0}✅
>upgraded::deps{aspnetcore§10.0.12+Scalar§2.17.3+SourceLink§10.0.401+TestSdk§18.10.0:vulnerable-clean}✅
>verified::test-counts{unit:77✅+E2E:56✅:tool-names:9-tools}✅
>verified::lint{error-severity-EXIT0:IDE0005-cleared:TreatWarningsAsErrors:false}✅
>verified::docker{keycloak:8080:postgres:15432}✅
>verified::3rdp-sdk{gitlink-v2.2.0:signature-extraction-OK}✅
>staged::cvm-exec{id:run-20260914-1620:block-02-complete}✅

[PHASE8_DECISIONS]{user-approved:2026-09-14}
@sdk::ModelContextProtocol§2.2.0{0.6.0-preview.1→2.2.0}
@auth::SDK-AddAuthorizationFilters(){replaces:IAuthForMcpSupplier+McpAuthorizationPreFilter+ToolListFilter+IUserRoleResolver+ToolAuthorizationMetadata}
@attributes::SDK{ModelContextProtocol.Server.McpServerToolType+McpServerTool:own-copies-deleted}
@keep::MarshalResult{ActionResult<T>:SDK-still-passthrough:our-differentiator}!
@keep::ZeroMcpOptions.UseAuthorization{false→no-auth-metadata+no-filters}
@remove::ZeroMcpOptions.FilterToolsByPermissions
@new::ZeroMcpOptions.ToolsListTimeToLive{ttlMs+cacheScope:Private|Public}
@new::ZeroMcpOptions.SessionMode{default:Stateless}
@new::ToolMetadataBuilder+ToolCreateOptionsFactory{Title+hints+OutputSchemaType+IconSource}
@tests::xunit.v3§4.0.1+AwesomeAssertions§9.6.0+NSubstitute§6.2.0{Moq+FluentAssertions-removed}
@deps::Microsoft.*§10.0.12+Scalar§2.17.3+SourceLink§10.0.401+Test.Sdk§18.10.0
@3rdp::csharp-sdk{gitlink:v0.6.0-preview.1→v2.2.0}
@version::3.0.0{breaking}
@lint::close-debt-in-block-08{TreatWarningsAsErrors:true}

[PHASE8_BLOCKS]
✅01::migrate-test-stack-and-deps{COMPLETE}✅
✅02::tool-metadata-builder{COMPLETE:ToolMetadataBuilder.cs:internal-static:Build(MethodInfo,includeAuthorization)→[MethodInfo,class-attrs,method-attrs]:strips-IAuthorizeData/IAllowAnonymous-when-false:7-tests}✅
✅03::upgrade-sdk-and-adopt-sdk-attributes{COMPLETE:ModelContextProtocol*→2.2.0:own-McpServerToolType/McpServerTool-attributes-DELETED:scanner+ToolNameGenerator-read-ModelContextProtocol.Server.*:tools/list-filter-via-WithRequestFilters(f=>f.AddListToolsFilter):custom-auth-STILL-present-until-block-05:8-tests}✅
✅04::tool-create-options-factory{COMPLETE:ToolCreateOptionsFactory.cs:internal-static:Create(MethodInfo,IServiceProvider,JsonSerializerOptions,includeAuthorization)→McpServerToolCreateOptions{Title+hints-only-when-non-default+UseStructuredContent+OutputSchema(AIJsonUtilities.CreateJsonSchema)+Icons+Description+Metadata}:wired-in-both-registration-paths-with-includeAuthorization:false-LITERAL(SDK-guard-filters-would-throw-before-block-05):13-tests}✅
⚡05::replace-custom-auth-with-sdk-filters{NEXT}
?06::tools-list-cache-hints
?07::stateless-and-output-schema-e2e
?08::close-lint-debt-and-enable-gate
?09::release-3-0-0

[SDK_2.2.0_FACTS]{verified-on-source}
@filters::WithRequestFilters(f=>f.AddListToolsFilter|AddCallToolFilter){AddXxxFilter-on-builder:REMOVED}
@authz::AddAuthorizationFilters(){reads:McpServerTool.Metadata:[MethodInfo+class-attrs+method-attrs]:throws-if-metadata-without-filter}
@create::McpServerTool.Create(AIFunction,McpServerToolCreateOptions{Services+Metadata+Title+hints+OutputSchema+Icons})
@transport::HttpServerTransportOptions.SessionMode{Stateless-default|Stateful|StatefulForInitializeClients}+EnableLegacySse{false-default}
@list::ListToolsResult{Tools+TimeToLive+CacheScope}
@context::RequestContext<T>{User+Services+Items+Params+MatchedPrimitive}
@spec::2026-07-28{no-initialize+no-session-id+server/discover+Mcp-Method-headers+ttlMs+Roots/Sampling/Logging-deprecated+DCR→CIMD}

[ECOSYSTEM_2026-09]
@competitors::ZeroMCP.net§2.0.0{name-collision!}+Nabu.Mcp.AspNetCore§1.0.14+McpIt§1.4.0{all-more-downloads-than-us:743}
@keycloak::MCP-guide{CIMD-experimental-26.6:no-RFC8707→ValidateAudience:false-still-needed}
@nuget::Zero.Mcp.Extensions{2.0.0+2.1.0:743-dl:0-issues:0-PRs}

[NEXT]
!next::j-cvm-exec-plan{block-05-replace-custom-auth-with-sdk-filters:AddAuthorizationFilters+delete-IAuthForMcpSupplier/PreFilter/ToolListFilter/IUserRoleResolver+flip-includeAuthorization→options.UseAuthorization:8-tests:docker-keycloak-up}
?then::j-close{merge-direct→main:publish-nuget.sh}
?later::git-crypt.key{delete-from-root-after-saving}
?later::ast-grep{install-for-lint-layer-2}

[SDK_STATUS]
!hack-still-needed::MarshalResult{ActionResult<T>:unwrapping:confirmed-on-2.2.0-source}✅
@sdk-version::0.6.0-preview.1{current}→2.2.0{target}
@sdk-limitation-old::HttpContext{not-flowed}→2.2.0{stateless:per-request-HttpContext:re-verify-in-block-07}
@workaround::Path-based-fallback{/mcp:StartsWith}{keep-until-block-07-proves-unneeded}

[ARCHIVE_PHASE7]
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
@tests::161-total{105-unit+56-E2E}✅
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

