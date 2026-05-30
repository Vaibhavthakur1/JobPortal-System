namespace Shared.Contracts.Events.Application;

/// <summary>
/// CorrelationId = ApplicationId.
/// Consumed by RecruiterService to auto-add candidate to pipeline.
/// </summary>
public record ApplicationSubmittedEvent(
    Guid CorrelationId,     // = ApplicationId
    Guid JobSeekerId,
    Guid JobId,
    Guid RecruiterId,
    DateTime SubmittedAt);
