using System.ComponentModel.DataAnnotations;

namespace IdentityService.Models;

public static class UserRoles
{
    public const string JobSeeker = "JobSeeker";
    public const string Recruiter = "Recruiter";
    public const string Admin = "Admin";

    public static readonly string[] All = [JobSeeker, Recruiter, Admin];

    public static bool IsValid(string role) =>
        All.Contains(role, StringComparer.OrdinalIgnoreCase);
}

public record RegisterRequest(
    [Required] string FullName,
    [Required][EmailAddress] string Email,
    [Required][MinLength(6)] string Password,
    [Required] string Role);
public record LoginRequest(string Email, string Password);
public record RefreshTokenRequest(string RefreshToken);
public record ForgotPasswordRequest(string Email);
public record ResetPasswordRequest(string Token, string NewPassword);
public record VerifyEmailRequest(string Token);

public record AuthResponse(
    string AccessToken,
    string RefreshToken,
    DateTime ExpiresAt,
    string Role,
    Guid UserId);

public record UserDto(Guid Id, string FullName, string Email, string Role, bool IsEmailVerified, DateTime CreatedAt);
