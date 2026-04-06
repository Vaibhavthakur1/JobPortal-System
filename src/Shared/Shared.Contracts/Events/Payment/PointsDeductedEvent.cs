namespace Shared.Contracts.Events.Payment;

public record PointsDeductedEvent(
    Guid RecruiterId,
    int PointsDeducted,
    int RemainingBalance,
    string Reason,
    DateTime DeductedAt);
