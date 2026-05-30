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

    // Internal endpoint called by RecruiterService
    [HttpGet("candidate/{userId:guid}/default")]
    [AllowAnonymous]
    public async Task<IActionResult> GetDefaultByUser(Guid userId)
    {
        var resumes = await resumeService.GetMyResumesAsync(userId);
        var def = resumes.FirstOrDefault(r => r.IsDefault) ?? resumes.FirstOrDefault();
        return def is null ? NotFound() : Ok(def);
    }

    /// <summary>Upload a PDF/DOC/DOCX resume file. Returns a ResumeDto that can be used when applying.</summary>
    [HttpPost("upload")]
    [Consumes("multipart/form-data")]
    public async Task<IActionResult> Upload([FromForm] string? title, IFormFile file)
    {
        if (file is null || file.Length == 0)
            return BadRequest(new { message = "No file provided." });

        var resume = await resumeService.UploadAsync(CurrentUserId, title ?? string.Empty, file);
        return CreatedAtAction(nameof(GetResume), new { id = resume.Id }, resume);
    }

    /// <summary>Download the original uploaded file for an uploaded resume.</summary>
    [HttpGet("{id:guid}/download-uploaded")]
    public async Task<IActionResult> DownloadUploaded(Guid id)
    {
        var (data, contentType, fileName) = await resumeService.DownloadUploadedAsync(id, CurrentUserId);
        return File(data, contentType, fileName);
    }

    /// <summary>Internal service-to-service endpoint — no auth required.</summary>
    [HttpGet("{id:guid}/download-uploaded-internal")]
    [AllowAnonymous]
    public async Task<IActionResult> DownloadUploadedInternal(Guid id)
    {
        var resume = await resumeService.GetByIdAsync(id);
        if (resume is null) return NotFound();
        // Use a dummy userId — internal call bypasses ownership check
        var (data, contentType, fileName) = await resumeService.DownloadUploadedInternalAsync(id);
        return File(data, contentType, fileName);
    }
}
