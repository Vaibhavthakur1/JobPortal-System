using MassTransit;
using PaymentService.Models;
using PaymentService.Repositories;
using Shared.Contracts.Events.Payment;

namespace PaymentService.Services;

public class PaymentService(IWalletRepository walletRepo, IPublishEndpoint publisher, IConfiguration config) : IPaymentService
{
    // Points pricing: 100 points = $1
    private const int PointsPerDollar = 100;

    public async Task<PaymentInitResponse> InitiatePaymentAsync(Guid recruiterId, PaymentInitRequest request)
    {
        var amount = (decimal)request.Points / PointsPerDollar;
        var orderId = $"ORDER_{recruiterId}_{DateTime.UtcNow.Ticks}";

        // Create pending transaction
        var wallet = await walletRepo.GetOrCreateAsync(recruiterId);
        await walletRepo.AddTransactionAsync(new Transaction
        {
            RecruiterId = recruiterId,
            Type = "Purchase",
            Points = request.Points,
            Amount = amount,
            Currency = request.Currency,
            Reason = $"Purchase {request.Points} points",
            Status = "Pending",
            PaymentGatewayRef = orderId
        });

        return new PaymentInitResponse(orderId, amount, request.Currency, config["Payment:GatewayKey"] ?? "test_key");
    }

    public async Task<WalletDto> ConfirmPaymentAsync(Guid recruiterId, PurchasePointsRequest request)
    {
        var wallet = await walletRepo.GetOrCreateAsync(recruiterId);
        wallet.PointsBalance += request.Points;
        await walletRepo.UpdateAsync(wallet);

        await walletRepo.AddTransactionAsync(new Transaction
        {
            RecruiterId = recruiterId,
            Type = "Purchase",
            Points = request.Points,
            Amount = request.Amount,
            Currency = request.Currency,
            Reason = $"Purchased {request.Points} points",
            Status = "Completed",
            PaymentGatewayRef = request.PaymentGatewayRef
        });

        await publisher.Publish(new PaymentCompletedEvent(
            Guid.NewGuid(), recruiterId, request.Points, request.Amount, request.Currency, DateTime.UtcNow));

        return new WalletDto(recruiterId, wallet.PointsBalance, wallet.UpdatedAt ?? wallet.CreatedAt);
    }

    public async Task<WalletDto> DeductPointsAsync(DeductPointsRequest request)
    {
        var wallet = await walletRepo.GetOrCreateAsync(request.RecruiterId);

        if (wallet.PointsBalance < request.Points)
            throw new InvalidOperationException("Insufficient points balance.");

        wallet.PointsBalance -= request.Points;
        await walletRepo.UpdateAsync(wallet);

        await walletRepo.AddTransactionAsync(new Transaction
        {
            RecruiterId = request.RecruiterId,
            Type = "Deduction",
            Points = -request.Points,
            Reason = request.Reason,
            Status = "Completed"
        });

        await publisher.Publish(new PointsDeductedEvent(
            request.RecruiterId, request.Points, wallet.PointsBalance, request.Reason, DateTime.UtcNow));

        return new WalletDto(request.RecruiterId, wallet.PointsBalance, wallet.UpdatedAt ?? wallet.CreatedAt);
    }

    public async Task<WalletDto> GetWalletAsync(Guid recruiterId)
    {
        var wallet = await walletRepo.GetOrCreateAsync(recruiterId);
        return new WalletDto(recruiterId, wallet.PointsBalance, wallet.UpdatedAt ?? wallet.CreatedAt);
    }

    public async Task<IEnumerable<TransactionDto>> GetTransactionHistoryAsync(Guid recruiterId, int page, int pageSize)
    {
        var txns = await walletRepo.GetTransactionsAsync(recruiterId, page, pageSize);
        return txns.Select(t => new TransactionDto(t.Id, t.Type, t.Points, t.Amount, t.Reason, t.Status, t.CreatedAt));
    }
}
