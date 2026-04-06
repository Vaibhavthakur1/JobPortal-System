using PaymentService.Models;

namespace PaymentService.Services;

public interface IPaymentService
{
    Task<PaymentInitResponse> InitiatePaymentAsync(Guid recruiterId, PaymentInitRequest request);
    Task<WalletDto> ConfirmPaymentAsync(Guid recruiterId, PurchasePointsRequest request);
    Task<WalletDto> DeductPointsAsync(DeductPointsRequest request);
    Task<WalletDto> GetWalletAsync(Guid recruiterId);
    Task<IEnumerable<TransactionDto>> GetTransactionHistoryAsync(Guid recruiterId, int page, int pageSize);
}
