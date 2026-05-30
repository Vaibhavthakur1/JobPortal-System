namespace Shared.Contracts.Events.Application;

/// <summary>
/// CorrelationId = ApplicationId.
/// Each consuming service configures x.CorrelateById(m => m.Message.CorrelationId)
/// so MassTransit routes to the correct saga instance automatically.
/// UserEmail / UserName are resolved by ApplicationService for email delivery.
/// </summary>
public record ApplicationStatusChangedEvent(
    Guid CorrelationId,     // = ApplicationId
    Guid JobSeekerId,
    Guid JobId,
    Guid RecruiterId,
    string OldStatus,
    string NewStatus,
    DateTime ChangedAt,
    string? JobSeekerEmail = null,
    string? JobSeekerName = null);
