using ApplicationService.Data;
using ApplicationService.Models;
using Microsoft.EntityFrameworkCore;

namespace ApplicationService.Repositories;

public class ApplicationRepository(ApplicationDbContext db) : IApplicationRepository
{
    public async Task<JobApplication?> GetByIdAsync(Guid id) =>
        await db.Applications
            .Include(a => a.StatusHistory)
            .AsNoTracking()
            .FirstOrDefaultAsync(a => a.Id == id);

    public async Task<IEnumerable<JobApplication>> GetByJobSeekerAsync(Guid jobSeekerId) =>
        await db.Applications
            .Include(a => a.StatusHistory)
            .AsNoTracking()
            .Where(a => a.JobSeekerId == jobSeekerId)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

    public async Task<IEnumerable<JobApplication>> GetByJobAsync(Guid jobId) =>
        await db.Applications
            .Include(a => a.StatusHistory)
            .AsNoTracking()
            .Where(a => a.JobId == jobId && !a.IsWithdrawn)
            .OrderByDescending(a => a.CreatedAt)
            .ToListAsync();

    public async Task<bool> HasAppliedAsync(Guid jobSeekerId, Guid jobId) =>
        await db.Applications.AnyAsync(a =>
            a.JobSeekerId == jobSeekerId && a.JobId == jobId && !a.IsWithdrawn);

    public async Task AddAsync(JobApplication app)
    {
        await db.Applications.AddAsync(app);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(JobApplication app)
    {
        // 1. Update scalar fields only — avoids EF concurrency/tracking issues
        await db.Applications
            .Where(a => a.Id == app.Id)
            .ExecuteUpdateAsync(s => s
                .SetProperty(a => a.Status, app.Status)
                .SetProperty(a => a.IsWithdrawn, app.IsWithdrawn)
                .SetProperty(a => a.UpdatedAt, DateTime.UtcNow));

        // 2. Insert only NEW history entries — those not yet in the DB
        //    Get existing IDs first, then insert only what's missing
        var existingIds = await db.StatusHistories
            .Where(h => h.ApplicationId == app.Id)
            .Select(h => h.Id)
            .ToListAsync();

        var newEntries = app.StatusHistory
            .Where(h => !existingIds.Contains(h.Id))
            .ToList();

        if (newEntries.Count > 0)
        {
            await db.StatusHistories.AddRangeAsync(newEntries);
            await db.SaveChangesAsync();
        }
    }
}
