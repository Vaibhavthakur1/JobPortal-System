using AdminService.Data;
using AdminService.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Repositories;

public class AuditRepository(AdminDbContext db) : IAuditRepository
{
    public async Task LogAsync(AuditLog log)
    {
        await db.AuditLogs.AddAsync(log);
        await db.SaveChangesAsync();
    }

    public async Task<IEnumerable<AuditLog>> GetLogsAsync(int page, int pageSize, Guid? userId = null, string? entity = null)
    {
        var query = db.AuditLogs.AsQueryable();
        if (userId.HasValue) query = query.Where(l => l.UserId == userId);
        if (!string.IsNullOrEmpty(entity)) query = query.Where(l => l.Entity == entity);
        return await query.OrderByDescending(l => l.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }

    public async Task AddFlagAsync(FlaggedJob flag) { await db.FlaggedJobs.AddAsync(flag); await db.SaveChangesAsync(); }

    public async Task<FlaggedJob?> GetFlagAsync(Guid id) => await db.FlaggedJobs.FindAsync(id);

    public async Task UpdateFlagAsync(FlaggedJob flag) { db.FlaggedJobs.Update(flag); await db.SaveChangesAsync(); }

    public async Task<IEnumerable<FlaggedJob>> GetFlagsAsync(string? status, int page, int pageSize)
    {
        var query = db.FlaggedJobs.AsQueryable();
        if (!string.IsNullOrEmpty(status)) query = query.Where(f => f.Status == status);
        return await query.OrderByDescending(f => f.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
    }
}
