using AdminService.Models;
using AdminService.Repositories;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace AdminService.Controllers;

[ApiController]
[Route("api/admin")]
[Authorize(Roles = "Admin")]
public class AdminController(IAuditRepository auditRepo, IHttpClientFactory httpClientFactory) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    // Audit Logs
    [HttpGet("audit-logs")]
    public async Task<IActionResult> GetAuditLogs(
        [FromQuery] int page = 1, [FromQuery] int pageSize = 50,
        [FromQuery] Guid? userId = null, [FromQuery] string? entity = null)
    {
        var logs = await auditRepo.GetLogsAsync(page, pageSize, userId, entity);
        return Ok(logs);
    }

    // Job Flagging
    [HttpPost("flag-job")]
    public async Task<IActionResult> FlagJob([FromBody] FlagJobRequest request)
    {
        var flag = new FlaggedJob
        {
            JobId = request.JobId,
            FlaggedBy = CurrentUserId,
            Reason = request.Reason
        };
        await auditRepo.AddFlagAsync(flag);
        return Ok(new FlaggedJobDto(flag.Id, flag.JobId, flag.FlaggedBy, flag.Reason, flag.Status, flag.CreatedAt));
    }

    [HttpGet("flagged-jobs")]
    public async Task<IActionResult> GetFlaggedJobs([FromQuery] string? status, [FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var flags = await auditRepo.GetFlagsAsync(status, page, pageSize);
        return Ok(flags.Select(f => new FlaggedJobDto(f.Id, f.JobId, f.FlaggedBy, f.Reason, f.Status, f.CreatedAt)));
    }

    [HttpPatch("flagged-jobs/{id:guid}")]
    public async Task<IActionResult> ReviewFlag(Guid id, [FromBody] ReviewFlagRequest request)
    {
        var flag = await auditRepo.GetFlagAsync(id);
        if (flag is null) return NotFound();
        flag.Status = request.Status;
        flag.ReviewedAt = DateTime.UtcNow;
        await auditRepo.UpdateFlagAsync(flag);
        return Ok(new FlaggedJobDto(flag.Id, flag.JobId, flag.FlaggedBy, flag.Reason, flag.Status, flag.CreatedAt));
    }

    // User management — proxies to IdentityService
    [HttpGet("users")]
    public async Task<IActionResult> GetUsers([FromQuery] int page = 1, [FromQuery] int pageSize = 20)
    {
        var client = httpClientFactory.CreateClient("IdentityService");
        var response = await client.GetAsync($"/api/admin/users?page={page}&pageSize={pageSize}");
        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json");
    }

    [HttpPatch("users/{userId:guid}/role")]
    public async Task<IActionResult> UpdateUserRole(Guid userId, [FromBody] UpdateUserRoleRequest request)
    {
        var client = httpClientFactory.CreateClient("IdentityService");
        var response = await client.PatchAsJsonAsync($"/api/admin/users/{userId}/role", request);
        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json");
    }

    [HttpPatch("users/{userId:guid}/deactivate")]
    public async Task<IActionResult> DeactivateUser(Guid userId)
    {
        var client = httpClientFactory.CreateClient("IdentityService");
        var response = await client.PatchAsync($"/api/admin/users/{userId}/deactivate", null);
        var content = await response.Content.ReadAsStringAsync();
        return Content(content, "application/json");
    }
}
