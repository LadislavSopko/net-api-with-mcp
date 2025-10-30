§MBEL:5.0
@purpose::AIMemoryEncoding{compression%75,fidelity%100}

[FOCUS]
⚡phase6::RoleBasedToolFiltering{tools/list→filter-by-permissions}!
@status::Planning{TDDAB:metadata+filter+interceptor+integration}
@trigger::UX-issue{viewer-sees-unusable-tools}
@solution::ToolMetadata+IToolFilter+ToolsListInterceptor

✅phase5::LibraryExtraction{Zero.Mcp.Extensions→NuGet}✅
@status::COMPLETE{v1.9.0:production-ready}
@architecture::IAuthForMcpSupplier{10/10:zen-validated+security-hardened}⭐

[RECENT]
✅phase5::COMPLETE{library-extraction+tests+config-system}✅!
>implemented::Zero.Mcp.Extensions{v1.9.0:all-features}✅
>added::ZeroMcpOptions{RequireAuth+UseAuth+Path+Assembly}✅
>fixed::Tests{36→44:client-credentials→password-flow}✅
>added::UserRole.Viewer{0:read-only-access}✅!
>added::User{viewer:viewer123:ID-102}✅
>tested::Viewer{8-tests:4-MCP+4-HTTP}✅!
>created::do-login-poc.sh{auto-login+update-.mcp.json}✅
>recreated::KeycloakDB{fresh-realm-with-viewer}✅
©User>identified::UX-issue{tools/list-shows-unauthorized-tools}!
@next::Implement-tool-filtering{TDDAB:4-blocks}

[CURRENT]
@status::Phase6-Planning{tool-filtering-by-permissions}⚡
@tests::44-total{36-core+8-viewer-role}✅
@users::6{viewer+alice+bob+carol+admin+user}
@roles::4{Viewer:0+Member:1+Manager:2+Admin:3}
@challenge::Filter-tools/list{show-only-authorized-tools}

[DECISIONS]
@library-name::Zero.Mcp.Extensions{professional-branding}✨
@versioning::v1.9.0{published}✅
@roles::4-tier{Viewer:0→Member:1→Manager:2→Admin:3}!
@viewer::Read-only{¬create¬update¬promote}✅
@users::6-total{viewer+alice+bob+carol+admin+user}
@tests::44{36-core+8-viewer}✅
@keycloak::Recreated{fresh-DB:with-viewer}✅
@scripts::do-login-poc.sh{auto-login+mcp-update}✅
@test-auth::Password-flow{¬client-credentials:needs-user-context}!
@next-feature::Tool-filtering{metadata-based:TDDAB-4-blocks}⚡

[KEY_LEARNING]
!brilliant::¬HttpContext-in-interface{host-uses-IHttpContextAccessor-internally}⭐⭐⭐
!security::GetCustomAttribute→GetCustomAttributes{ALL-attrs-enforced}⭐⭐
!parameter-binding::JSON-name-based>positional{robust+flexible}⭐
!design-pattern::clean-separation{library:mechanics|host:dependencies}⭐⭐
!role-hierarchy::MinimumRole-pattern{higher≥lower:Manager-can-create}⭐
!test-auth::Password-flow{client-credentials:¬user-context}!
!keycloak-public-client::¬client-secret{PKCE-enabled}✅
!viewer-role::Essential{UX:read-only-users-need-access}⭐
!ux-issue::tools/list-shows-all{BAD:user-sees-unauthorized-tools}!
@solution::Metadata-filtering{runtime:based-on-user-role}⚡
