using AdminService.Models;

namespace AdminService.Repositories;

public interface IAuditRepository
{
    Task LogAsync(AuditLog log);
    Task<IEnumerable<AuditLog>> GetLogsAsync(int page, int pageSize, Guid? userId = null, string? entity = null);

    Task AddFlagAsync(FlaggedJob flag);
    Task<FlaggedJob?> GetFlagAsync(Guid id);
    Task UpdateFlagAsync(FlaggedJob flag);
    Task<IEnumerable<FlaggedJob>> GetFlagsAsync(string? status, int page, int pageSize);
}
