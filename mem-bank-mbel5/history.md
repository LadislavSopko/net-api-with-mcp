§MBEL:5.0
@purpose::ClosedWorkArchive{write-only:never-read-at-session-start}

## 2026-09-17 — 01-upgrade-mcp-sdk-2 (Zero.Mcp.Extensions 3.0.0 on MCP SDK 2.2.0)

@closed::j-close{merged→main:4c7f09e:pushed:nuget.org-3.0.0-published}
@commits::3a7e018..4c7f09e{20-commits:9-TDDAB-blocks-via-CVM-run-20260914-1620}
@gate::2026-09-17{build:0-warnings-4-projects:unit-99/99:E2E-59/59:dotnet-format-exit-0:ast-grep-absent-skip}

### from activeContext.md

[FOCUS]
@state::TEST{phase8-complete:9/9-blocks:next-j-close}
@feature::01-upgrade-mcp-sdk-2
@branch::feature/laco/upgrade
@task-notes::tasks/01-upgrade-mcp-sdk-2/notes.md
@task-plan::tasks/01-upgrade-mcp-sdk-2/plan.md{9-blocks:57-red-lines:j-review-plan✓:cvm-parsePlan✓}
@junior-workflow::active{j-settings.md:2026-09-14}
@version::3.0.0{released-block-09}✅
@status::Phase8-Upgrade{COMPLETE:9/9-blocks:TEST}

