§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[FOCUS]
⚡phase5::LibraryExtraction{McpApiExtensions→NuGet}!
@plan::tasks/tddab-mcpapi-extensions-library-v4.1-final.md{FINAL:8-blocks}✅
@status::PRODUCTION-READY{all-fixes+codebase-verified+interface-corrected}
@architecture::IAuthForMcpSupplier{10/10:zen-validated+security-hardened+¬HttpContext-param}⭐

[RECENT]
✓zenReview-v2::COMPLETE{gemini-2.5-pro:found-6-issues}!
>found::🔴CRITICAL{multiple-[Authorize]-security-vulnerability}
>found::🟠HIGH{null-in-ActionResult+positional-param-binding}
>fixed::Plan-v3{ALL-critical+high-priority-issues}✅
©User>verified::Codebase{MarshalResult-naming+source-file}
>created::Plan-v4{unified:v3+v3.1-mechanics}✅
©User>corrected::Interface{¬HttpContext-param:host-manages-own-dependencies}!
>created::Plan-v4.1{FINAL:interface-corrected}✅
@confidence::very-high{zen-validated+codebase-verified+interface-final}

[CURRENT]
@status::Planning{COMPLETE:v4-FINAL-UNIFIED}✅
@next::await-ACT{implement-TDDAB-1}
@tests::60-total{32-existing+18-library+4-supplier+2-package+4-integration}

[DECISIONS]
@interface::IAuthForMcpSupplier{HttpContext-param:library-¬-depends-on-IHttpContextAccessor}
@separation::Library{mechanics}+Host{domain+HttpContext+IAuthorizationService}
@cpm::Directory.Build.props+Directory.Packages.props{CPM:enabled}
@versioning::1.8.0{from:Version.props}
@tests::60-total{updated:32-existing+28-new}
!security::MultipleAuthorizeAttributes{GetCustomAttributes:NOT-GetCustomAttribute}
!robustness::NameBasedBinding{JSON-params:NOT-positional}
!correctness::NullInActionResult{Ok(null):valid-for-nullable}
@source::Extensions/McpServerBuilderExtensions.cs{delete-after-migration}
@naming::MarshalResult{NOT-ActionResultUnwrapper:use-actual-name}

[KEY_LEARNING]
!brilliant::¬HttpContext-in-library{host-manages-all-auth}⭐
!security::GetCustomAttribute→GetCustomAttributes{ALL-attrs-enforced}⭐⭐
!parameter-binding::JSON-name-based>positional{robust+flexible}⭐
!design-pattern::pass-HttpContext-as-param{NOT-IHttpContextAccessor-DI}⭐⭐
@verified::src/McpPoc.Api/Extensions/McpServerBuilderExtensions.cs{single-file-extraction}
