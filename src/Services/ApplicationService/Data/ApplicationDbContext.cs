using ApplicationService.Models;
using MassTransit;
using Microsoft.EntityFrameworkCore;

namespace ApplicationService.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<JobApplication> Applications => Set<JobApplication>();
    public DbSet<ApplicationStatusHistory> StatusHistories => Set<ApplicationStatusHistory>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<JobApplication>(e =>
        {
            e.HasKey(a => a.Id);
            e.Property(a => a.Status).HasMaxLength(50);
            e.HasMany(a => a.StatusHistory)
             .WithOne()
             .HasForeignKey(h => h.ApplicationId);
        });

        modelBuilder.Entity<ApplicationStatusHistory>(e =>
        {
            e.HasKey(h => h.Id);
        });

        // MassTransit saga state table
        modelBuilder.AddInboxStateEntity();
        modelBuilder.AddOutboxMessageEntity();
        modelBuilder.AddOutboxStateEntity();
    }
}