✅phase8::UpgradeToSdk2.2.0{COMPLETE:9/9-blocks:TEST-next-j-close}✅
✅block01::migrate-test-stack-and-deps{COMPLETE:xunit.v3+AwesomeAssertions+NSubstitute}✅
✅block02::tool-metadata-builder{COMPLETE:ToolMetadataBuilder.cs}✅
✅block03::upgrade-sdk-2.2.0{COMPLETE:ModelContextProtocol§2.2.0}✅
✅block04::tool-create-options-factory{COMPLETE:ToolCreateOptionsFactory.cs}✅
✅block05::sdk-authorization-filters{COMPLETE:SDK-AddAuthorizationFilters}✅
✅block06::tools-list-cache-hints{COMPLETE:ToolsListTimeToLive}✅
✅block07::stateless-output-schema{COMPLETE:SessionMode+OutputSchemaType}✅
✅block08::lint-gate-strict{COMPLETE:TreatWarningsAsErrors:true}✅
✅block09::release-3-0-0{COMPLETE:Version-3-0-0:README-rewrite:CHANGELOG-created:PackageTests-updated}✅
✅phase7::ToolNaming+McpContext{COMPLETE:9-blocks:131-tests}✅
✅phase8::SDK-2.2.0-Upgrade{COMPLETE:9-blocks:158-tests}✅
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
>staged::cvm-exec{id:run-20260914-1620:block-08-complete}✅
>completed::block05{SDK-AddAuthorizationFilters:deleted-9-auth-classes:kept-MarshalResult:tests-unit90+E2E53}✅
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
@lint::STRICT{TreatWarningsAsErrors:true:0-warnings-all-4-projects:dotnet-format-exit-0}✅
[PHASE8_BLOCKS]
✅01::migrate-test-stack-and-deps{COMPLETE}✅
✅02::tool-metadata-builder{COMPLETE:ToolMetadataBuilder.cs:7-tests}✅
✅03::upgrade-sdk-2.2.0-sdk-attributes{COMPLETE:ModelContextProtocol→2.2.0:8-tests}✅
✅04::tool-create-options-factory{COMPLETE:ToolCreateOptionsFactory.cs:13-tests}✅
✅05::replace-auth-with-sdk-filters{COMPLETE:AddAuthorizationFilters:unit90+E2E53}✅
✅06::tools-list-cache-hints{COMPLETE:ToolsListTimeToLive+CacheScope:6-tests}✅
✅07::stateless-output-schema-e2e{COMPLETE:SessionMode+OutputSchemaType:6-tests}✅
✅08::close-lint-debt-enable-gate{COMPLETE:TreatWarningsAsErrors:true:0-warnings:unit96+E2E59}✅
✅09::release-3-0-0{COMPLETE:Version-3-0-0:README+CHANGELOG+PackageTests:unit99+E2E59}✅
[POST_PHASE8_2026-09-14]{SESSION-COMPLETE}
>docs::3-files-rewritten{MCP-COMPLETE-INTEGRATION-GUIDE:3570-lines-v1→v3:MCP-AUTHORIZATION-COMPLETE-GUIDE:SDK-native-authz-deep-dive+single-"What-Changed-3.0.0"-section:USERS-AND-PERMISSIONS:9-tools+per-role-visibility:PAT-AUTHENTICATION-DESIGN}✓
>mcp-json::created{symlink:root→.00-secrets/.mcp.json:cvm-server-entry+FOUR-poc-servers:poc{admin/admin123:9-tools}+poc-manager{bob@example.com/bob123:8-tools}+poc-member{alice@example.com/alice123:7-tools}+poc-viewer{viewer/viewer123:6-tools}:all→http://127.0.0.1:5001/mcp:Keycloak-password-grant-tokens}✓
>demo-service::detached{Start-Process-dotnet-run:src/McpPoc.Api:--no-build:--no-launch-profile:--urls-http://127.0.0.1:5001:log-.cvm/outputs/poc-service.log:LOCKS-Zero.Mcp.Extensions.dll→builds-fail-MSB3021-until-stopped}✓
>verification::verified-mcp-server{get_mcp_context:is_mcp_call-true✓:UserGetById/get_all/get_public_info-bare-payloads✓:create+promote_to_manager-as-admin✓:unknown-id→tool-error✓:per-role-tools/list-9/8/7/6✓}✓
>async-investigation::dead-code{MarshalResult.UnwrapAsync-Task/ValueTask-branches-2025-10-30:DEAD-CODE:Microsoft.Extensions.AI-10.8.3-awaits-Task/ValueTask-before-marshaller→ActionResult<T>-direct:kept-harmless:candidate-cleanup}✓
[NEXT]
?then::j-close{run-full-gate:push-feature-branch:merge→main:publish-nuget.sh→https://www.nuget.org/packages/Zero.Mcp.Extensions}
?followup::mcp-json-token{bearer-expires-60min:regenerate-POST-http://127.0.0.1:8080/realms/mcppoc-realm/protocol/openid-connect/token:client_id=mcppoc-api:grant_type=password}
?followup::get-token.sh{CRLF-line-endings-issue:fails-from-Python-subprocess:works-bash-direct}⚠
?followup::cvm-mcp-reconnect{after-.mcp.json-edits→/mcp-required}
@sdk-version::0.6.0-preview.1{current}→2.2.0{target}
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
  verified::SDK-2.2.0{HttpContext-flows-into-tools:IsMcpCall=true:E2E-Should_ReportIsMcpCallTrue_WhenInvokedOverStatelessTransport}✅
  fallback::Path-based{/mcp→IsMcpCall:true:kept-as-safety-net}
  status::COMPLETE✅
}

[PHASE7_TDDAB_RESULTS]
@blocks::9/9✅
@tests::155-total{96-unit+59-E2E}✅
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

### from progress.md

[STATUS]
✅phase8::UpgradeToSdk2.2.0{COMPLETE:9/9-blocks:TEST:feature/laco/upgrade}✅
✅post-phase8::ReleaseDocumentation{COMPLETE:9/14/2026:docs-3-files-rewritten:MCP-Integration-3570-lines:MCP-Authorization-SDK-native:Users-Permissions-9-tools:PAT-Design}✅
✅post-phase8::MCP-POC-Setup{COMPLETE:.mcp.json-config:cvm+4-role-based-servers:admin/bob/alice/viewer:demo-service-detached:role-verification-9/8/7/6-tools}✅
✅post-phase8::Async-Analysis{COMPLETE:MarshalResult-dead-code-found:ValueTask-pattern-documented:cleanup-optional}✅
✅upgrade::.NET10{9.0→10.0.100:complete+verified}✅!
✅phase5::COMPLETE{library-extraction:Zero.Mcp.Extensions-v2.0.0}✅
✅phase7::COMPLETE{ToolNaming+McpContext:v2.1.0}✅!
✗phase6::RoleBasedToolFiltering{SUPERSEDED←SDK-AddAuthorizationFilters}
@tests::158/158{100%}✅{99-unit+59-E2E}
@version::3.0.0✅
@lint-gate::STRICT{TreatWarningsAsErrors:true:0-warnings:closed-phase8-block-08}✅
@security-audit::NU1903{Microsoft.OpenApi-2.0.0-high+SourceLink-10.0.102-moderate}→fixed-in-phase8-block-01
@infra::.ai-agent-submodule+git-crypt(.00-secrets/)+j-settings.md✅
[PHASE8_PLAN]{2026-09-14:8/9-blocks}
@plan::tasks/01-upgrade-mcp-sdk-2/plan.md{9-blocks:57-tests:reviewed✓}
✅block01::test-stack+deps{xunit.v3§4.0.1+AwesomeAssertions§9.6.0+NSubstitute§6.2.0+deps:unit77+E2E56}✅
✅block02::ToolMetadataBuilder{7-tests:unit84+E2E56}✅
✅block03::SDK-2.2.0+SDK-attributes+WithRequestFilters{8-tests:unit92+E2E56}✅
✅block04::ToolCreateOptionsFactory{13-tests:unit105+E2E56:includeAuthorization:false-until-block-05}✅
✅block05::AddAuthorizationFilters{SDK-filters:delete-IAuthForMcpSupplier/PreFilter/ToolListFilter/IUserRoleResolver:tools/list-filtered:tools/call→McpProtocolException:unit90+E2E53}✅
✅block06::ToolsListTimeToLive{ttlMs+cacheScope:Private|Public:6-tests:unit95+E2E54}✅
✅block07::Stateless+OutputSchemaType-E2E{6-tests:unit96+E2E59:IsMcpCall-true-under-stateless}✅
✅block08::lint-debt+TreatWarningsAsErrors:true{unit96+E2E59:0-warnings-4-projects}✅
✅block09::release-3-0-0{Version-3-0-0:README-rewrite:CHANGELOG-added:PackageTests-updated:unit99+E2E59}✅
@openapi::Scalar§2.12.11{modern-UI}✅
[PHASE7_COMPLETE]
✅status::COMPLETE{TDDAB:9-blocks:131-tests}✅!
@problem1::GenericControllers{duplicate-tool-names}→SOLVED✅
@problem2::HeaderAccess{need-mcp-call-detection}→SOLVED✅
@solution1::ToolNamingConvention{ControllerPrefix:works}✅
@solution2::IMcpRequestContext{+path-based-fallback}✅

@block1::Options{enum+defaults:4-tests}✅
@block2::ToolNameGenerator{core-logic:7-tests}✅
@block3::BuilderIntegration{7-tests}✅
@block4::IMcpRequestContext{interface+impl:7-tests}✅
@block5::Middleware{x-mcp-call:3-tests}✅
@block6::Registration{2-tests}✅
@block7::E2E-ToolNaming{4-tests}✅
@block8::E2E-McpContext{4-tests}✅
@block9::VersionBump{2.1.0+pack}✅
!sdk-httpcontext::NotFlowed-to-tool-scopes{use:path-based-detection}!
✅phase5::COMPLETE{library+viewer+tests}✅
✅.NET10::COMPLETE{upgraded+verified}✅
✅phase7::COMPLETE{ToolNaming+McpContext:v2.1.0}✅!
[TASK_FILES]
@phase6::tasks/tddab-tool-filtering-by-permissions.md{paused}
@phase7::tasks/tddab-tool-naming-and-mcp-context.md{COMPLETE}✅
[V2.1.0_RELEASE_NOTES]
@features::{
  ToolNamingConvention::ControllerPrefix{generic-controllers}
  IMcpRequestContext::header-access+mcp-detection
  UseZeroMcpMarking::middleware{x-mcp-call-injection}
  McpServerTool.Name::explicit-tool-naming
}
@package::Zero.Mcp.Extensions.2.1.0.nupkg✅

### superseded in place on 2026-09-17 (moved here verbatim)

@nuget::Zero.Mcp.Extensions{2.0.0+2.1.0:743-dl:0-issues:0-PRs}
⏸phase6::ToolFiltering{ready-to-resume}
