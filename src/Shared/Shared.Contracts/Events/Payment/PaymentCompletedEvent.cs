namespace Shared.Contracts.Events.Payment;

public record PaymentCompletedEvent(
    Guid PaymentId,
    Guid RecruiterId,
    int PointsPurchased,
    decimal AmountPaid,
    string Currency,
    DateTime CompletedAt);
