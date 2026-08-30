using Microsoft.AspNetCore.Authorization;using Microsoft.AspNetCore.Mvc;using Microsoft.EntityFrameworkCore;using VirtualTryOn.Api.Constants;using VirtualTryOn.Api.Data;
namespace VirtualTryOn.Api.Controllers;
[ApiController,Authorize(Roles=UserRoles.Admin),Route("api/admin/security")]
public sealed class AdminSecurityController(AppDbContext db):ControllerBase
{
 [HttpGet("login-attempts")]public async Task<IActionResult> Attempts([FromQuery]int limit=200,CancellationToken token=default)=>Ok(await db.LoginAttempts.AsNoTracking().OrderByDescending(x=>x.CreatedAt).Take(Math.Clamp(limit,1,500)).Select(x=>new{x.Id,x.UserId,x.Email,x.Succeeded,x.FailureReason,x.IpAddress,x.UserAgent,x.CreatedAt}).ToListAsync(token));
}
