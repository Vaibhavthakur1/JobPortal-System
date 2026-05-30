using Microsoft.EntityFrameworkCore;
using RecruiterService.Data;
using RecruiterService.Models;

namespace RecruiterService.Repositories;

public class RecruiterRepository(RecruiterDbContext db) : IRecruiterRepository
{
    public async Task<RecruiterProfile?> GetProfileAsync(Guid userId) =>
        await db.Profiles.AsNoTracking().FirstOrDefaultAsync(p => p.UserId == userId);

    public async Task AddProfileAsync(RecruiterProfile profile)
    {
        await db.Profiles.AddAsync(profile);
        await db.SaveChangesAsync();
    }

    public async Task UpdateProfileAsync(RecruiterProfile profile)
    {
        profile.UpdatedAt = DateTime.UtcNow;
        db.Profiles.Update(profile);
        await db.SaveChangesAsync();
    }

    public async Task<CandidatePipeline?> GetPipelineEntryAsync(Guid id) =>
        await db.Pipelines.AsNoTracking().FirstOrDefaultAsync(p => p.Id == id);

    public async Task<IEnumerable<CandidatePipeline>> GetPipelineByJobAsync(Guid jobId, Guid recruiterId) =>
        await db.Pipelines
            .AsNoTracking()
            .Where(p => p.JobId == jobId && p.RecruiterId == recruiterId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<CandidatePipeline?> GetPipelineByApplicationAsync(Guid applicationId, Guid recruiterId) =>
        await db.Pipelines.AsNoTracking()
            .FirstOrDefaultAsync(p => p.ApplicationId == applicationId && p.RecruiterId == recruiterId);

    public async Task AddPipelineEntryAsync(CandidatePipeline entry)
    {
        await db.Pipelines.AddAsync(entry);
        await db.SaveChangesAsync();
    }

    public async Task UpdatePipelineEntryAsync(CandidatePipeline entry)
    {
        entry.UpdatedAt = DateTime.UtcNow;
        // Use ExecuteUpdate to avoid EF tracking conflicts (entity loaded with AsNoTracking)
        await db.Pipelines
            .Where(p => p.Id == entry.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(p => p.Stage, entry.Stage)
                .SetProperty(p => p.Notes, entry.Notes)
                .SetProperty(p => p.ResumeViewed, entry.ResumeViewed)
                .SetProperty(p => p.ResumeViewedAt, entry.ResumeViewedAt)
                .SetProperty(p => p.ResumeAccessExpiresAt, entry.ResumeAccessExpiresAt)
                .SetProperty(p => p.ContactUnlocked, entry.ContactUnlocked)
                .SetProperty(p => p.ContactUnlockedAt, entry.ContactUnlockedAt)
                .SetProperty(p => p.UpdatedAt, entry.UpdatedAt));
    }
}
