using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using ResumeService.Models;
using ResumeService.Services;
using System.Security.Claims;

namespace ResumeService.Controllers;

[ApiController]
[Route("api/resumes")]
[Authorize]
public class ResumeController(IResumeService resumeService) : ControllerBase
{
    private Guid CurrentUserId => Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);

    [HttpGet]
    public async Task<IActionResult> GetMyResumes()
    {
        var resumes = await resumeService.GetMyResumesAsync(CurrentUserId);
        return Ok(resumes);
    }

    [HttpGet("{id:guid}")]
    public async Task<IActionResult> GetResume(Guid id)
    {
        var resume = await resumeService.GetByIdAsync(id);
        return resume is null ? NotFound() : Ok(resume);
    }

    [HttpPost]
    public async Task<IActionResult> Create([FromBody] CreateResumeRequest request)
    {
        var resume = await resumeService.CreateAsync(CurrentUserId, request);
        return CreatedAtAction(nameof(GetResume), new { id = resume.Id }, resume);
    }

    [HttpPut("{id:guid}")]
    public async Task<IActionResult> Update(Guid id, [FromBody] UpdateResumeRequest request)
    {
        var resume = await resumeService.UpdateAsync(id, CurrentUserId, request);
        return Ok(resume);
    }

    [HttpDelete("{id:guid}")]
    public async Task<IActionResult> Delete(Guid id)
    {
        await resumeService.DeleteAsync(id, CurrentUserId);
        return NoContent();
    }

    [HttpPatch("{id:guid}/set-default")]
    public async Task<IActionResult> SetDefault(Guid id)
    {
        await resumeService.SetDefaultAsync(id, CurrentUserId);
        return NoContent();
    }

    [HttpGet("{id:guid}/export-pdf")]
    public async Task<IActionResult> ExportPdf(Guid id)
    {
        var pdfBytes = await resumeService.ExportPdfAsync(id, CurrentUserId);
        return File(pdfBytes, "application/octet-stream", $"resume_{id}.txt");
    }
}
