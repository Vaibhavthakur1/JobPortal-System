namespace ResumeService.Models;

public record PersonalInfoDto(
    string FullName, string Email, string Phone, string Location,
    string? LinkedInUrl, string? GitHubUrl, string? Summary);

public record EducationDto(
    Guid? Id, string Institution, string Degree, string FieldOfStudy,
    DateTime StartDate, DateTime? EndDate, bool IsCurrent, string? Grade);

public record ExperienceDto(
    Guid? Id, string Company, string JobTitle, string Location,
    DateTime StartDate, DateTime? EndDate, bool IsCurrent, string Description);

public record ProjectDto(
    Guid? Id, string Name, string Description, string? Url, List<string> Technologies);

public record CreateResumeRequest(
    string Title,
    string Template,
    PersonalInfoDto Personal,
    List<EducationDto> Educations,
    List<ExperienceDto> Experiences,
    List<string> Skills,
    List<ProjectDto> Projects);

public record UpdateResumeRequest(
    string? Title,
    string? Template,
    PersonalInfoDto? Personal,
    List<EducationDto>? Educations,
    List<ExperienceDto>? Experiences,
    List<string>? Skills,
    List<ProjectDto>? Projects);

public record ResumeDto(
    Guid Id, Guid UserId, string Title, string Template, bool IsDefault,
    PersonalInfoDto Personal,
    List<EducationDto> Educations,
    List<ExperienceDto> Experiences,
    List<string> Skills,
    List<ProjectDto> Projects,
    DateTime CreatedAt);
