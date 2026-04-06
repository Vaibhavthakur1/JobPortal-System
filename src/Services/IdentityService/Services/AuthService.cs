using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IdentityService.Models;
using IdentityService.Repositories;
using MassTransit;
using Microsoft.IdentityModel.Tokens;
using Shared.Contracts.Events.Identity;
using Shared.Contracts.Events.Notification;

namespace IdentityService.Services;

public class AuthService(
    IUserRepository userRepo,
    IConfiguration config,
    IPublishEndpoint publisher) : IAuthService
{
    public async Task<AuthResponse> RegisterAsync(RegisterRequest request)
    {
        var existing = await userRepo.GetByEmailAsync(request.Email);
        if (existing is not null)
            throw new InvalidOperationException("Email already registered.");

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            EmailVerificationToken = GenerateSecureToken()
        };

        await userRepo.AddAsync(user);

        // publish events
        await publisher.Publish(new UserRegisteredEvent(user.Id, user.Email, user.FullName, user.Role, user.CreatedAt));
        await publisher.Publish(new EmailVerificationRequestedEvent(user.Id, user.Email, user.EmailVerificationToken!));

        return GenerateAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await userRepo.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsEmailVerified)
            throw new UnauthorizedAccessException("Email not verified.");

        var response = GenerateAuthResponse(user);
        user.RefreshToken = response.RefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await userRepo.UpdateAsync(user);

        return response;
    }

    public async Task<AuthResponse> RefreshTokenAsync(string refreshToken)
    {
        var user = await userRepo.GetByRefreshTokenAsync(refreshToken)
            ?? throw new UnauthorizedAccessException("Invalid refresh token.");

        if (user.RefreshTokenExpiry < DateTime.UtcNow)
            throw new UnauthorizedAccessException("Refresh token expired.");

        var response = GenerateAuthResponse(user);
        user.RefreshToken = response.RefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await userRepo.UpdateAsync(user);

        return response;
    }

    public async Task ForgotPasswordAsync(string email)
    {
        var user = await userRepo.GetByEmailAsync(email);
        if (user is null) return; // silent fail for security

        user.PasswordResetToken = GenerateSecureToken();
        user.PasswordResetTokenExpiry = DateTime.UtcNow.AddHours(1);
        await userRepo.UpdateAsync(user);

        await publisher.Publish(new SendNotificationEvent(
            user.Id, "Email",
            "Password Reset Request",
            $"Use this token to reset your password: {user.PasswordResetToken}",
            DateTime.UtcNow));
    }

    public async Task ResetPasswordAsync(ResetPasswordRequest request)
    {
        var user = await userRepo.GetByResetTokenAsync(request.Token)
            ?? throw new InvalidOperationException("Invalid or expired token.");

        if (user.PasswordResetTokenExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("Token expired.");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.PasswordResetToken = null;
        user.PasswordResetTokenExpiry = null;
        await userRepo.UpdateAsync(user);
    }

    public async Task VerifyEmailAsync(string token)
    {
        var user = await userRepo.GetByVerificationTokenAsync(token)
            ?? throw new InvalidOperationException("Invalid verification token.");

        user.IsEmailVerified = true;
        user.EmailVerificationToken = null;
        await userRepo.UpdateAsync(user);
    }

    public async Task LogoutAsync(Guid userId)
    {
        var user = await userRepo.GetByIdAsync(userId);
        if (user is null) return;
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await userRepo.UpdateAsync(user);
    }

    private AuthResponse GenerateAuthResponse(User user)
    {
        var expiry = DateTime.UtcNow.AddHours(1);
        var token = GenerateJwt(user, expiry);
        var refresh = GenerateSecureToken();
        return new AuthResponse(token, refresh, expiry, user.Role, user.Id);
    }

    private string GenerateJwt(User user, DateTime expiry)
    {
        var key = new SymmetricSecurityKey(Encoding.UTF8.GetBytes(config["Jwt:Secret"]!));
        var creds = new SigningCredentials(key, SecurityAlgorithms.HmacSha256);
        var claims = new[]
        {
            new Claim(JwtRegisteredClaimNames.Sub, user.Id.ToString()),
            new Claim(JwtRegisteredClaimNames.Email, user.Email),
            new Claim(ClaimTypes.Role, user.Role),
            new Claim("fullName", user.FullName)
        };
        var token = new JwtSecurityToken(
            issuer: config["Jwt:Issuer"],
            audience: config["Jwt:Audience"],
            claims: claims,
            expires: expiry,
            signingCredentials: creds);
        return new JwtSecurityTokenHandler().WriteToken(token);
    }

    private static string GenerateSecureToken() =>
        Convert.ToBase64String(RandomNumberGenerator.GetBytes(64))
            .Replace("+", "-").Replace("/", "_").Replace("=", "");
}
