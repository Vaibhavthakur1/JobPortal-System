using Microsoft.EntityFrameworkCore;
using PaymentService.Data;
using PaymentService.Models;

namespace PaymentService.Repositories;

public class WalletRepository(PaymentDbContext db) : IWalletRepository
{
    public async Task<RecruiterWallet?> GetByRecruiterIdAsync(Guid recruiterId) =>
        await db.Wallets.FirstOrDefaultAsync(w => w.RecruiterId == recruiterId);

    public async Task<RecruiterWallet> GetOrCreateAsync(Guid recruiterId)
    {
        var wallet = await GetByRecruiterIdAsync(recruiterId);
        if (wallet is not null) return wallet;

        wallet = new RecruiterWallet { RecruiterId = recruiterId };
        await db.Wallets.AddAsync(wallet);
        await db.SaveChangesAsync();
        return wallet;
    }

    public async Task UpdateAsync(RecruiterWallet wallet)
    {
        wallet.UpdatedAt = DateTime.UtcNow;
        db.Wallets.Update(wallet);
        await db.SaveChangesAsync();
    }

    public async Task AddTransactionAsync(Transaction transaction)
    {
        await db.Transactions.AddAsync(transaction);
        await db.SaveChangesAsync();
    }

    public async Task UpdateTransactionAsync(Transaction transaction)
    {
        db.Transactions.Update(transaction);
        await db.SaveChangesAsync();
    }

    public async Task<Transaction?> GetTransactionAsync(Guid transactionId) =>
        await db.Transactions.FindAsync(transactionId);

    public async Task<IEnumerable<Transaction>> GetTransactionsAsync(Guid recruiterId, int page, int pageSize) =>
        await db.Transactions
            .Where(t => t.RecruiterId == recruiterId)
            .OrderByDescending(t => t.CreatedAt)
            .Skip((page - 1) * pageSize).Take(pageSize)
            .ToListAsync();
}
