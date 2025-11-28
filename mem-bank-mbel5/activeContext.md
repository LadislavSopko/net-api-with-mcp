§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[FOCUS]
⚡phase6::RoleBasedToolFiltering{tools/list→filter-by-permissions}!
@status::Ready-to-implement{TDDAB-v2:simplified:3-blocks}⚡
@trigger::UX-issue{viewer-sees-unusable-tools}
@solution::SDK-AddListToolsFilter{¬decorator-pattern}✅

✅phase5::LibraryExtraction{Zero.Mcp.Extensions→NuGet}✅
@status::COMPLETE{v1.9.0:production-ready}

[RECENT]
>investigated::SDK-internals{AddListToolsFilter:exists}✅!
>discovered::SDK-filter-hook{request.User+request.Services:available}⭐⭐
>refactored::TDDAB-plan{v1→v2:4-blocks→3-blocks:~400→~110-lines}✅
>abandoned::Decorator-pattern{IMcpServer:risky+complex}
>adopted::SDK-filter{AddListToolsFilter:designed-for-this}✅
@plan::tasks/tddab-tool-filtering-by-permissions.md{v2:simplified}

[CURRENT]
@status::Phase6-Ready{TDDAB-v2:3-blocks:~110-lines}⚡
@tests::44-total{36-core+8-viewer-role}✅
@target::59-tests{44-existing+15-new}
@users::6{viewer+alice+bob+carol+admin+user}
@roles::4{Viewer:0+Member:1+Manager:2+Admin:3}

[PHASE6_PLAN_V2]
@block1::ToolAuthorizationMetadata{record+store+extractor:5-tests}
@block2::ToolListFilter{SDK-hook+role-check:6-tests}
@block3::IntegrationTests{end-to-end:4-tests}
@total::~110-lines-new-code{simple+focused}

[ARCHITECTURE_SIMPLIFIED]
@startup::Scan-Authorize-attrs→store{toolName:minRole}
@runtime::SDK-filter→check{user.role≥tool.minRole}→return-filtered
@components::{
  ToolAuthorizationMetadata::record{toolName+minRole}
  ToolAuthorizationStore::dictionary-wrapper
  ToolListFilter::static-methods{FilterByRole+GetUserRole}
  AddListToolsFilter::SDK-built-in-hook⭐
}

[KEY_DISCOVERY]
!sdk-filter::AddListToolsFilter{request.User:ClaimsPrincipal+request.Services:DI}⭐⭐⭐
!no-decorator::IMcpServer-wrapping{unnecessary:SDK-provides-filter-hook}✅
!simple-over-complex::~110-lines>~400-lines{same-result}⭐

[DECISIONS]
@approach::SDK-filter{¬decorator:AddListToolsFilter}✅
@plan-version::v2{simplified:3-blocks}
@code-reduction::~75%{400→110-lines}
@test-count::15-new{5+6+4}
@config::FilterToolsByPermissions{default:true}

[NEXT]
?start::Block1{ToolAuthorizationMetadata+Store:5-tests}
?then::Block2{ToolListFilter+SDK-integration:6-tests}
?finally::Block3{Integration-tests:4-tests}
@command::ACT{to-start-implementation}
