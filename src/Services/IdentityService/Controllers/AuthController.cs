using IdentityService.Models;
using IdentityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using System.Security.Claims;

namespace IdentityService.Controllers;

[ApiController]
[Route("api/auth")]
public class AuthController(IAuthService authService) : ControllerBase
{
    // Step 1: Register → sends OTP to email
    [AllowAnonymous]
    [HttpPost("register")]
    public async Task<IActionResult> Register([FromBody] RegisterRequest request)
    {
        var message = await authService.RegisterAsync(request);
        return Ok(new { message });
    }

    // Step 2: Verify email with OTP
    [AllowAnonymous]
    [HttpPost("verify-email-otp")]
    public async Task<IActionResult> VerifyEmailOtp([FromBody] VerifyEmailOtpRequest request)
    {
        await authService.VerifyEmailOtpAsync(request);
        return Ok(new { message = "Email verified successfully. You can now login." });
    }

    // Resend OTP (for both email verification and password reset)
    [AllowAnonymous]
    [HttpPost("resend-otp")]
    public async Task<IActionResult> ResendOtp([FromBody] ResendOtpRequest request)
    {
        await authService.ResendOtpAsync(request);
        return Ok(new { message = "OTP resent to your email." });
    }

    // Login
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);
        return Ok(result);
    }

    // Refresh token
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh([FromBody] RefreshTokenRequest request)
    {
        var result = await authService.RefreshTokenAsync(request.RefreshToken);
        return Ok(result);
    }

    // Step 1 of forgot password: sends OTP to email
    [AllowAnonymous]
    [HttpPost("forgot-password")]
    public async Task<IActionResult> ForgotPassword([FromBody] ForgotPasswordRequest request)
    {
        await authService.ForgotPasswordAsync(request.Email);
        return Ok(new { message = "If the email exists, an OTP has been sent." });
    }

    // Step 2 of forgot password: verify OTP + set new password
    [AllowAnonymous]
    [HttpPost("reset-password-otp")]
    public async Task<IActionResult> ResetPasswordWithOtp([FromBody] VerifyForgotPasswordOtpRequest request)
    {
        await authService.VerifyForgotPasswordOtpAsync(request);
        return Ok(new { message = "Password reset successful. You can now login." });
    }

    // Logout
    [Authorize]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var userId = Guid.Parse(User.FindFirstValue(ClaimTypes.NameIdentifier)!);
        await authService.LogoutAsync(userId);
        return Ok(new { message = "Logged out." });
    }
}
