using IdentityService.Models;
using IdentityService.Services;
using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;

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

    // Login — sets HttpOnly refresh token cookie, returns access token in body
    [AllowAnonymous]
    [HttpPost("login")]
    public async Task<IActionResult> Login([FromBody] LoginRequest request)
    {
        var result = await authService.LoginAsync(request);

        // Refresh token → HttpOnly cookie (JS cannot read this)
        Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions
        {
            HttpOnly  = true,
            Secure    = false,          // set true in production (HTTPS)
            SameSite  = SameSiteMode.Strict,
            Expires   = DateTimeOffset.UtcNow.AddDays(7),
            Path      = "/api/auth"     // only sent to auth endpoints
        });

        // Return access token + user info in body — frontend stores in memory
        return Ok(new
        {
            result.AccessToken,
            result.ExpiresAt,
            result.Role,
            result.UserId,
            result.FullName
        });
    }

    // Refresh token — reads HttpOnly cookie automatically
    [AllowAnonymous]
    [HttpPost("refresh")]
    public async Task<IActionResult> Refresh()
    {
        var token = Request.Cookies["refreshToken"];
        if (string.IsNullOrEmpty(token))
            return Unauthorized(new { message = "No refresh token." });

        var result = await authService.RefreshTokenAsync(token);

        // Rotate refresh token cookie
        Response.Cookies.Append("refreshToken", result.RefreshToken, new CookieOptions
        {
            HttpOnly  = true,
            Secure    = false,
            SameSite  = SameSiteMode.Strict,
            Expires   = DateTimeOffset.UtcNow.AddDays(7),
            Path      = "/api/auth"
        });

        return Ok(new
        {
            result.AccessToken,
            result.ExpiresAt,
            result.Role,
            result.UserId,
            result.FullName
        });
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

    // Logout — clears HttpOnly cookie
    [AllowAnonymous]
    [HttpPost("logout")]
    public async Task<IActionResult> Logout()
    {
        var token = Request.Cookies["refreshToken"];
        if (!string.IsNullOrEmpty(token))
            await authService.LogoutByTokenAsync(token);

        Response.Cookies.Delete("refreshToken", new CookieOptions
        {
            HttpOnly = true,
            Secure   = false,
            SameSite = SameSiteMode.Strict,
            Path     = "/api/auth"
        });

        return Ok(new { message = "Logged out." });
    }
}
