using MailKit.Net.Smtp;
using MailKit.Security;
using MimeKit;

namespace NotificationService.Services;

public class SmtpEmailService(IConfiguration config, ILogger<SmtpEmailService> logger) : IEmailService
{
    public async Task SendAsync(string toEmail, string toName, string subject, string htmlBody)
    {
        var smtpHost  = config["Email:SmtpHost"]!;
        var smtpPort  = int.Parse(config["Email:SmtpPort"]!);
        var smtpUser  = config["Email:Username"]!;
        var smtpPass  = config["Email:Password"]!;
        var fromEmail = config["Email:FromEmail"]!;
        var fromName  = config["Email:FromName"]!;

        var message = new MimeMessage();
        message.From.Add(new MailboxAddress(fromName, fromEmail));
        message.To.Add(new MailboxAddress(toName, toEmail));
        message.Subject = subject;
        message.Body = new TextPart("html") { Text = htmlBody };

        using var client = new SmtpClient();
        await client.ConnectAsync(smtpHost, smtpPort, SecureSocketOptions.StartTls);
        await client.AuthenticateAsync(smtpUser, smtpPass);
        await client.SendAsync(message);
        await client.DisconnectAsync(true);

        logger.LogInformation("Email sent to {Email} — Subject: {Subject}", toEmail, subject);
    }
}
