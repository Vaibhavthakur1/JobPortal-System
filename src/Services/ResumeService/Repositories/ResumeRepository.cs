using Microsoft.EntityFrameworkCore;
using ResumeService.Data;
using ResumeService.Models;

namespace ResumeService.Repositories;

public class ResumeRepository(ResumeDbContext db) : IResumeRepository
{
    private IQueryable<Resume> FullResume => db.Resumes
        .Include(r => r.Personal)
        .Include(r => r.Educations)
        .Include(r => r.Experiences)
        .Include(r => r.Projects);

    public async Task<Resume?> GetByIdAsync(Guid id) =>
        await FullResume.FirstOrDefaultAsync(r => r.Id == id);

    public async Task<IEnumerable<Resume>> GetByUserAsync(Guid userId) =>
        await FullResume.Where(r => r.UserId == userId).OrderByDescending(r => r.CreatedAt).ToListAsync();

    public async Task<Resume?> GetDefaultAsync(Guid userId) =>
        await FullResume.FirstOrDefaultAsync(r => r.UserId == userId && r.IsDefault);

    public async Task AddAsync(Resume resume)
    {
        await db.Resumes.AddAsync(resume);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(Resume resume)
    {
        resume.UpdatedAt = DateTime.UtcNow;
        db.Resumes.Update(resume);
        await db.SaveChangesAsync();
    }

    public async Task DeleteAsync(Guid id)
    {
        var resume = await db.Resumes.FindAsync(id);
        if (resume is not null) { db.Resumes.Remove(resume); await db.SaveChangesAsync(); }
    }
}
