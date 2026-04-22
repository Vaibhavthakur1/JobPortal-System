namespace IdentityService.Services;

public interface IEmailService
{
    Task SendOtpAsync(string toEmail, string toName, string otp, string purpose);
}
