using ResumeService.Models;

namespace ResumeService.Repositories;

public interface IResumeRepository
{
    Task<Resume?> GetByIdAsync(Guid id);
    Task<IEnumerable<Resume>> GetByUserAsync(Guid userId);
    Task<Resume?> GetDefaultAsync(Guid userId);
    Task AddAsync(Resume resume);
    Task UpdateAsync(Resume resume);
    Task DeleteAsync(Guid id);
}
