using ApplicationService.Models;
using ApplicationService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace ApplicationService.Controllers;

[ApiController]
[Route("api/applications")]
[Authorize]
public class ApplicationsController(IApplicationService appService) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [Authorize(Roles = "JobSeeker")]
    [HttpPost]
    public async Task<IActionResult> Submit([FromBody] SubmitApplicationRequest request)
    {
        var result = await appService.SubmitApplicationAsync(CurrentUserId, request);
        return CreatedAtAction(nameof(GetApplication), new { id = result.Id }, result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetApplication(Guid id)
    {
        var result = await appService.GetApplicationAsync(id);
        return result is null ? NotFound() : Ok(result);
    }

    [Authorize(Roles = "JobSeeker")]
    [HttpGet("my")]
    public async Task<IActionResult> MyApplications()
    {
        var result = await appService.GetMyApplicationsAsync(CurrentUserId);
        return Ok(result);
    }

    [Authorize(Roles = "JobSeeker")]
    [HttpPatch("{id:guid}/withdraw")]
    public async Task<IActionResult> Withdraw(Guid id)
    {
        await appService.WithdrawAsync(id, CurrentUserId);
        return NoContent();
    }

    [Authorize(Roles = "Recruiter,Admin")]
    [HttpPatch("{id:guid}/status")]
    public async Task<IActionResult> UpdateStatus(Guid id, [FromBody] UpdateStatusRequest request)
    {
        var result = await appService.UpdateStatusAsync(id, CurrentUserId, request);
        return Ok(result);
    }

    [Authorize(Roles = "Recruiter,Admin")]
    [HttpGet("job/{jobId:guid}")]
    public async Task<IActionResult> GetApplicants(Guid jobId)
    {
        var result = await appService.GetJobApplicantsAsync(jobId, CurrentUserId);
        return Ok(result);
    }
}
