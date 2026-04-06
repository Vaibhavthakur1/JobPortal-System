using RecruiterService.Models;

namespace RecruiterService.Repositories;

public interface IRecruiterRepository
{
    Task<RecruiterProfile?> GetProfileAsync(Guid userId);
    Task AddProfileAsync(RecruiterProfile profile);
    Task UpdateProfileAsync(RecruiterProfile profile);

    Task<CandidatePipeline?> GetPipelineEntryAsync(Guid id);
    Task<IEnumerable<CandidatePipeline>> GetPipelineByJobAsync(Guid jobId, Guid recruiterId);
    Task<CandidatePipeline?> GetPipelineByApplicationAsync(Guid applicationId, Guid recruiterId);
    Task AddPipelineEntryAsync(CandidatePipeline entry);
    Task UpdatePipelineEntryAsync(CandidatePipeline entry);
}
