using McpPoc.Api.Authorization;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;

namespace McpPoc.Api.Controllers;

/// <summary>
/// Scanned controller for the inherited-tool regression: it declares the STRICTER policy and inherits
/// <see cref="ReportsControllerBase.Summary"/> without overriding it. The class-level RequireAdmin policy
/// must reach the tool metadata, so only Admin may list or call <c>admin_reports_summary</c>.
/// </summary>
[Route("api/admin-reports")]
[Authorize(Policy = PolicyNames.RequireAdmin)]
[McpServerToolType]
public sealed class AdminReportsController : ReportsControllerBase
{
    // Summary is inherited and deliberately NOT overridden.
}
