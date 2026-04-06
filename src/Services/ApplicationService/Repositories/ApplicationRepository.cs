using ApplicationService.Data;
using ApplicationService.Models;
using Microsoft.EntityFrameworkCore;

namespace ApplicationService.Repositories;

public class ApplicationRepository(ApplicationDbContext db) : IApplicationRepository
{
    public async Task<JobApplication?> GetByIdAsync(Guid id) =>
        await db.Applications.Include(a => a.StatusHistory).FirstOrDefaultAsync(a => a.Id == id);

    public async Task<IEnumerable<JobApplication>> GetByJobSeekerAsync(Guid jobSeekerId) =>
        await db.Applications.Include(a => a.StatusHistory)
            .Where(a => a.JobSeekerId == jobSeekerId)
            .OrderByDescending(a => a.CreatedAt).ToListAsync();

    public async Task<IEnumerable<JobApplication>> GetByJobAsync(Guid jobId) =>
        await db.Applications.Include(a => a.StatusHistory)
            .Where(a => a.JobId == jobId && !a.IsWithdrawn)
            .OrderByDescending(a => a.CreatedAt).ToListAsync();

    public async Task<bool> HasAppliedAsync(Guid jobSeekerId, Guid jobId) =>
        await db.Applications.AnyAsync(a => a.JobSeekerId == jobSeekerId && a.JobId == jobId && !a.IsWithdrawn);

    public async Task AddAsync(JobApplication app) { await db.Applications.AddAsync(app); await db.SaveChangesAsync(); }

    public async Task UpdateAsync(JobApplication app) { app.UpdatedAt = DateTime.UtcNow; db.Applications.Update(app); await db.SaveChangesAsync(); }
}
