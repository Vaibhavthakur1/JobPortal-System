using ApplicationService.Models;

namespace ApplicationService.Repositories;

public interface IApplicationRepository
{
    Task<JobApplication?> GetByIdAsync(Guid id);
    Task<IEnumerable<JobApplication>> GetByJobSeekerAsync(Guid jobSeekerId);
    Task<IEnumerable<JobApplication>> GetByJobAsync(Guid jobId);
    Task<bool> HasAppliedAsync(Guid jobSeekerId, Guid jobId);
    Task AddAsync(JobApplication application);
    Task UpdateAsync(JobApplication application);
}
