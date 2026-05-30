using MassTransit;
using RecruiterService.Models;
using RecruiterService.Repositories;
using System.Text.Json;

namespace RecruiterService.Services;

public class RecruiterService(
    IRecruiterRepository repo,
    IPublishEndpoint publisher,
    IHttpClientFactory httpClientFactory,
    IConfiguration config,
    ILogger<RecruiterService> logger) : IRecruiterService
{
    private static readonly JsonSerializerOptions JsonOpts = new() { PropertyNameCaseInsensitive = true };

    public async Task<RecruiterProfileDto> CreateProfileAsync(Guid userId, CreateProfileRequest req)
    {
        var existing = await repo.GetProfileAsync(userId);
        if (existing is not null) throw new InvalidOperationException("Profile already exists.");

        var profile = new RecruiterProfile
        {
            UserId = userId,
            CompanyName = req.CompanyName,
            CompanyDescription = req.CompanyDescription,
            Industry = req.Industry,
            Website = req.Website,
            Location = req.Location
        };

        await repo.AddProfileAsync(profile);
        return MapToDto(profile);
    }

    public async Task<RecruiterProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest req)
    {
        var profile = await repo.GetProfileAsync(userId)
            ?? throw new KeyNotFoundException("Profile not found.");

        if (req.CompanyName is not null) profile.CompanyName = req.CompanyName;
        if (req.CompanyDescription is not null) profile.CompanyDescription = req.CompanyDescription;
        if (req.Industry is not null) profile.Industry = req.Industry;
        if (req.Website is not null) profile.Website = req.Website;
        if (req.Location is not null) profile.Location = req.Location;

        await repo.UpdateProfileAsync(profile);
        return MapToDto(profile);
    }

    public async Task<RecruiterProfileDto?> GetProfileAsync(Guid userId)
    {
        var profile = await repo.GetProfileAsync(userId);
        return profile is null ? null : MapToDto(profile);
    }

    public async Task<PipelineDto> AddToPipelineAsync(Guid recruiterId, AddToPipelineRequest req)
    {
        var existing = await repo.GetPipelineByApplicationAsync(req.ApplicationId, recruiterId);
        if (existing is not null) throw new InvalidOperationException("Candidate already in pipeline.");

        var entry = new CandidatePipeline
        {
            RecruiterId = recruiterId,
            JobId = req.JobId,
            CandidateId = req.CandidateId,
            ApplicationId = req.ApplicationId
        };

        await repo.AddPipelineEntryAsync(entry);
        return MapPipelineDto(entry);
    }

    public async Task<PipelineDto> UpdateStageAsync(Guid pipelineId, Guid recruiterId, UpdatePipelineStageRequest req)
    {
        var entry = await repo.GetPipelineEntryAsync(pipelineId)
            ?? throw new KeyNotFoundException("Pipeline entry not found.");
        if (entry.RecruiterId != recruiterId) throw new UnauthorizedAccessException("Not authorized.");

        entry.Stage = req.Stage;
        entry.Notes = req.Notes ?? entry.Notes;
        await repo.UpdatePipelineEntryAsync(entry);
        return MapPipelineDto(entry);
    }

    public async Task<IEnumerable<PipelineDto>> GetPipelineAsync(Guid jobId, Guid recruiterId, string? stageFilter)
    {
        var entries = await repo.GetPipelineByJobAsync(jobId, recruiterId);
        if (!string.IsNullOrEmpty(stageFilter))
            entries = entries.Where(e => e.Stage == stageFilter);
        return entries.Select(MapPipelineDto);
    }

    public async Task<CandidateResumeView> ViewResumeAsync(Guid pipelineId, Guid recruiterId)
    {
        var entry = await repo.GetPipelineEntryAsync(pipelineId)
            ?? throw new KeyNotFoundException("Pipeline entry not found.");
        if (entry.RecruiterId != recruiterId) throw new UnauthorizedAccessException("Not authorized.");

        var now = DateTime.UtcNow;
        bool isAccessActive = entry.ResumeViewed &&
                              entry.ResumeAccessExpiresAt.HasValue &&
                              entry.ResumeAccessExpiresAt.Value > now;

        // Only deduct points if never viewed OR access has expired
        if (!isAccessActive)
        {
            await DeductPointsAsync(recruiterId, PointsCost.ResumeView, "Resume view");
            entry.ResumeViewed = true;
            entry.ResumeViewedAt = now;
            entry.ResumeAccessExpiresAt = now.AddDays(PointsCost.ResumeAccessDays);
            await repo.UpdatePipelineEntryAsync(entry);
            isAccessActive = true;
        }

        // Fetch resume data from ResumeService
        return await FetchCandidateResumeAsync(entry.CandidateId, isAccessActive, entry.ResumeAccessExpiresAt);
    }

    public async Task<(byte[] Data, string ContentType, string FileName)> GetResumeFileAsync(Guid pipelineId, Guid recruiterId)
    {
        var entry = await repo.GetPipelineEntryAsync(pipelineId)
            ?? throw new KeyNotFoundException("Pipeline entry not found.");
        if (entry.RecruiterId != recruiterId) throw new UnauthorizedAccessException("Not authorized.");

        var client = httpClientFactory.CreateClient("ResumeService");
        // Use the internal candidate default endpoint to get the resume id, then download
        var metaResponse = await client.GetAsync($"/api/resumes/candidate/{entry.CandidateId}/default");
        if (!metaResponse.IsSuccessStatusCode)
            throw new InvalidOperationException("Resume not found.");

        var json = await metaResponse.Content.ReadAsStringAsync();
        var resume = JsonSerializer.Deserialize<ResumeDto>(json, JsonOpts)
            ?? throw new InvalidOperationException("Failed to read resume.");

        if (resume.ResumeType != "Uploaded")
            throw new InvalidOperationException("This resume is not an uploaded file.");

        // Proxy the file download through ResumeService using a service-level token
        // For now we call the internal download endpoint directly (no auth needed for internal calls)
        var fileResponse = await client.GetAsync($"/api/resumes/{resume.Id}/download-uploaded-internal");
        if (!fileResponse.IsSuccessStatusCode)
            throw new InvalidOperationException("Failed to download resume file.");

        var data = await fileResponse.Content.ReadAsByteArrayAsync();
        var contentType = fileResponse.Content.Headers.ContentType?.MediaType ?? "application/pdf";
        var fileName = resume.UploadedFileName ?? $"resume_{resume.Id}.pdf";
        return (data, contentType, fileName);
    }

    // ─── Private Helpers ─────────────────────────────────────────────────────

    private async Task<CandidateResumeView> FetchCandidateResumeAsync(
        Guid candidateId, bool isFullAccess, DateTime? accessExpiresAt)
    {
        try
        {
            var client = httpClientFactory.CreateClient("ResumeService");
            var response = await client.GetAsync($"/api/resumes/candidate/{candidateId}/default");

            logger.LogInformation("ResumeService response for candidate {CandidateId}: {Status}",
                candidateId, response.StatusCode);

            if (!response.IsSuccessStatusCode)
            {
                logger.LogWarning("ResumeService returned {Status} for candidate {CandidateId}",
                    response.StatusCode, candidateId);
                return BuildEmptyResumeView(candidateId, isFullAccess, accessExpiresAt);
            }

            var json = await response.Content.ReadAsStringAsync();
            logger.LogInformation("ResumeService raw JSON: {Json}", json);

            var resume = JsonSerializer.Deserialize<ResumeDto>(json, JsonOpts);
            if (resume is null)
            {
                logger.LogWarning("Failed to deserialize resume for candidate {CandidateId}", candidateId);
                return BuildEmptyResumeView(candidateId, isFullAccess, accessExpiresAt);
            }

            return new CandidateResumeView(
                CandidateId: candidateId,
                IsFullAccess: isFullAccess,
                AccessExpiresAt: accessExpiresAt,
                FullName: resume.Personal?.FullName ?? "Unknown",
                Summary: resume.Personal?.Summary,
                Skills: resume.Skills ?? [],
                Experiences: (resume.Experiences ?? []).Select(e => new ExperiencePreview(
                    e.JobTitle, e.Company, e.Location, e.StartDate, e.EndDate, e.IsCurrent)).ToList(),
                Educations: (resume.Educations ?? []).Select(e => new EducationPreview(
                    e.Degree, e.FieldOfStudy, e.Institution, e.StartDate, e.EndDate)).ToList(),
                Email: resume.Personal?.Email,
                Phone: resume.Personal?.Phone,
                LinkedInUrl: resume.Personal?.LinkedInUrl,
                GitHubUrl: resume.Personal?.GitHubUrl,
                ResumeType: resume.ResumeType,
                ResumeId: resume.Id,
                UploadedFileName: resume.UploadedFileName
            );
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error fetching resume for candidate {CandidateId}", candidateId);
            return BuildEmptyResumeView(candidateId, isFullAccess, accessExpiresAt);
        }
    }

    private static CandidateResumeView BuildEmptyResumeView(
        Guid candidateId, bool isFullAccess, DateTime? accessExpiresAt) =>
        new(candidateId, isFullAccess, accessExpiresAt,
            "N/A", null, [], [], [],
            null, null, null, null);

    private async Task DeductPointsAsync(Guid recruiterId, int points, string reason)
    {
        var client = httpClientFactory.CreateClient("PaymentService");
        var response = await client.PostAsJsonAsync("/api/payment/deduct", new
        {
            RecruiterId = recruiterId,
            Points = points,
            Reason = reason
        });

        if (!response.IsSuccessStatusCode)
        {
            var error = await response.Content.ReadAsStringAsync();
            throw new InvalidOperationException($"Points deduction failed: {error}");
        }
    }

    private static RecruiterProfileDto MapToDto(RecruiterProfile p) =>
        new(p.Id, p.UserId, p.CompanyName, p.Industry, p.Website, p.Location, p.CreatedAt);

    private static PipelineDto MapPipelineDto(CandidatePipeline p)
    {
        var now = DateTime.UtcNow;
        var isActive = p.ResumeViewed &&
                       p.ResumeAccessExpiresAt.HasValue &&
                       p.ResumeAccessExpiresAt.Value > now;

        return new(p.Id, p.RecruiterId, p.JobId, p.CandidateId, p.ApplicationId,
            p.Stage, p.Notes,
            p.ResumeViewed, p.ResumeViewedAt, p.ResumeAccessExpiresAt,
            isActive,
            p.IsWithdrawn, p.WithdrawnAt,
            p.CreatedAt);
    }
}

// Internal DTOs matching ResumeService's ResumeDto JSON shape exactly
file record ResumeDto(
    Guid Id, Guid UserId, string Title, string Template, bool IsDefault,
    string? ResumeType, string? UploadedFileName,
    PersonalInfoDto? Personal,
    List<EducationDto>? Educations,
    List<ExperienceDto>? Experiences,
    List<string>? Skills,
    List<ProjectDto>? Projects,
    DateTime CreatedAt);

file record PersonalInfoDto(
    string FullName, string Email, string Phone, string Location,
    string? LinkedInUrl, string? GitHubUrl, string? Summary);

file record ExperienceDto(
    Guid? Id, string Company, string JobTitle, string Location,
    DateTime StartDate, DateTime? EndDate, bool IsCurrent, string Description);

file record EducationDto(
    Guid? Id, string Institution, string Degree, string FieldOfStudy,
    DateTime StartDate, DateTime? EndDate, bool IsCurrent, string? Grade);

file record ProjectDto(Guid? Id, string Name, string Description, string? Url, List<string> Technologies);
