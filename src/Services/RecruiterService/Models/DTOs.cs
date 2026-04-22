namespace RecruiterService.Models;

public record CreateProfileRequest(string CompanyName, string CompanyDescription, string Industry, string Website, string Location);
public record UpdateProfileRequest(string? CompanyName, string? CompanyDescription, string? Industry, string? Website, string? Location);

public record AddToPipelineRequest(Guid JobId, Guid CandidateId, Guid ApplicationId);
public record UpdatePipelineStageRequest(string Stage, string? Notes);

public record RecruiterProfileDto(Guid Id, Guid UserId, string CompanyName, string Industry, string Website, string Location, DateTime CreatedAt);

public record PipelineDto(
    Guid Id, Guid RecruiterId, Guid JobId, Guid CandidateId, Guid ApplicationId,
    string Stage, string? Notes,
    bool ResumeViewed, DateTime? ResumeViewedAt, DateTime? ResumeAccessExpiresAt,
    bool IsResumeAccessActive,   // true if within 30 days
    bool ContactUnlocked, DateTime? ContactUnlockedAt,
    DateTime CreatedAt);

// What recruiter sees when viewing a resume
public record CandidateResumeView(
    Guid CandidateId,
    bool IsFullAccess,           // true = within 30 days, false = expired/not viewed
    DateTime? AccessExpiresAt,

    // Always visible
    string FullName,
    string? Summary,
    List<string> Skills,
    List<ExperiencePreview> Experiences,
    List<EducationPreview> Educations,

    // Only visible with full access
    string? Email,               // null if no access
    string? Phone,               // null if no access
    string? LinkedInUrl,         // null if no access
    string? GitHubUrl            // null if no access
);

public record ExperiencePreview(string JobTitle, string Company, string Location, DateTime StartDate, DateTime? EndDate, bool IsCurrent);
public record EducationPreview(string Degree, string FieldOfStudy, string Institution, DateTime StartDate, DateTime? EndDate);

// Points cost config
public static class PointsCost
{
    public const int ResumeView = 5;
    public const int ContactUnlock = 10;
    public const int ResumeAccessDays = 30;
}
