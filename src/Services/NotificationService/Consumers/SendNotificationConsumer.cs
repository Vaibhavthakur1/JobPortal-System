using MassTransit;
using Microsoft.EntityFrameworkCore;
using NotificationService.Data;
using NotificationService.Models;
using Shared.Contracts.Events.Notification;

namespace NotificationService.Consumers;

public class SendNotificationConsumer(NotificationDbContext db, ILogger<SendNotificationConsumer> logger) 
    : IConsumer<SendNotificationEvent>
{
    public async Task Consume(ConsumeContext<SendNotificationEvent> context)
    {
        var msg = context.Message;
        logger.LogInformation("Sending {Type} notification to user {UserId}: {Subject}", msg.Type, msg.UserId, msg.Subject);

        var notification = new Notification
        {
            UserId = msg.UserId,
            Type = msg.Type,
            Subject = msg.Subject,
            Body = msg.Body
        };

        await db.Notifications.AddAsync(notification);
        await db.SaveChangesAsync();

        // In production: send actual email via SendGrid/SMTP or push via SignalR/FCM
        if (msg.Type is "Email" or "Both")
            logger.LogInformation("[EMAIL] To: {UserId} | Subject: {Subject}", msg.UserId, msg.Subject);

        if (msg.Type is "Push" or "Both")
            logger.LogInformation("[PUSH] To: {UserId} | {Body}", msg.UserId, msg.Body);
    }
}
