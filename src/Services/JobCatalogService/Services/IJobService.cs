using JobCatalogService.Models;

namespace JobCatalogService.Services;

public interface IJobService
{
    Task<JobDto> CreateJobAsync(Guid recruiterId, CreateJobRequest request);
    Task<JobDto> UpdateJobAsync(Guid jobId, Guid recruiterId, UpdateJobRequest request);
    Task CloseJobAsync(Guid jobId, Guid recruiterId);
    Task ArchiveJobAsync(Guid jobId, Guid recruiterId);
    Task<JobDto?> GetJobAsync(Guid jobId);
    Task<PagedResult<JobDto>> SearchJobsAsync(JobSearchRequest request);
    Task<IEnumerable<JobDto>> GetRecruiterJobsAsync(Guid recruiterId);
}
