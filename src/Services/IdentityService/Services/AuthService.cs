using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Security.Cryptography;
using System.Text;
using IdentityService.Models;
using IdentityService.Repositories;
using MassTransit;
using Microsoft.IdentityModel.Tokens;
using Shared.Contracts.Events.Identity;

namespace IdentityService.Services;

public class AuthService(
    IUserRepository userRepo,
    IConfiguration config,
    IPublishEndpoint publisher,
    IEmailService emailService) : IAuthService
{
    // ─── Register ────────────────────────────────────────────────────────────
    public async Task<string> RegisterAsync(RegisterRequest request)
    {
        if (!UserRoles.IsValid(request.Role))
            throw new InvalidOperationException($"Invalid role '{request.Role}'. Allowed: {string.Join(", ", UserRoles.All)}");

        if (request.Role == UserRoles.Admin)
            throw new InvalidOperationException("Admin accounts cannot be self-registered.");

        var existing = await userRepo.GetByEmailAsync(request.Email);
        if (existing is not null)
            throw new InvalidOperationException("Email already registered.");

        var otp = GenerateOtp();

        var user = new User
        {
            FullName = request.FullName,
            Email = request.Email.ToLower(),
            PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.Password),
            Role = request.Role,
            OtpCode = otp,
            OtpExpiry = DateTime.UtcNow.AddMinutes(10),
            OtpPurpose = "EmailVerification"
        };

        await userRepo.AddAsync(user);

        // Fire and forget — respond immediately, send email in background
        _ = Task.Run(async () =>
        {
            try
            {
                await emailService.SendOtpAsync(user.Email, user.FullName, otp, "EmailVerification");
                await publisher.Publish(new UserRegisteredEvent(user.Id, user.Email, user.FullName, user.Role, user.CreatedAt));
            }
            catch { /* silent */ }
        });

        return "Registration successful. Please check your email for the OTP to verify your account.";
    }

    // ─── Verify Email OTP ────────────────────────────────────────────────────
    public async Task VerifyEmailOtpAsync(VerifyEmailOtpRequest request)
    {
        var user = await userRepo.GetByEmailAsync(request.Email)
            ?? throw new InvalidOperationException("User not found.");

        if (user.IsEmailVerified)
            throw new InvalidOperationException("Email already verified.");

        ValidateOtp(user, request.Otp, "EmailVerification");

        user.IsEmailVerified = true;
        user.OtpCode = null;
        user.OtpExpiry = null;
        user.OtpPurpose = null;
        await userRepo.UpdateAsync(user);
    }

    // ─── Resend OTP ──────────────────────────────────────────────────────────
    public async Task ResendOtpAsync(ResendOtpRequest request)
    {
        var user = await userRepo.GetByEmailAsync(request.Email)
            ?? throw new InvalidOperationException("User not found.");

        if (request.Purpose == "EmailVerification" && user.IsEmailVerified)
            throw new InvalidOperationException("Email already verified.");

        var otp = GenerateOtp();
        user.OtpCode = otp;
        user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
        user.OtpPurpose = request.Purpose;
        await userRepo.UpdateAsync(user);

        _ = Task.Run(async () =>
        {
            try { await emailService.SendOtpAsync(user.Email, user.FullName, otp, request.Purpose); }
            catch { /* silent */ }
        });
    }

    // ─── Login ───────────────────────────────────────────────────────────────
    public async Task<AuthResponse> LoginAsync(LoginRequest request)
    {
        var user = await userRepo.GetByEmailAsync(request.Email)
            ?? throw new UnauthorizedAccessException("Invalid credentials.");

        if (!BCrypt.Net.BCrypt.Verify(request.Password, user.PasswordHash))
            throw new UnauthorizedAccessException("Invalid credentials.");

        if (!user.IsEmailVerified)
            throw new UnauthorizedAccessException("Please verify your email before logging in.");

        var response = GenerateAuthResponse(user);
        user.RefreshToken = response.RefreshToken;
        user.RefreshTokenExpiry = DateTime.UtcNow.AddDays(7);
        await userRepo.UpdateAsync(user);

        return response;
    }

    // ─── Refresh Token ───────────────────────────────────────────────────────
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

    // ─── Forgot Password ─────────────────────────────────────────────────────
    public async Task ForgotPasswordAsync(string email)
    {
        var user = await userRepo.GetByEmailAsync(email);
        if (user is null) return; // silent fail

        var otp = GenerateOtp();
        user.OtpCode = otp;
        user.OtpExpiry = DateTime.UtcNow.AddMinutes(10);
        user.OtpPurpose = "PasswordReset";
        await userRepo.UpdateAsync(user);

        _ = Task.Run(async () =>
        {
            try { await emailService.SendOtpAsync(user.Email, user.FullName, otp, "PasswordReset"); }
            catch { /* silent */ }
        });
    }

    // ─── Verify Forgot Password OTP + Reset ──────────────────────────────────
    public async Task VerifyForgotPasswordOtpAsync(VerifyForgotPasswordOtpRequest request)
    {
        var user = await userRepo.GetByEmailAsync(request.Email)
            ?? throw new InvalidOperationException("User not found.");

        ValidateOtp(user, request.Otp, "PasswordReset");

        user.PasswordHash = BCrypt.Net.BCrypt.HashPassword(request.NewPassword);
        user.OtpCode = null;
        user.OtpExpiry = null;
        user.OtpPurpose = null;
        await userRepo.UpdateAsync(user);
    }

    // ─── Logout ──────────────────────────────────────────────────────────────
    public async Task LogoutAsync(Guid userId)
    {
        var user = await userRepo.GetByIdAsync(userId);
        if (user is null) return;
        user.RefreshToken = null;
        user.RefreshTokenExpiry = null;
        await userRepo.UpdateAsync(user);
    }

    // ─── Helpers ─────────────────────────────────────────────────────────────
    private static void ValidateOtp(User user, string otp, string purpose)
    {
        if (user.OtpPurpose != purpose)
            throw new InvalidOperationException("Invalid OTP purpose.");

        if (user.OtpCode != otp)
            throw new InvalidOperationException("Invalid OTP.");

        if (user.OtpExpiry < DateTime.UtcNow)
            throw new InvalidOperationException("OTP has expired. Please request a new one.");
    }

    private static string GenerateOtp() =>
        Random.Shared.Next(100000, 999999).ToString();

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
