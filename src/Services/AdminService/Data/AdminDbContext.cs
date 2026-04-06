using AdminService.Models;
using Microsoft.EntityFrameworkCore;

namespace AdminService.Data;

public class AdminDbContext(DbContextOptions<AdminDbContext> options) : DbContext(options)
{
    public DbSet<AuditLog> AuditLogs => Set<AuditLog>();
    public DbSet<FlaggedJob> FlaggedJobs => Set<FlaggedJob>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<AuditLog>(e =>
        {
            e.HasKey(a => a.Id);
            e.HasIndex(a => a.CreatedAt);
            e.Property(a => a.Action).HasMaxLength(200);
            e.Property(a => a.Entity).HasMaxLength(100);
        });

        modelBuilder.Entity<FlaggedJob>(e =>
        {
            e.HasKey(f => f.Id);
            e.Property(f => f.Status).HasMaxLength(50);
            e.Property(f => f.Reason).HasMaxLength(500);
        });
    }
}
