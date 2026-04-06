using ApplicationService.Models;
using ApplicationService.Repositories;
using MassTransit;
using Shared.Contracts.Events.Application;

namespace ApplicationService.Services;

public class ApplicationService(IApplicationRepository repo, IPublishEndpoint publisher) : IApplicationService
{
    public async Task<ApplicationDto> SubmitApplicationAsync(Guid jobSeekerId, SubmitApplicationRequest req)
    {
        if (await repo.HasAppliedAsync(jobSeekerId, req.JobId))
            throw new InvalidOperationException("Already applied to this job.");

        var app = new JobApplication
        {
            JobSeekerId = jobSeekerId,
            JobId = req.JobId,
            ResumeId = req.ResumeId,
            CoverLetter = req.CoverLetter,
            Status = "Submitted"
        };

        app.StatusHistory.Add(new ApplicationStatusHistory
        {
            ApplicationId = app.Id,
            FromStatus = "Draft",
            ToStatus = "Submitted"
        });

        await repo.AddAsync(app);

        await publisher.Publish(new ApplicationStatusChangedEvent(
            app.Id, jobSeekerId, req.JobId, "Draft", "Submitted", DateTime.UtcNow));

        return MapToDto(app);
    }

    public async Task<ApplicationDto> UpdateStatusAsync(Guid applicationId, Guid recruiterId, UpdateStatusRequest req)
    {
        var app = await repo.GetByIdAsync(applicationId)
            ?? throw new KeyNotFoundException("Application not found.");

        var oldStatus = app.Status;
        app.Status = req.NewStatus;
        app.StatusHistory.Add(new ApplicationStatusHistory
        {
            ApplicationId = app.Id,
            FromStatus = oldStatus,
            ToStatus = req.NewStatus,
            Note = req.Note
        });

        await repo.UpdateAsync(app);

        await publisher.Publish(new ApplicationStatusChangedEvent(
            app.Id, app.JobSeekerId, app.JobId, oldStatus, req.NewStatus, DateTime.UtcNow));

        return MapToDto(app);
    }

    public async Task WithdrawAsync(Guid applicationId, Guid jobSeekerId)
    {
        var app = await repo.GetByIdAsync(applicationId)
            ?? throw new KeyNotFoundException("Application not found.");

        if (app.JobSeekerId != jobSeekerId)
            throw new UnauthorizedAccessException("Not authorized.");

        app.IsWithdrawn = true;
        app.Status = "Withdrawn";
        app.StatusHistory.Add(new ApplicationStatusHistory
        {
            ApplicationId = app.Id,
            FromStatus = app.Status,
            ToStatus = "Withdrawn"
        });

        await repo.UpdateAsync(app);

        await publisher.Publish(new ApplicationStatusChangedEvent(
            app.Id, jobSeekerId, app.JobId, app.Status, "Withdrawn", DateTime.UtcNow));
    }

    public async Task<ApplicationDto?> GetApplicationAsync(Guid applicationId)
    {
        var app = await repo.GetByIdAsync(applicationId);
        return app is null ? null : MapToDto(app);
    }

    public async Task<IEnumerable<ApplicationDto>> GetMyApplicationsAsync(Guid jobSeekerId)
    {
        var apps = await repo.GetByJobSeekerAsync(jobSeekerId);
        return apps.Select(MapToDto);
    }

    public async Task<IEnumerable<ApplicationDto>> GetJobApplicantsAsync(Guid jobId, Guid recruiterId)
    {
        var apps = await repo.GetByJobAsync(jobId);
        return apps.Select(MapToDto);
    }

    private static ApplicationDto MapToDto(JobApplication a) => new(
        a.Id, a.JobSeekerId, a.JobId, a.ResumeId, a.Status, a.CoverLetter, a.IsWithdrawn, a.CreatedAt,
        a.StatusHistory.Select(h => new StatusHistoryDto(h.FromStatus, h.ToStatus, h.Note, h.ChangedAt)).ToList());
}
