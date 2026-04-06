namespace RecruiterService.Models;

public record CreateProfileRequest(string CompanyName, string CompanyDescription, string Industry, string Website, string Location);
public record UpdateProfileRequest(string? CompanyName, string? CompanyDescription, string? Industry, string? Website, string? Location);

public record AddToPipelineRequest(Guid JobId, Guid CandidateId, Guid ApplicationId);
public record UpdatePipelineStageRequest(string Stage, string? Notes);

public record RecruiterProfileDto(Guid Id, Guid UserId, string CompanyName, string Industry, string Website, string Location, DateTime CreatedAt);

public record PipelineDto(
    Guid Id, Guid RecruiterId, Guid JobId, Guid CandidateId, Guid ApplicationId,
    string Stage, string? Notes, bool ResumeViewed, bool ContactUnlocked, DateTime CreatedAt);

// Points cost config
public static class PointsCost
{
    public const int ResumeView = 5;
    public const int ContactUnlock = 10;
}
