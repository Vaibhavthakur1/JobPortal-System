using IdentityService.Models;

namespace IdentityService.Services;

public interface IAuthService
{
    Task<string> RegisterAsync(RegisterRequest request);
    Task VerifyEmailOtpAsync(VerifyEmailOtpRequest request);
    Task ResendOtpAsync(ResendOtpRequest request);
    Task<AuthResponse> LoginAsync(LoginRequest request);
    Task<AuthResponse> RefreshTokenAsync(string refreshToken);
    Task ForgotPasswordAsync(string email);
    Task VerifyForgotPasswordOtpAsync(VerifyForgotPasswordOtpRequest request);
    Task LogoutAsync(Guid userId);
}
