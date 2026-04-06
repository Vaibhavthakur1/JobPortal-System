namespace PaymentService.Models;

public class RecruiterWallet
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecruiterId { get; set; }
    public int PointsBalance { get; set; } = 0;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

public class Transaction
{
    public Guid Id { get; set; } = Guid.NewGuid();
    public Guid RecruiterId { get; set; }
    public string Type { get; set; } = string.Empty; // Purchase | Deduction
    public int Points { get; set; }
    public decimal? Amount { get; set; }
    public string? Currency { get; set; }
    public string Reason { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending"; // Pending | Completed | Failed
    public string? PaymentGatewayRef { get; set; }
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
}
