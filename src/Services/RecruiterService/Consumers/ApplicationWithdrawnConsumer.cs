using MassTransit;
using RecruiterService.Repositories;
using Shared.Contracts.Events.Application;

namespace RecruiterService.Consumers;

/// <summary>
/// Marks the pipeline entry as withdrawn when a job seeker withdraws their application.
/// This keeps the recruiter's pipeline in sync without any polling.
/// </summary>
public class ApplicationWithdrawnConsumer(
    IRecruiterRepository repo,
    ILogger<ApplicationWithdrawnConsumer> logger) : IConsumer<ApplicationStatusChangedEvent>
{
    public async Task Consume(ConsumeContext<ApplicationStatusChangedEvent> context)
    {
        var msg = context.Message;

        if (msg.NewStatus != "Withdrawn") return;

        var entry = await repo.GetPipelineByApplicationAsync(msg.CorrelationId, msg.RecruiterId);
        if (entry is null)
        {
            logger.LogWarning(
                "[CorrelationId:{CorrelationId}] No pipeline entry found for application {AppId} — skipping withdrawal sync",
                msg.CorrelationId, msg.CorrelationId);
            return;
        }

        entry.IsWithdrawn = true;
        entry.WithdrawnAt = msg.ChangedAt;
        entry.Stage = "Rejected"; // move out of active stages
        await repo.UpdatePipelineEntryAsync(entry);

        logger.LogInformation(
            "[CorrelationId:{CorrelationId}] Pipeline entry {EntryId} marked as withdrawn",
            msg.CorrelationId, entry.Id);
    }
}
