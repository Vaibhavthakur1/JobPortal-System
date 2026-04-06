using IdentityService.Models;

namespace IdentityService.Repositories;

public interface IUserRepository
{
    Task<User?> GetByIdAsync(Guid id);
    Task<User?> GetByEmailAsync(string email);
    Task<User?> GetByRefreshTokenAsync(string refreshToken);
    Task<User?> GetByVerificationTokenAsync(string token);
    Task<User?> GetByResetTokenAsync(string token);
    Task AddAsync(User user);
    Task UpdateAsync(User user);
    Task<IEnumerable<User>> GetAllAsync(int page, int pageSize);
}
