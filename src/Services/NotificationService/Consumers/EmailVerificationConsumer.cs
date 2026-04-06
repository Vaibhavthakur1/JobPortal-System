using MassTransit;
using Shared.Contracts.Events.Identity;

namespace NotificationService.Consumers;

public class EmailVerificationConsumer(ILogger<EmailVerificationConsumer> logger) 
    : IConsumer<EmailVerificationRequestedEvent>
{
    public Task Consume(ConsumeContext<EmailVerificationRequestedEvent> context)
    {
        var msg = context.Message;
        // In production: send actual email with verification link
        logger.LogInformation("[EMAIL VERIFICATION] To: {Email} | Token: {Token}", msg.Email, msg.VerificationToken);
        return Task.CompletedTask;
    }
}
