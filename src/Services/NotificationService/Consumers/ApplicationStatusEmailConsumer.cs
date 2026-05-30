using MassTransit;
using NotificationService.Data;
using NotificationService.Models;
using NotificationService.Services;
using Shared.Contracts.Events.Application;

namespace NotificationService.Consumers;

/// <summary>
/// Directly consumes ApplicationStatusChangedEvent and sends an email + in-app
/// notification to the job seeker whenever a recruiter updates their status.
/// This runs independently of the saga so every status change is covered,
/// including transitions the saga doesn't explicitly handle.
/// </summary>
public class ApplicationStatusEmailConsumer(
    NotificationDbContext db,
    IEmailService emailService,
    ILogger<ApplicationStatusEmailConsumer> logger) : IConsumer<ApplicationStatusChangedEvent>
{
    public async Task Consume(ConsumeContext<ApplicationStatusChangedEvent> context)
    {
        var msg = context.Message;

        // Only send when a recruiter changes the status (not on withdrawal — saga handles that)
        if (msg.NewStatus == "Withdrawn") return;

        // Skip if no email available
        if (string.IsNullOrWhiteSpace(msg.JobSeekerEmail))
        {
            logger.LogWarning(
                "[AppId:{AppId}] Status changed to {Status} but JobSeekerEmail is missing — skipping email",
                msg.CorrelationId, msg.NewStatus);
            return;
        }

        var (subject, body) = GetEmailContent(msg.NewStatus, msg.OldStatus);

        // ── Persist in-app notification ───────────────────────────────────────
        var notification = new Notification
        {
            UserId  = msg.JobSeekerId,
            Type    = "Both",
            Subject = subject,
            Body    = body
        };
        await db.Notifications.AddAsync(notification);
        await db.SaveChangesAsync();

        logger.LogInformation(
            "[AppId:{AppId}] Saved notification for JobSeeker {JobSeekerId}: {Subject}",
            msg.CorrelationId, msg.JobSeekerId, subject);

        // ── Send email ────────────────────────────────────────────────────────
        try
        {
            var recipientName = msg.JobSeekerName ?? "there";
            var html = EmailTemplates.ApplicationStatus(recipientName, subject, body);
            await emailService.SendAsync(msg.JobSeekerEmail, recipientName, subject, html);

            logger.LogInformation(
                "[AppId:{AppId}] Status email sent to {Email} — {OldStatus} → {NewStatus}",
                msg.CorrelationId, msg.JobSeekerEmail, msg.OldStatus, msg.NewStatus);
        }
        catch (Exception ex)
        {
            logger.LogError(ex,
                "[AppId:{AppId}] Failed to send status email to {Email}",
                msg.CorrelationId, msg.JobSeekerEmail);
            // Non-fatal — notification already persisted
        }
    }

    private static (string Subject, string Body) GetEmailContent(string newStatus, string oldStatus) =>
        newStatus switch
        {
            "Screening" => (
                "Your Application is Under Review — JobMart",
                "Great news! A recruiter is now actively reviewing your application. We'll keep you updated on the next steps."),

            "Interview" => (
                "You've Been Shortlisted for an Interview — JobMart",
                "Congratulations! You have been selected for an interview. The recruiter will contact you shortly with the details. Best of luck!"),

            "Offered" => (
                "🎉 You Have a Job Offer! — JobMart",
                "Fantastic news! You have received a job offer. Please log in to your JobMart account to review the offer details and respond."),

            "Accepted" => (
                "Offer Accepted — Welcome Aboard! — JobMart",
                "Your job offer has been confirmed. Welcome to the team! The recruiter will be in touch soon with onboarding details."),

            "Rejected" => (
                "Application Update — JobMart",
                $"Thank you for your interest and the time you invested in the application process. After careful consideration, the recruiter has decided to move forward with other candidates at this stage. We encourage you to keep applying — the right opportunity is out there!"),

            _ => (
                $"Application Status Updated to {newStatus} — JobMart",
                $"Your application status has been updated from {oldStatus} to {newStatus}. Log in to JobMart to view the details.")
        };
}
