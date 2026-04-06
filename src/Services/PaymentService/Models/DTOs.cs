namespace PaymentService.Models;

public record PurchasePointsRequest(int Points, decimal Amount, string Currency, string PaymentGatewayRef);
public record DeductPointsRequest(Guid RecruiterId, int Points, string Reason);

public record WalletDto(Guid RecruiterId, int PointsBalance, DateTime UpdatedAt);
public record TransactionDto(Guid Id, string Type, int Points, decimal? Amount, string Reason, string Status, DateTime CreatedAt);
public record PaymentInitRequest(int Points, string Currency);
public record PaymentInitResponse(string OrderId, decimal Amount, string Currency, string GatewayKey);
