namespace IdentityService.Models;

public record RegisterRequest(string FullName, string Email, string Password, string Role);
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
