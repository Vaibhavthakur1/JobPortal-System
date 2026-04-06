using JobCatalogService.Models;
using JobCatalogService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace JobCatalogService.Controllers;

[ApiController]
[Route("api/jobs")]
public class JobsController(IJobService jobService) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet("search")]
    public async Task<IActionResult> Search([FromQuery] JobSearchRequest request)
    {
        var result = await jobService.SearchJobsAsync(request);
        return Ok(result);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetJob(Guid id)
    {
        var job = await jobService.GetJobAsync(id);
        return job is null ? NotFound() : Ok(job);
    }

    [Authorize(Roles = "Recruiter,Admin")]
    [HttpPost]
    public async Task<IActionResult> CreateJob([FromBody] CreateJobRequest request)
    {
        var job = await jobService.CreateJobAsync(CurrentUserId, request);
        return CreatedAtAction(nameof(GetJob), new { id = job.Id }, job);
    }

    [Authorize(Roles = "Recruiter,Admin")]
    [HttpPut("{id:guid}")]
    public async Task<IActionResult> UpdateJob(Guid id, [FromBody] UpdateJobRequest request)
    {
        var job = await jobService.UpdateJobAsync(id, CurrentUserId, request);
        return Ok(job);
    }

    [Authorize(Roles = "Recruiter,Admin")]
    [HttpPatch("{id:guid}/close")]
    public async Task<IActionResult> CloseJob(Guid id)
    {
        await jobService.CloseJobAsync(id, CurrentUserId);
        return NoContent();
    }

    [Authorize(Roles = "Recruiter,Admin")]
    [HttpPatch("{id:guid}/archive")]
    public async Task<IActionResult> ArchiveJob(Guid id)
    {
        await jobService.ArchiveJobAsync(id, CurrentUserId);
        return NoContent();
    }

    [Authorize(Roles = "Recruiter")]
    [HttpGet("my-listings")]
    public async Task<IActionResult> MyListings()
    {
        var jobs = await jobService.GetRecruiterJobsAsync(CurrentUserId);
        return Ok(jobs);
    }
}
