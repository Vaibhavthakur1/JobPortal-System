using Microsoft.EntityFrameworkCore;
using RecruiterService.Models;

namespace RecruiterService.Data;

public class RecruiterDbContext(DbContextOptions<RecruiterDbContext> options) : DbContext(options)
{
    public DbSet<RecruiterProfile> Profiles => Set<RecruiterProfile>();
    public DbSet<CandidatePipeline> Pipelines => Set<CandidatePipeline>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<RecruiterProfile>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => p.UserId).IsUnique();
            e.Property(p => p.CompanyName).IsRequired().HasMaxLength(200);
        });

        modelBuilder.Entity<CandidatePipeline>(e =>
        {
            e.HasKey(p => p.Id);
            e.HasIndex(p => new { p.RecruiterId, p.ApplicationId }).IsUnique();
            e.Property(p => p.Stage).HasMaxLength(50);
        });
    }
}
