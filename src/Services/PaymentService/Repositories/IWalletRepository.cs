using PaymentService.Models;

namespace PaymentService.Repositories;

public interface IWalletRepository
{
    Task<RecruiterWallet?> GetByRecruiterIdAsync(Guid recruiterId);
    Task<RecruiterWallet> GetOrCreateAsync(Guid recruiterId);
    Task UpdateAsync(RecruiterWallet wallet);
    Task AddTransactionAsync(Transaction transaction);
    Task UpdateTransactionAsync(Transaction transaction);
    Task<Transaction?> GetTransactionAsync(Guid transactionId);
    Task<IEnumerable<Transaction>> GetTransactionsAsync(Guid recruiterId, int page, int pageSize);
}
