using MassTransit;
using Shared.Contracts.Events.Application;
using Shared.Contracts.Events.Notification;

namespace ApplicationService.Sagas;

/// <summary>
/// Tracks the full lifecycle of a job application.
/// CorrelationId = ApplicationId — MassTransit routes events automatically.
/// Publishes "Both" (in-app + email) notifications on every status transition.
/// </summary>
public class ApplicationSaga : MassTransitStateMachine<ApplicationSagaState>
{
    public State Submitted  { get; private set; } = null!;
    public State Screening  { get; private set; } = null!;
    public State Interview  { get; private set; } = null!;
    public State Offered    { get; private set; } = null!;
    public State Accepted   { get; private set; } = null!;
    public State Rejected   { get; private set; } = null!;
    public State Withdrawn  { get; private set; } = null!;

    public Event<ApplicationStatusChangedEvent> StatusChanged { get; private set; } = null!;

    public ApplicationSaga()
    {
        InstanceState(x => x.CurrentState);

        Event(() => StatusChanged, x =>
            x.CorrelateById(m => m.Message.CorrelationId));

        // ── Draft → Submitted ─────────────────────────────────────────────────
        Initially(
            When(StatusChanged, ctx => ctx.Message.NewStatus == "Submitted")
                .Then(ctx =>
                {
                    ctx.Saga.JobSeekerId    = ctx.Message.JobSeekerId;
                    ctx.Saga.JobId          = ctx.Message.JobId;
                    ctx.Saga.RecruiterId    = ctx.Message.RecruiterId;
                    ctx.Saga.SubmittedAt    = ctx.Message.ChangedAt;
                    ctx.Saga.JobSeekerEmail = ctx.Message.JobSeekerEmail;
                    ctx.Saga.JobSeekerName  = ctx.Message.JobSeekerName;
                })
                .PublishAsync(ctx => ctx.Init<SendNotificationEvent>(new SendNotificationEvent(
                    UserId:        ctx.Message.JobSeekerId,
                    Type:          "Both",
                    Subject:       "Application Submitted — JobMart",
                    Body:          "Your application has been submitted successfully. We'll notify you of any updates.",
                    CreatedAt:     DateTime.UtcNow,
                    CorrelationId: ctx.Saga.CorrelationId,
                    UserEmail:     ctx.Message.JobSeekerEmail,
                    UserName:      ctx.Message.JobSeekerName)))
                .TransitionTo(Submitted));

        // ── Submitted ─────────────────────────────────────────────────────────
        During(Submitted,
            When(StatusChanged, ctx => ctx.Message.NewStatus == "Screening")
                .Then(ctx => ctx.Saga.LastUpdatedAt = DateTime.UtcNow)
                .PublishAsync(ctx => ctx.Init<SendNotificationEvent>(new SendNotificationEvent(
                    UserId:        ctx.Saga.JobSeekerId,
                    Type:          "Both",
                    Subject:       "Your Application is Under Review — JobMart",
                    Body:          "Good news! Your application is now being reviewed by the recruiter.",
                    CreatedAt:     DateTime.UtcNow,
                    CorrelationId: ctx.Saga.CorrelationId,
                    UserEmail:     ctx.Saga.JobSeekerEmail,
                    UserName:      ctx.Saga.JobSeekerName)))
                .TransitionTo(Screening),

            When(StatusChanged, ctx => ctx.Message.NewStatus == "Rejected")
                .Then(ctx => ctx.Saga.LastUpdatedAt = DateTime.UtcNow)
                .PublishAsync(ctx => ctx.Init<SendNotificationEvent>(new SendNotificationEvent(
                    UserId:        ctx.Saga.JobSeekerId,
                    Type:          "Both",
                    Subject:       "Application Update — JobMart",
                    Body:          "Thank you for your interest. Unfortunately, your application was not selected at this time.",
                    CreatedAt:     DateTime.UtcNow,
                    CorrelationId: ctx.Saga.CorrelationId,
                    UserEmail:     ctx.Saga.JobSeekerEmail,
                    UserName:      ctx.Saga.JobSeekerName)))
                .TransitionTo(Rejected).Finalize());

        // ── Screening ─────────────────────────────────────────────────────────
        During(Screening,
            When(StatusChanged, ctx => ctx.Message.NewStatus == "Interview")
                .Then(ctx => ctx.Saga.LastUpdatedAt = DateTime.UtcNow)
                .PublishAsync(ctx => ctx.Init<SendNotificationEvent>(new SendNotificationEvent(
                    UserId:        ctx.Saga.JobSeekerId,
                    Type:          "Both",
                    Subject:       "Interview Scheduled — JobMart",
                    Body:          "Congratulations! You have been selected for an interview. The recruiter will contact you shortly.",
                    CreatedAt:     DateTime.UtcNow,
                    CorrelationId: ctx.Saga.CorrelationId,
                    UserEmail:     ctx.Saga.JobSeekerEmail,
                    UserName:      ctx.Saga.JobSeekerName)))
                .TransitionTo(Interview),

            When(StatusChanged, ctx => ctx.Message.NewStatus == "Rejected")
                .Then(ctx => ctx.Saga.LastUpdatedAt = DateTime.UtcNow)
                .PublishAsync(ctx => ctx.Init<SendNotificationEvent>(new SendNotificationEvent(
                    UserId:        ctx.Saga.JobSeekerId,
                    Type:          "Both",
                    Subject:       "Application Update — JobMart",
                    Body:          "Thank you for your interest. Unfortunately, your application was not selected after screening.",
                    CreatedAt:     DateTime.UtcNow,
                    CorrelationId: ctx.Saga.CorrelationId,
                    UserEmail:     ctx.Saga.JobSeekerEmail,
                    UserName:      ctx.Saga.JobSeekerName)))
                .TransitionTo(Rejected).Finalize());

        // ── Interview ─────────────────────────────────────────────────────────
        During(Interview,
            When(StatusChanged, ctx => ctx.Message.NewStatus == "Offered")
                .Then(ctx => ctx.Saga.LastUpdatedAt = DateTime.UtcNow)
                .PublishAsync(ctx => ctx.Init<SendNotificationEvent>(new SendNotificationEvent(
                    UserId:        ctx.Saga.JobSeekerId,
                    Type:          "Both",
                    Subject:       "You Have a Job Offer! — JobMart",
                    Body:          "Congratulations! You have received a job offer. Please check your application for details.",
                    CreatedAt:     DateTime.UtcNow,
                    CorrelationId: ctx.Saga.CorrelationId,
                    UserEmail:     ctx.Saga.JobSeekerEmail,
                    UserName:      ctx.Saga.JobSeekerName)))
                .TransitionTo(Offered),

            When(StatusChanged, ctx => ctx.Message.NewStatus == "Rejected")
                .Then(ctx => ctx.Saga.LastUpdatedAt = DateTime.UtcNow)
                .PublishAsync(ctx => ctx.Init<SendNotificationEvent>(new SendNotificationEvent(
                    UserId:        ctx.Saga.JobSeekerId,
                    Type:          "Both",
                    Subject:       "Interview Outcome — JobMart",
                    Body:          "Thank you for interviewing with us. Unfortunately, we have decided to move forward with other candidates.",
                    CreatedAt:     DateTime.UtcNow,
                    CorrelationId: ctx.Saga.CorrelationId,
                    UserEmail:     ctx.Saga.JobSeekerEmail,
                    UserName:      ctx.Saga.JobSeekerName)))
                .TransitionTo(Rejected).Finalize());

        // ── Offered ───────────────────────────────────────────────────────────
        During(Offered,
            When(StatusChanged, ctx => ctx.Message.NewStatus == "Accepted")
                .Then(ctx => ctx.Saga.LastUpdatedAt = DateTime.UtcNow)
                .PublishAsync(ctx => ctx.Init<SendNotificationEvent>(new SendNotificationEvent(
                    UserId:        ctx.Saga.JobSeekerId,
                    Type:          "Both",
                    Subject:       "Offer Accepted — Welcome Aboard! — JobMart",
                    Body:          "You have accepted the job offer. Welcome aboard! The recruiter will be in touch soon.",
                    CreatedAt:     DateTime.UtcNow,
                    CorrelationId: ctx.Saga.CorrelationId,
                    UserEmail:     ctx.Saga.JobSeekerEmail,
                    UserName:      ctx.Saga.JobSeekerName)))
                .PublishAsync(ctx => ctx.Init<SendNotificationEvent>(new SendNotificationEvent(
                    UserId:        ctx.Saga.RecruiterId,
                    Type:          "Push",
                    Subject:       "Offer Accepted by Candidate",
                    Body:          "A candidate has accepted your job offer. Check your pipeline for details.",
                    CreatedAt:     DateTime.UtcNow,
                    CorrelationId: ctx.Saga.CorrelationId)))
                .TransitionTo(Accepted).Finalize(),

            When(StatusChanged, ctx => ctx.Message.NewStatus == "Rejected")
                .Then(ctx => ctx.Saga.LastUpdatedAt = DateTime.UtcNow)
                .TransitionTo(Rejected).Finalize());

        // ── Catch-all: Withdrawn from any state ───────────────────────────────
        DuringAny(
            When(StatusChanged, ctx => ctx.Message.NewStatus == "Withdrawn")
                .Then(ctx => ctx.Saga.LastUpdatedAt = DateTime.UtcNow)
                .PublishAsync(ctx => ctx.Init<SendNotificationEvent>(new SendNotificationEvent(
                    UserId:        ctx.Saga.JobSeekerId,
                    Type:          "Both",
                    Subject:       "Application Withdrawn — JobMart",
                    Body:          "You have successfully withdrawn your application.",
                    CreatedAt:     DateTime.UtcNow,
                    CorrelationId: ctx.Saga.CorrelationId,
                    UserEmail:     ctx.Saga.JobSeekerEmail,
                    UserName:      ctx.Saga.JobSeekerName)))
                .PublishAsync(ctx => ctx.Init<SendNotificationEvent>(new SendNotificationEvent(
                    UserId:        ctx.Saga.RecruiterId,
                    Type:          "Push",
                    Subject:       "Candidate Withdrew Application",
                    Body:          "A candidate has withdrawn their application from your job posting.",
                    CreatedAt:     DateTime.UtcNow,
                    CorrelationId: ctx.Saga.CorrelationId)))
                .TransitionTo(Withdrawn).Finalize());

        SetCompletedWhenFinalized();
    }
}
