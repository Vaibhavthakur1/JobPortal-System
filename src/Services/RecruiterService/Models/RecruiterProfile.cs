namespace RecruiterService.Models;

public class RecruiterProfile
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid UserId { get; set; }
    public string CompanyName { get; set; } = string.Empty;
    public string CompanyDescription { get; set; } = string.Empty;
    public string Industry { get; set; } = string.Empty;
    public string Website { get; set; } = string.Empty;
    public string Location { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class CandidatePipeline
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecruiterId { get; set; }
    public Guid JobId { get; set; }
    public Guid CandidateId { get; set; }
    public Guid ApplicationId { get; set; }
    public string Stage { get; set; } = "New"; // New | Shortlisted | Contacted | Rejected
    public string? Notes { get; set; }
    public bool ResumeViewed { get; set; } = false;
    public bool ContactUnlocked { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}
