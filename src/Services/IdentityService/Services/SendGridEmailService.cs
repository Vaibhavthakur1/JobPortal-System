using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace IdentityService.Services;

public class SendGridEmailService(IConfiguration config, ILogger<SendGridEmailService> logger) : IEmailService
{
    public async Task SendOtpAsync(string toEmail, string toName, string otp, string purpose)
    {
        var subject = purpose == "EmailVerification"
            ? "Verify Your Email - JobMart"
            : "Password Reset OTP - JobMart";

        var body = purpose == "EmailVerification"
            ? $@"
                <div style='font-family:Arial,sans-serif;max-width:480px;margin:auto;border:1px solid #e5e7eb;border-radius:8px;padding:32px;'>
                  <h2 style='color:#4F46E5;'>Welcome to JobMart!</h2>
                  <p>Hi <strong>{toName}</strong>,</p>
                  <p>Use the OTP below to verify your email address:</p>
                  <div style='background:#F3F4F6;border-radius:8px;padding:20px;text-align:center;margin:24px 0;'>
                    <span style='font-size:36px;font-weight:bold;letter-spacing:12px;color:#4F46E5;'>{otp}</span>
                  </div>
                  <p>This OTP is valid for <strong>10 minutes</strong>.</p>
                  <p style='color:#6B7280;font-size:13px;'>If you did not register on JobMart, please ignore this email.</p>
                  <hr style='border:none;border-top:1px solid #e5e7eb;margin:24px 0;'/>
                  <p style='color:#9CA3AF;font-size:12px;'>JobMart — Your Career Partner</p>
                </div>"
            : $@"
                <div style='font-family:Arial,sans-serif;max-width:480px;margin:auto;border:1px solid #e5e7eb;border-radius:8px;padding:32px;'>
                  <h2 style='color:#DC2626;'>Password Reset Request</h2>
                  <p>Hi <strong>{toName}</strong>,</p>
                  <p>Use the OTP below to reset your password:</p>
                  <div style='background:#FEF2F2;border-radius:8px;padding:20px;text-align:center;margin:24px 0;'>
                    <span style='font-size:36px;font-weight:bold;letter-spacing:12px;color:#DC2626;'>{otp}</span>
                  </div>
                  <p>This OTP is valid for <strong>10 minutes</strong>.</p>
                  <p style='color:#6B7280;font-size:13px;'>If you did not request this, please ignore this email.</p>
                  <hr style='border:none;border-top:1px solid #e5e7eb;margin:24px 0;'/>
                  <p style='color:#9CA3AF;font-size:12px;'>JobMart — Your Career Partner</p>
                </div>";

        var smtpHost = config["Email:SmtpHost"]!;
        var smtpPort = int.Parse(config["Email:SmtpPort"]!);
        var smtpUser = config["Email:Username"]!;
        var smtpPass = config["Email:Password"]!;
        var fromEmail = config["Email:FromEmail"]!;
        var fromName = config["Email:FromName"]!;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = body };

        using var client = new SmtpClient();
        await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(smtpUser, smtpPass);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        logger.LogInformation("OTP email sent to {Email} for {Purpose}", toEmail, purpose);
    }
}
