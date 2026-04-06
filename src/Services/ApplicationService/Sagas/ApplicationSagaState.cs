using MassTransit;

namespace ApplicationService.Sagas;

// Saga state machine tracks the full lifecycle of a job application
public class ApplicationSagaState : SagaStateMachineInstance
{
    public Guid CorrelationId { get; set; }
    public string CurrentState { get; set; } = string.Empty;
    public Guid JobSeekerId { get; set; }
    public Guid JobId { get; set; }
    public DateTime SubmittedAt { get; set; }
    public DateTime? LastUpdatedAt { get; set; }
}
