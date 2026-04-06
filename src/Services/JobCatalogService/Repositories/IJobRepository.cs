using JobCatalogService.Models;

namespace JobCatalogService.Repositories;

public interface IJobRepository
{
    Task<Job?> GetByIdAsync(Guid id);
    Task<PagedResult<Job>> SearchAsync(JobSearchRequest request);
    Task<IEnumerable<Job>> GetByRecruiterAsync(Guid recruiterId);
    Task AddAsync(Job job);
    Task UpdateAsync(Job job);
    Task<bool> ExistsAsync(Guid id);
}
