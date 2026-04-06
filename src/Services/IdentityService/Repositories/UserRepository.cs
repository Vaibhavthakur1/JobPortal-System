using IdentityService.Data;
using IdentityService.Models;
using Microsoft.EntityFrameworkCore;

namespace IdentityService.Repositories;

public class UserRepository(IdentityDbContext db) : IUserRepository
{
    public async Task<User?> GetByIdAsync(Guid id) =>
        await db.Users.FirstOrDefaultAsync(u => u.Id == id && u.IsActive);

    public async Task<User?> GetByEmailAsync(string email) =>
        await db.Users.FirstOrDefaultAsync(u => u.Email == email.ToLower());

    public async Task<User?> GetByRefreshTokenAsync(string refreshToken) =>
        await db.Users.FirstOrDefaultAsync(u => u.RefreshToken == refreshToken);

    public async Task<User?> GetByVerificationTokenAsync(string token) =>
        await db.Users.FirstOrDefaultAsync(u => u.EmailVerificationToken == token);

    public async Task<User?> GetByResetTokenAsync(string token) =>
        await db.Users.FirstOrDefaultAsync(u => u.PasswordResetToken == token);

    public async Task AddAsync(User user)
    {
        await db.Users.AddAsync(user);
        await db.SaveChangesAsync();
    }

    public async Task UpdateAsync(User user)
    {
        user.UpdatedAt = DateTime.UtcNow;
        db.Users.Update(user);
        await db.SaveChangesAsync();
    }

    public async Task<IEnumerable<User>> GetAllAsync(int page, int pageSize) =>
        await db.Users.Skip((page - 1) * pageSize).Take(pageSize).ToListAsync();
}
