§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[FOCUS]
@state::DEVELOP{cvm:run-20260928-bug02:3/4-blocks}
@bug::02-inherited-tool-authorize{github-issue-1:authorization-bypass}
@branch::bugfix/02-inherited-tool-authorize
@bug-notes::tasks/02-inherited-tool-authorize/bug-notes.md
@bug-plan::tasks/02-inherited-tool-authorize/plan.md{4-blocks:reviewed✓:cvm-valid-28/28-red}

[BUG02_BLOCKS]
✅01::metadata-builder-uses-tooltype{COMPLETE:Build(MethodInfo,Type,bool):class-attrs←toolType¬DeclaringType:16-tests(7-existing+9-new):unit108+E2E59}✅
✅02::thread-tooltype-through-registration{COMPLETE:Create(method,toolType,services,serializerOptions,includeAuth):both-paths-pass-toolType:ReflectedType-bridge-REMOVED:4-new-tests:unit112+E2E59}✅
✅03::e2e-inherited-authorization{COMPLETE:ReportsControllerBase(abstract+[Authorize]+declares-tool)+AdminReportsController([Authorize(RequireAdmin)]+[McpServerToolType]+NO-override):admin_reports_summary:demo-9→10-tools:4-E2E+docs-4-places:unit112+E2E63}✅
⚡04::release-3-0-1{NEXT:Version.props+CHANGELOG+docs+PackageTests}

[BUG02_FACTS]
!rootcause::ToolMetadataBuilder{class-attrs←method.DeclaringType}→inherited-method:DeclaringType=BASE→derived-[Authorize]-LOST{GetCustomAttributes(inherit:true)-walks-UP-only}
!probe::DeclaringType→[ReaderOrAbove] | toolType→[AdminOnly,ReaderOrAbove]{runtime-verified}
!reflectedtype::MethodInfo.ReflectedType==scanned-type{even-for-inherited:probe-D2/B2}→used-as-block-01-bridge:block-02-replaces-with-explicit-param
!sdk-combine::AuthorizationFilterSetup{AuthorizationPolicy.CombineAsync(all-IAuthorizeData)}=MVC-semantics{derived+base-both-enforced}:vendored-3rdp/csharp-sdk@v2.2.0
!sdk-tryadd::McpServerOptionsSetup{toolCollection.TryAdd}→duplicate-tool-name-SILENTLY-DROPPED{¬exception}:MethodOnly+same-inherited-method=collision→use-ControllerPrefix-in-scan-fixtures
@affected::3.0.0{published-nuget:authorization-bypass-on-inherited-non-overridden-tools}⚠
@demo-tools::10{+admin_reports_summary:admin-only:inherited-from-ReportsControllerBase}:viewer6/member7/manager8/admin10
@version::3.0.0{published:nuget.org:2026-09-17:AFFECTED}⚠
@last-closed::01-upgrade-mcp-sdk-2{SDK-2.2.0-upgrade:9/9-blocks:details→history.md}

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
@nuget::Zero.Mcp.Extensions{2.0.0+2.1.0+3.0.0:743-dl-at-2.x:0-issues:0-PRs}

[SDK_STATUS]
@sdk-version::2.2.0{shipped:3.0.0}
!hack-still-needed::MarshalResult{ActionResult<T>:unwrapping:confirmed-on-2.2.0-source}✅
@sdk-2.2.0::HttpContext{FLOWS-into-tools:stateless=RequestServices+request-ExecutionContext:verified-block-07:IsMcpCall=true}✅

[DECISIONS]
@naming-default::MethodOnly{backward-compatible}
@naming-prefix::ControllerPrefix{removes-Controller-suffix+snake_case}
@context-lifetime::Scoped
@mcp-header::x-mcp-call{auto-injected:middleware}
@attribute-priority::[McpServerTool(Name)]>Convention{always}
@sdk-2.2.0::HttpContext{flows-into-tools:verified-E2E:path-fallback-kept}

[NEXT]
?followup::mcp-json-token{poc/poc-viewer/poc-member/poc-manager:bearer-expires-60min:regenerate-POST-http://127.0.0.1:8080/realms/mcppoc-realm/protocol/openid-connect/token{client_id=mcppoc-api:grant_type=password}→/mcp-reconnect}
?followup::get-token.sh{CRLF-line-endings:fails-from-Python-subprocess:works-bash-direct}⚠
?followup::MarshalResult-cleanup{Task/ValueTask-branches=dead-code:MEAI-awaits-before-marshaller:optional}
?followup::ast-grep{not-installed:lint-layer-2-skipped-exit-3}
?followup::parent-mcp-json{D:\Projekty\AI_Works\.mcp.json:placeholder-cvm-entry:overridden-by-project-entry}
