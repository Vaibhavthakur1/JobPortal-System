using MassTransit;
using RecruiterService.Models;
using RecruiterService.Repositories;
using Shared.Contracts.Events.Payment;

namespace RecruiterService.Services;

public class RecruiterService(
    IRecruiterRepository repo,
    IPublishEndpoint publisher,
    IHttpClientFactory httpClientFactory,
    IConfiguration config) : IRecruiterService
{
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

    public async Task<PipelineDto> ViewResumeAsync(Guid pipelineId, Guid recruiterId)
    {
        var entry = await repo.GetPipelineEntryAsync(pipelineId)
            ?? throw new KeyNotFoundException("Pipeline entry not found.");
        if (entry.RecruiterId != recruiterId) throw new UnauthorizedAccessException("Not authorized.");

        if (!entry.ResumeViewed)
        {
            await DeductPointsAsync(recruiterId, PointsCost.ResumeView, "Resume view");
            entry.ResumeViewed = true;
            await repo.UpdatePipelineEntryAsync(entry);
        }

        return MapPipelineDto(entry);
    }

    public async Task<PipelineDto> UnlockContactAsync(Guid pipelineId, Guid recruiterId)
    {
        var entry = await repo.GetPipelineEntryAsync(pipelineId)
            ?? throw new KeyNotFoundException("Pipeline entry not found.");
        if (entry.RecruiterId != recruiterId) throw new UnauthorizedAccessException("Not authorized.");

        if (!entry.ContactUnlocked)
        {
            await DeductPointsAsync(recruiterId, PointsCost.ContactUnlock, "Contact unlock");
            entry.ContactUnlocked = true;
            await repo.UpdatePipelineEntryAsync(entry);
        }

        return MapPipelineDto(entry);
    }

    private async Task DeductPointsAsync(Guid recruiterId, int points, string reason)
    {
        // Call PaymentService via HTTP to deduct points
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

    private static PipelineDto MapPipelineDto(CandidatePipeline p) =>
        new(p.Id, p.RecruiterId, p.JobId, p.CandidateId, p.ApplicationId,
            p.Stage, p.Notes, p.ResumeViewed, p.ContactUnlocked, p.CreatedAt);
}
