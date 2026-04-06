namespace JobCatalogService.Models;

public record CreateJobRequest(
    string Title, string Description, string Company,
    string Location, string JobType, string Industry,
    decimal? SalaryMin, decimal? SalaryMax,
    int ExperienceYears, List<string> RequiredSkills);

public record UpdateJobRequest(
    string? Title, string? Description, string? Location,
    string? JobType, decimal? SalaryMin, decimal? SalaryMax,
    int? ExperienceYears, List<string>? RequiredSkills);

public record JobSearchRequest(
    string? Keyword, string? Location, string? JobType,
    string? Industry, decimal? MinSalary, decimal? MaxSalary,
    int? MinExperience, List<string>? Skills,
    int Page = 1, int PageSize = 20);

public record JobDto(
    Guid Id, Guid RecruiterId, string Title, string Description,
    string Company, string Location, string JobType, string Industry,
    decimal? SalaryMin, decimal? SalaryMax, int ExperienceYears,
    List<string> RequiredSkills, string Status, DateTime CreatedAt);

public record PagedResult<T>(List<T> Items, int TotalCount, int Page, int PageSize);
