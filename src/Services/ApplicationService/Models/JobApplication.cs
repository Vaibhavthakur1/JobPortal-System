namespace ApplicationService.Models;

public class JobApplication
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid JobSeekerId { get; set; }
    public Guid JobId { get; set; }
    public Guid ResumeId { get; set; }
    public string Status { get; set; } = "Draft"; // Draft→Submitted→Screening→Interview→Offered→Accepted/Rejected
    public string? CoverLetter { get; set; }
    public bool IsWithdrawn { get; set; } = false;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
    public List<ApplicationStatusHistory> StatusHistory { get; set; } = [];
}

public class ApplicationStatusHistory
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid ApplicationId { get; set; }
    public string FromStatus { get; set; } = string.Empty;
    public string ToStatus { get; set; } = string.Empty;
    public string? Note { get; set; }
    public DateTime ChangedAt { get; set; } = DateTime.UtcNow;
}
