using ResumeService.Models;

namespace ResumeService.Services;

public interface IResumeService
{
    Task<ResumeDto> CreateAsync(Guid userId, CreateResumeRequest request);
    Task<ResumeDto> UpdateAsync(Guid resumeId, Guid userId, UpdateResumeRequest request);
    Task DeleteAsync(Guid resumeId, Guid userId);
    Task SetDefaultAsync(Guid resumeId, Guid userId);
    Task<ResumeDto?> GetByIdAsync(Guid resumeId);
    Task<IEnumerable<ResumeDto>> GetMyResumesAsync(Guid userId);
    Task<byte[]> ExportPdfAsync(Guid resumeId, Guid userId);
}
