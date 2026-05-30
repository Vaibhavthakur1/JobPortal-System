using ApplicationService.Clients;
using ApplicationService.Models;
using ApplicationService.Repositories;
using MassTransit;
using Shared.Contracts.Events.Application;

namespace ApplicationService.Services;

public class ApplicationService(
    IApplicationRepository repo,
    IPublishEndpoint publisher,
    IIdentityClient identityClient) : IApplicationService
{
    public async Task<ApplicationDto> SubmitApplicationAsync(Guid jobSeekerId, SubmitApplicationRequest req)
    {
        if (await repo.HasAppliedAsync(jobSeekerId, req.JobId))
            throw new InvalidOperationException("You have already applied to this job.");

        var app = new JobApplication
        {
            JobSeekerId = jobSeekerId,
            JobId       = req.JobId,
            RecruiterId = req.RecruiterId,
            ResumeId    = req.ResumeId,
            CoverLetter = req.CoverLetter,
            Status      = "Submitted"
        };

        app.StatusHistory.Add(new ApplicationStatusHistory
        {
            ApplicationId = app.Id,
            FromStatus    = "Draft",
            ToStatus      = "Submitted"
        });

        await repo.AddAsync(app);

        var userInfo = await identityClient.GetUserInfoAsync(jobSeekerId);

        await publisher.Publish(new ApplicationStatusChangedEvent(
            CorrelationId:   app.Id,
            JobSeekerId:     jobSeekerId,
            JobId:           req.JobId,
            RecruiterId:     req.RecruiterId,
            OldStatus:       "Draft",
            NewStatus:       "Submitted",
            ChangedAt:       DateTime.UtcNow,
            JobSeekerEmail:  userInfo?.Email,
            JobSeekerName:   userInfo?.FullName));

        await publisher.Publish(new ApplicationSubmittedEvent(
            CorrelationId: app.Id,
            JobSeekerId:   jobSeekerId,
            JobId:         req.JobId,
            RecruiterId:   req.RecruiterId,
            SubmittedAt:   DateTime.UtcNow));

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
            FromStatus    = oldStatus,
            ToStatus      = req.NewStatus,
            Note          = req.Note
        });

        await repo.UpdateAsync(app);

        var userInfo = await identityClient.GetUserInfoAsync(app.JobSeekerId);

        await publisher.Publish(new ApplicationStatusChangedEvent(
            CorrelationId:   app.Id,
            JobSeekerId:     app.JobSeekerId,
            JobId:           app.JobId,
            RecruiterId:     recruiterId,
            OldStatus:       oldStatus,
            NewStatus:       req.NewStatus,
            ChangedAt:       DateTime.UtcNow,
            JobSeekerEmail:  userInfo?.Email,
            JobSeekerName:   userInfo?.FullName));

        return MapToDto(app);
    }

    public async Task WithdrawAsync(Guid applicationId, Guid jobSeekerId)
    {
        var app = await repo.GetByIdAsync(applicationId)
            ?? throw new KeyNotFoundException("Application not found.");

        if (app.JobSeekerId != jobSeekerId)
            throw new UnauthorizedAccessException("Not authorized.");

        var oldStatus = app.Status;
        app.IsWithdrawn = true;
        app.Status = "Withdrawn";
        app.StatusHistory.Add(new ApplicationStatusHistory
        {
            ApplicationId = app.Id,
            FromStatus    = oldStatus,
            ToStatus      = "Withdrawn"
        });

        await repo.UpdateAsync(app);

        var userInfo = await identityClient.GetUserInfoAsync(jobSeekerId);

        await publisher.Publish(new ApplicationStatusChangedEvent(
            CorrelationId:   app.Id,
            JobSeekerId:     jobSeekerId,
            JobId:           app.JobId,
            RecruiterId:     app.RecruiterId,
            OldStatus:       oldStatus,
            NewStatus:       "Withdrawn",
            ChangedAt:       DateTime.UtcNow,
            JobSeekerEmail:  userInfo?.Email,
            JobSeekerName:   userInfo?.FullName));
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
        a.Id, a.JobSeekerId, a.JobId, a.ResumeId, a.Status,
        a.CoverLetter, a.IsWithdrawn, a.CreatedAt,
        a.StatusHistory.Select(h => new StatusHistoryDto(
            h.FromStatus, h.ToStatus, h.Note, h.ChangedAt)).ToList());
}
