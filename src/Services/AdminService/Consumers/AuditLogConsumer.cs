using AdminService.Models;
using AdminService.Repositories;
using MassTransit;
using Shared.Contracts.Events.Application;
using Shared.Contracts.Events.Identity;
using Shared.Contracts.Events.Payment;

namespace AdminService.Consumers;

// Listens to all major events and writes audit logs
public class UserRegisteredAuditConsumer(IAuditRepository auditRepo) : IConsumer<UserRegisteredEvent>
{
    public async Task Consume(ConsumeContext<UserRegisteredEvent> context)
    {
        await auditRepo.LogAsync(new AuditLog
        {
            UserId = context.Message.UserId,
            Action = "UserRegistered",
            Entity = "User",
            EntityId = context.Message.UserId.ToString(),
            NewValues = $"Email: {context.Message.Email}, Role: {context.Message.Role}"
        });
    }
}

public class ApplicationStatusAuditConsumer(IAuditRepository auditRepo) : IConsumer<ApplicationStatusChangedEvent>
{
    public async Task Consume(ConsumeContext<ApplicationStatusChangedEvent> context)
    {
        var msg = context.Message;
        await auditRepo.LogAsync(new AuditLog
        {
            UserId = msg.JobSeekerId,
            Action = "ApplicationStatusChanged",
            Entity = "Application",
            EntityId = msg.ApplicationId.ToString(),
            OldValues = msg.OldStatus,
            NewValues = msg.NewStatus
        });
    }
}

public class PaymentAuditConsumer(IAuditRepository auditRepo) : IConsumer<PaymentCompletedEvent>
{
    public async Task Consume(ConsumeContext<PaymentCompletedEvent> context)
    {
        var msg = context.Message;
        await auditRepo.LogAsync(new AuditLog
        {
            UserId = msg.RecruiterId,
            Action = "PaymentCompleted",
            Entity = "Payment",
            EntityId = msg.PaymentId.ToString(),
            NewValues = $"Points: {msg.PointsPurchased}, Amount: {msg.AmountPaid} {msg.Currency}"
        });
    }
}
