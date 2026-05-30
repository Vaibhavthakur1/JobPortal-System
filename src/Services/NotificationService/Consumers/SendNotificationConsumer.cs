using MassTransit;
using NotificationService.Data;
using NotificationService.Models;
using NotificationService.Services;
using Shared.Contracts.Events.Notification;

namespace NotificationService.Consumers;

/// <summary>
/// Persists a notification to the DB and sends an email when Type is "Email" or "Both".
/// </summary>
public class SendNotificationConsumer(
    NotificationDbContext db,
    IEmailService emailService,
    ILogger<SendNotificationConsumer> logger) : IConsumer<SendNotificationEvent>
{
    public async Task Consume(ConsumeContext<SendNotificationEvent> context)
    {
        var msg = context.Message;
        var correlationId = context.CorrelationId ?? msg.CorrelationId ?? Guid.Empty;

        if (msg.UserId == Guid.Empty)
        {
            logger.LogWarning(
                "[CorrelationId:{CorrelationId}] Skipping notification — empty UserId. Subject: {Subject}",
                correlationId, msg.Subject);
            return;
        }

        // ── Persist in-app notification ───────────────────────────────────────
        var notification = new Notification
        {
            UserId  = msg.UserId,
            Type    = msg.Type,
            Subject = msg.Subject,
            Body    = msg.Body
        };

        await db.Notifications.AddAsync(notification);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "[CorrelationId:{CorrelationId}] Saved {Type} notification for user {UserId}: {Subject}",
            correlationId, msg.Type, msg.UserId, msg.Subject);

        // ── Send email ────────────────────────────────────────────────────────
        if (msg.Type is "Email" or "Both")
        {
            if (string.IsNullOrWhiteSpace(msg.UserEmail))
            {
                logger.LogWarning(
                    "[CorrelationId:{CorrelationId}] Email requested but UserEmail is missing for user {UserId}",
                    correlationId, msg.UserId);
            }
            else
            {
                try
                {
                    var recipientName = msg.UserName ?? "there";
                    var html = EmailTemplates.ApplicationStatus(recipientName, msg.Subject, msg.Body);
                    await emailService.SendAsync(msg.UserEmail, recipientName, msg.Subject, html);

                    logger.LogInformation(
                        "[CorrelationId:{CorrelationId}] Email sent to {Email} — Subject: {Subject}",
                        correlationId, msg.UserEmail, msg.Subject);
                }
                catch (Exception ex)
                {
                    logger.LogError(ex,
                        "[CorrelationId:{CorrelationId}] Failed to send email to {Email}",
                        correlationId, msg.UserEmail);
                    // Don't rethrow — notification is already persisted; email failure is non-fatal
                }
            }
        }
    }
}
