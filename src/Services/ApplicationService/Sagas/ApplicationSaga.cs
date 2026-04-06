using MassTransit;
using Shared.Contracts.Events.Application;
using Shared.Contracts.Events.Notification;

namespace ApplicationService.Sagas;

public class ApplicationSaga : MassTransitStateMachine<ApplicationSagaState>
{
    public State Submitted { get; private set; } = null!;
    public State Screening { get; private set; } = null!;
    public State Interview { get; private set; } = null!;
    public State Offered { get; private set; } = null!;
    public State Accepted { get; private set; } = null!;
    public State Rejected { get; private set; } = null!;
    public State Withdrawn { get; private set; } = null!;

    public Event<ApplicationStatusChangedEvent> StatusChanged { get; private set; } = null!;

    public ApplicationSaga()
    {
        InstanceState(x => x.CurrentState);

        Event(() => StatusChanged, x =>
            x.CorrelateById(ctx => ctx.Message.ApplicationId));

        // Transitions
        Initially(
            When(StatusChanged, ctx => ctx.Message.NewStatus == "Submitted")
                .Then(ctx =>
                {
                    ctx.Saga.JobSeekerId = ctx.Message.JobSeekerId;
                    ctx.Saga.JobId = ctx.Message.JobId;
                    ctx.Saga.SubmittedAt = ctx.Message.ChangedAt;
                })
                .PublishAsync(ctx => ctx.Init<SendNotificationEvent>(new SendNotificationEvent(
                    ctx.Message.JobSeekerId, "Push",
                    "Application Submitted",
                    $"Your application has been submitted successfully.",
                    DateTime.UtcNow)))
                .TransitionTo(Submitted));

        During(Submitted,
            When(StatusChanged, ctx => ctx.Message.NewStatus == "Screening")
                .Then(ctx => ctx.Saga.LastUpdatedAt = DateTime.UtcNow)
                .PublishAsync(ctx => ctx.Init<SendNotificationEvent>(new SendNotificationEvent(
                    ctx.Saga.JobSeekerId, "Push",
                    "Application Update",
                    "Your application is now in screening.",
                    DateTime.UtcNow)))
                .TransitionTo(Screening));

        During(Screening,
            When(StatusChanged, ctx => ctx.Message.NewStatus == "Interview")
                .Then(ctx => ctx.Saga.LastUpdatedAt = DateTime.UtcNow)
                .PublishAsync(ctx => ctx.Init<SendNotificationEvent>(new SendNotificationEvent(
                    ctx.Saga.JobSeekerId, "Push",
                    "Interview Scheduled",
                    "Congratulations! You have been selected for an interview.",
                    DateTime.UtcNow)))
                .TransitionTo(Interview),
            When(StatusChanged, ctx => ctx.Message.NewStatus == "Rejected")
                .TransitionTo(Rejected).Finalize());

        During(Interview,
            When(StatusChanged, ctx => ctx.Message.NewStatus == "Offered")
                .PublishAsync(ctx => ctx.Init<SendNotificationEvent>(new SendNotificationEvent(
                    ctx.Saga.JobSeekerId, "Push",
                    "Job Offer",
                    "You have received a job offer!",
                    DateTime.UtcNow)))
                .TransitionTo(Offered),
            When(StatusChanged, ctx => ctx.Message.NewStatus == "Rejected")
                .TransitionTo(Rejected).Finalize());

        During(Offered,
            When(StatusChanged, ctx => ctx.Message.NewStatus == "Accepted")
                .TransitionTo(Accepted).Finalize(),
            When(StatusChanged, ctx => ctx.Message.NewStatus == "Rejected")
                .TransitionTo(Rejected).Finalize());

        DuringAny(
            When(StatusChanged, ctx => ctx.Message.NewStatus == "Withdrawn")
                .TransitionTo(Withdrawn).Finalize());

        SetCompletedWhenFinalized();
    }
}
