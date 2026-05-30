using MassTransit;
using RecruiterService.Models;
using RecruiterService.Repositories;
using Shared.Contracts.Events.Application;
using Shared.Contracts.Events.Notification;

namespace RecruiterService.Consumers;

/// <summary>
/// Consumes ApplicationSubmittedEvent (CorrelationId = ApplicationId).
/// 1. Auto-adds the candidate to the recruiter's pipeline (idempotent).
/// 2. Publishes a notification to the recruiter.
/// </summary>
public class ApplicationSubmittedConsumer(
    IRecruiterRepository repo,
    IPublishEndpoint publisher,
    ILogger<ApplicationSubmittedConsumer> logger) : IConsumer<ApplicationSubmittedEvent>
{
    public async Task Consume(ConsumeContext<ApplicationSubmittedEvent> context)
    {
        var msg = context.Message;
        var correlationId = context.CorrelationId ?? msg.CorrelationId;

        logger.LogInformation(
            "[CorrelationId:{CorrelationId}] ApplicationSubmitted — AppId:{AppId} JobId:{JobId} " +
            "Candidate:{CandidateId} Recruiter:{RecruiterId}",
            correlationId, msg.CorrelationId, msg.JobId, msg.JobSeekerId, msg.RecruiterId);

        // 1. Add to pipeline — idempotent, skip if already exists
        var existing = await repo.GetPipelineByApplicationAsync(msg.CorrelationId, msg.RecruiterId);
        if (existing is null)
        {
            var entry = new CandidatePipeline
            {
                RecruiterId   = msg.RecruiterId,
                JobId         = msg.JobId,
                CandidateId   = msg.JobSeekerId,
                ApplicationId = msg.CorrelationId,
                Stage         = "New"
            };
            await repo.AddPipelineEntryAsync(entry);

            logger.LogInformation(
                "[CorrelationId:{CorrelationId}] Pipeline entry created for candidate {CandidateId} on job {JobId}",
                correlationId, msg.JobSeekerId, msg.JobId);
        }
        else
        {
            logger.LogWarning(
                "[CorrelationId:{CorrelationId}] Pipeline entry already exists — skipping duplicate",
                correlationId);
        }

        // 2. Notify recruiter — pass CorrelationId so the notification is traceable
        await publisher.Publish(new SendNotificationEvent(
            UserId: msg.RecruiterId,
            Type: "Push",
            Subject: "New Application Received",
            Body: "A new candidate has applied to your job posting. Check your pipeline to review their profile.",
            CreatedAt: DateTime.UtcNow,
            CorrelationId: correlationId));
    }
}
