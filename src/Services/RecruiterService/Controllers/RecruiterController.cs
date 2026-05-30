using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using RecruiterService.Models;
using RecruiterService.Services;
using System.Security.Claims;

namespace RecruiterService.Controllers;

[ApiController]
[Route("api/recruiter")]
[Authorize(Roles = "Recruiter,Admin")]
public class RecruiterController(IRecruiterService recruiterService) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("profile")]
    public async Task<IActionResult> GetProfile()
    {
        var profile = await recruiterService.GetProfileAsync(CurrentUserId);
        return profile is null ? NotFound() : Ok(profile);
    }

    [HttpPost("profile")]
    public async Task<IActionResult> CreateProfile([FromBody] CreateProfileRequest request)
    {
        var profile = await recruiterService.CreateProfileAsync(CurrentUserId, request);
        return Ok(profile);
    }

    [HttpPut("profile")]
    public async Task<IActionResult> UpdateProfile([FromBody] UpdateProfileRequest request)
    {
        var profile = await recruiterService.UpdateProfileAsync(CurrentUserId, request);
        return Ok(profile);
    }

    [HttpPost("pipeline")]
    public async Task<IActionResult> AddToPipeline([FromBody] AddToPipelineRequest request)
    {
        var entry = await recruiterService.AddToPipelineAsync(CurrentUserId, request);
        return Ok(entry);
    }

    [HttpGet("pipeline/{jobId:guid}")]
    public async Task<IActionResult> GetPipeline(Guid jobId, [FromQuery] string? stage)
    {
        var pipeline = await recruiterService.GetPipelineAsync(jobId, CurrentUserId, stage);
        return Ok(pipeline);
    }

    [HttpPatch("pipeline/{id:guid}/stage")]
    public async Task<IActionResult> UpdateStage(Guid id, [FromBody] UpdatePipelineStageRequest request)
    {
        var entry = await recruiterService.UpdateStageAsync(id, CurrentUserId, request);
        return Ok(entry);
    }

    [HttpPost("pipeline/{id:guid}/view-resume")]
    public async Task<IActionResult> ViewResume(Guid id)
    {
        var result = await recruiterService.ViewResumeAsync(id, CurrentUserId);
        return Ok(result);
    }

    [HttpGet("pipeline/{id:guid}/resume-file")]
    public async Task<IActionResult> DownloadResumeFile(Guid id)
    {
        var (data, contentType, fileName) = await recruiterService.GetResumeFileAsync(id, CurrentUserId);
        return File(data, contentType, fileName);
    }
}
