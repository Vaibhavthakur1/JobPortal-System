namespace Shared.Contracts.Events.Notification;

public record SendNotificationEvent(
    Guid UserId,
    string Type,       // Email | Push | Both
    string Subject,
    string Body,
    DateTime CreatedAt);
