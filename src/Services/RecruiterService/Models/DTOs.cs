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
    bool IsResumeAccessActive,
    bool IsWithdrawn, DateTime? WithdrawnAt,
    DateTime CreatedAt);

// What recruiter sees when viewing a resume — full contact always included after paying 10 pts
public record CandidateResumeView(
    Guid CandidateId,
    bool IsFullAccess,       // always true — kept for API compatibility
    DateTime? AccessExpiresAt,
    string FullName,
    string? Summary,
    List<string> Skills,
    List<ExperiencePreview> Experiences,
    List<EducationPreview> Educations,
    string? Email,
    string? Phone,
    string? LinkedInUrl,
    string? GitHubUrl,
    string? ResumeType = null,
    Guid? ResumeId = null,
    string? UploadedFileName = null
);

public record ExperiencePreview(string JobTitle, string Company, string Location, DateTime StartDate, DateTime? EndDate, bool IsCurrent);
public record EducationPreview(string Degree, string FieldOfStudy, string Institution, DateTime StartDate, DateTime? EndDate);

// Points cost config
public static class PointsCost
{
    public const int ResumeView = 10;
    public const int ResumeAccessDays = 30;
}
