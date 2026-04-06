using Microsoft.EntityFrameworkCore;
using RecruiterService.Data;
using RecruiterService.Models;

namespace RecruiterService.Repositories;

public class RecruiterRepository(RecruiterDbContext db) : IRecruiterRepository
{
    public async Task<RecruiterProfile?> GetProfileAsync(Guid userId) =>
        await db.Profiles.FirstOrDefaultAsync(p => p.UserId == userId);

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
        await db.Pipelines.FindAsync(id);

    public async Task<IEnumerable<CandidatePipeline>> GetPipelineByJobAsync(Guid jobId, Guid recruiterId) =>
        await db.Pipelines
            .Where(p => p.JobId == jobId && p.RecruiterId == recruiterId)
            .OrderByDescending(p => p.CreatedAt)
            .ToListAsync();

    public async Task<CandidatePipeline?> GetPipelineByApplicationAsync(Guid applicationId, Guid recruiterId) =>
        await db.Pipelines.FirstOrDefaultAsync(p => p.ApplicationId == applicationId && p.RecruiterId == recruiterId);

    public async Task AddPipelineEntryAsync(CandidatePipeline entry)
    {
        await db.Pipelines.AddAsync(entry);
        await db.SaveChangesAsync();
    }

    public async Task UpdatePipelineEntryAsync(CandidatePipeline entry)
    {
        entry.UpdatedAt = DateTime.UtcNow;
        db.Pipelines.Update(entry);
        await db.SaveChangesAsync();
    }
}
