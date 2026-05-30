using MassTransit;

namespace ApplicationService.Sagas;

/// <summary>
/// Saga state persisted per application lifecycle.
/// CorrelationId = ApplicationId — one saga instance per application.
/// </summary>
public class ApplicationSagaState : SagaStateMachineInstance, ISagaVersion
{
    /// <summary>ApplicationId — the natural correlation key for the entire lifecycle.</summary>
    public Guid CorrelationId { get; set; }

    /// <summary>Optimistic concurrency version — prevents lost updates under concurrent events.</summary>
    public int Version { get; set; }

    public string CurrentState { get; set; } = string.Empty;
    public Guid JobSeekerId { get; set; }
    public Guid JobId { get; set; }
    public Guid RecruiterId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
    public string? JobSeekerEmail { get; set; }
    public string? JobSeekerName { get; set; }
}
