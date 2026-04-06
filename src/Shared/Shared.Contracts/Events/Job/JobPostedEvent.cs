namespace Shared.Contracts.Events.Job;

public record JobPostedEvent(
    Guid JobId,
    Guid RecruiterId,
    string Title,
    string Company,
    string Location,
    DateTime PostedAt);
