using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using MassTransit;
using PaymentService.Models;
using PaymentService.Repositories;
using Shared.Contracts.Events.Payment;

namespace PaymentService.Services;

public class PaymentService(
    IWalletRepository walletRepo,
    IPublishEndpoint publisher,
    IConfiguration config,
    IHttpClientFactory httpClientFactory,
    ILogger<PaymentService> logger) : IPaymentService
{
    // Pricing: 100 points = ₹99
    private static decimal PointsToRupees(int points) =>
        Math.Round((points / 100m) * 99m, 2);

    public async Task<PaymentInitResponse> InitiatePaymentAsync(Guid recruiterId, PaymentInitRequest request)
    {
        var amountInr   = PointsToRupees(request.Points);
        var amountPaise = (long)(amountInr * 100); // Razorpay uses paise
        var keyId       = config["Razorpay:KeyId"]!;
        var keySecret   = config["Razorpay:KeySecret"]!;

        // Create Razorpay order via REST API
        var client = httpClientFactory.CreateClient("Razorpay");
        var credentials = Convert.ToBase64String(Encoding.UTF8.GetBytes($"{keyId}:{keySecret}"));
        client.DefaultRequestHeaders.Authorization = new AuthenticationHeaderValue("Basic", credentials);

        var body = JsonSerializer.Serialize(new
        {
            amount   = amountPaise,
            currency = "INR",
            receipt  = $"rcpt_{recruiterId}_{DateTime.UtcNow.Ticks}",
            notes    = new { recruiterId = recruiterId.ToString(), points = request.Points.ToString() }
        });

        var response = await client.PostAsync("https://api.razorpay.com/v1/orders",
            new StringContent(body, Encoding.UTF8, "application/json"));

        string orderId;
        if (response.IsSuccessStatusCode)
        {
            var json = await response.Content.ReadAsStringAsync();
            using var doc = JsonDocument.Parse(json);
            orderId = doc.RootElement.GetProperty("id").GetString()!;
        }
        else
        {
            var errorBody = await response.Content.ReadAsStringAsync();
            logger.LogWarning("Razorpay order creation failed ({Status}): {Body}", response.StatusCode, errorBody);
            // Fallback for local dev without valid secret
            orderId = $"order_dev_{recruiterId}_{DateTime.UtcNow.Ticks}";
        }

        // Persist pending transaction
        await walletRepo.GetOrCreateAsync(recruiterId);
        await walletRepo.AddTransactionAsync(new Transaction
        {
            RecruiterId       = recruiterId,
            Type              = "Purchase",
            Points            = request.Points,
            Amount            = amountInr,
            Currency          = "INR",
            Reason            = $"Purchase {request.Points} points",
            Status            = "Pending",
            PaymentGatewayRef = orderId
        });

        return new PaymentInitResponse(orderId, amountInr, "INR", keyId);
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

    public async Task CancelPaymentAsync(Guid recruiterId, string orderId, string status)
    {
        var tx = await walletRepo.GetTransactionByGatewayRefAsync(orderId);
        if (tx is null || tx.RecruiterId != recruiterId || tx.Status != "Pending") return;
        tx.Status = status; // "Cancelled" or "Failed"
        await walletRepo.UpdateTransactionAsync(tx);
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
