using System.ComponentModel;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ModelContextProtocol.Server;

namespace McpPoc.Api.Controllers;

/// <summary>
/// Base controller that DECLARES a tool method while carrying only the weak class-level policy
/// (authenticated user). Derived controllers tighten it with their own [Authorize] and deliberately do
/// not override the method — the shape that exposed the authorization bypass of GitHub issue #1.
/// It is abstract and carries no [McpServerToolType], so it is never scanned on its own.
/// </summary>
[ApiController]
[Authorize]
public abstract class ReportsControllerBase : ControllerBase
{
    /// <summary>
    /// Returns a report summary. Inherited as-is by the derived controllers.
    /// </summary>
    [HttpGet("summary")]
    [McpServerTool(Name = "admin_reports_summary"), Description("Returns a report summary")]
    public virtual ActionResult<ReportSummary> Summary() => Ok(new ReportSummary("ok", DateTime.UtcNow));
}

/// <summary>
/// Payload of the report summary tool.
/// </summary>
public record ReportSummary(string Status, DateTime GeneratedAt);
