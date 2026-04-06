using Microsoft.EntityFrameworkCore;
using PaymentService.Models;

namespace PaymentService.Data;

public class PaymentDbContext(DbContextOptions<PaymentDbContext> options) : DbContext(options)
{
    public DbSet<RecruiterWallet> Wallets => Set<RecruiterWallet>();
    public DbSet<Transaction> Transactions => Set<Transaction>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecruiterWallet>(e =>
        {
            e.HasKey(w => w.Id);
            e.HasIndex(w => w.RecruiterId).IsUnique();
        });

        modelBuilder.Entity<Transaction>(e =>
        {
            e.HasKey(t => t.Id);
            e.Property(t => t.Type).HasMaxLength(50);
            e.Property(t => t.Status).HasMaxLength(50);
            e.Property(t => t.Reason).HasMaxLength(500);
        });
    }
}
