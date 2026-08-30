using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using VirtualTryOn.Api.Constants;
using VirtualTryOn.Api.Models;
using VirtualTryOn.Api.Services;

namespace VirtualTryOn.Api.Controllers;

[ApiController, Authorize(Roles = UserRoles.Admin), Route("api/admin/audit-logs")]
public sealed class AdminAuditLogsController(AdminAuditService auditService) : ControllerBase
{
    [HttpGet]
    public async Task<ActionResult<IReadOnlyList<AdminAuditLog>>> GetRecent(
        [FromQuery] int limit = 50, CancellationToken token = default) =>
        Ok(await auditService.GetRecentAsync(limit, token));
}
