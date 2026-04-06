namespace Shared.Contracts.Events.Application;

public record ApplicationStatusChangedEvent(
    Guid ApplicationId,
    Guid JobSeekerId,
    Guid JobId,
    string OldStatus,
    string NewStatus,
    DateTime ChangedAt);
