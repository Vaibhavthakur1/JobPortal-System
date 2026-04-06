namespace JobCatalogService.Models;

public class Job
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecruiterId { get; set; }
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Company { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public string JobType { get; set; } = string.Empty; // Full-time | Part-time | Contract | Remote
    public string Industry { get; set; } = string.Empty;
    public decimal? SalaryMin { get; set; }
    public decimal? SalaryMax { get; set; }
    public int ExperienceYears { get; set; }
    public List<string> RequiredSkills { get; set; } = [];
    public string Status { get; set; } = "Active"; // Active | Closed | Archived
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ClosedAt { get; set; }
}
