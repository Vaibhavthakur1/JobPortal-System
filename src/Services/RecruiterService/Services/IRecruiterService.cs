using RecruiterService.Models;

namespace RecruiterService.Services;

public interface IRecruiterService
{
    Task<RecruiterProfileDto> CreateProfileAsync(Guid userId, CreateProfileRequest request);
    Task<RecruiterProfileDto> UpdateProfileAsync(Guid userId, UpdateProfileRequest request);
    Task<RecruiterProfileDto?> GetProfileAsync(Guid userId);

    Task<PipelineDto> AddToPipelineAsync(Guid recruiterId, AddToPipelineRequest request);
    Task<PipelineDto> UpdateStageAsync(Guid pipelineId, Guid recruiterId, UpdatePipelineStageRequest request);
    Task<IEnumerable<PipelineDto>> GetPipelineAsync(Guid jobId, Guid recruiterId, string? stageFilter);

    // Points-based actions
    Task<CandidateResumeView> ViewResumeAsync(Guid pipelineId, Guid recruiterId);
    Task<PipelineDto> UnlockContactAsync(Guid pipelineId, Guid recruiterId);
}
