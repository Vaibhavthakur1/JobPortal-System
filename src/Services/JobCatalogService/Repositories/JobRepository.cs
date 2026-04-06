using JobCatalogService.Data;
using JobCatalogService.Models;
using Microsoft.EntityFrameworkCore;

namespace JobCatalogService.Repositories;

public class JobRepository(JobCatalogDbContext db) : IJobRepository
{
    public async Task<Job?> GetByIdAsync(Guid id) =>
        await db.Jobs.FirstOrDefaultAsync(j => j.Id == id);

    public async Task<PagedResult<Job>> SearchAsync(JobSearchRequest req)
    {
        var query = db.Jobs.Where(j => j.Status == "Active").AsQueryable();

        if (!string.IsNullOrWhiteSpace(req.Keyword))
            query = query.Where(j => j.Title.Contains(req.Keyword) || j.Description.Contains(req.Keyword) || j.Company.Contains(req.Keyword));

        if (!string.IsNullOrWhiteSpace(req.Location))
            query = query.Where(j => j.Location.Contains(req.Location));

        if (!string.IsNullOrWhiteSpace(req.JobType))
            query = query.Where(j => j.JobType == req.JobType);

        if (!string.IsNullOrWhiteSpace(req.Industry))
            query = query.Where(j => j.Industry == req.Industry);

        if (req.MinSalary.HasValue)
            query = query.Where(j => j.SalaryMin >= req.MinSalary);

        if (req.MaxSalary.HasValue)
            query = query.Where(j => j.SalaryMax <= req.MaxSalary);

        if (req.MinExperience.HasValue)
            query = query.Where(j => j.ExperienceYears >= req.MinExperience);

        var total = await query.CountAsync();
        var items = await query
            .OrderByDescending(j => j.CreatedAt)
            .Skip((req.Page - 1) * req.PageSize)
            .Take(req.PageSize)
            .ToListAsync();

        return new PagedResult<Job>(items, total, req.Page, req.PageSize);
    }

    public async Task<IEnumerable<Job>> GetByRecruiterAsync(Guid recruiterId) =>
        await db.Jobs.Where(j => j.RecruiterId == recruiterId).OrderByDescending(j => j.CreatedAt).ToListAsync();

    public async Task AddAsync(Job job) { await db.Jobs.AddAsync(job); await db.SaveChangesAsync(); }

    public async Task UpdateAsync(Job job) { job.UpdatedAt = DateTime.UtcNow; db.Jobs.Update(job); await db.SaveChangesAsync(); }

    public async Task<bool> ExistsAsync(Guid id) => await db.Jobs.AnyAsync(j => j.Id == id);
}
