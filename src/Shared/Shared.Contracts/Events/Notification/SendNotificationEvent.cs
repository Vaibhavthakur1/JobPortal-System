namespace Shared.Contracts.Events.Notification;

/// <summary>
/// CorrelationId propagated from the originating event for end-to-end tracing.
/// UserEmail / UserName are optional — when provided the consumer sends a real email.
/// </summary>
public record SendNotificationEvent(
    Guid UserId,
    string Type,
    string Subject,
    string Body,
    DateTime CreatedAt,
    Guid? CorrelationId = null,
    string? UserEmail = null,
    string? UserName = null);

