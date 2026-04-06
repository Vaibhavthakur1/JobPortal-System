using ApplicationService.Models;

namespace ApplicationService.Services;

public interface IApplicationService
{
    Task<ApplicationDto> SubmitApplicationAsync(Guid jobSeekerId, SubmitApplicationRequest request);
    Task<ApplicationDto> UpdateStatusAsync(Guid applicationId, Guid recruiterId, UpdateStatusRequest request);
    Task WithdrawAsync(Guid applicationId, Guid jobSeekerId);
    Task<ApplicationDto?> GetApplicationAsync(Guid applicationId);
    Task<IEnumerable<ApplicationDto>> GetMyApplicationsAsync(Guid jobSeekerId);
    Task<IEnumerable<ApplicationDto>> GetJobApplicantsAsync(Guid jobId, Guid recruiterId);
}
