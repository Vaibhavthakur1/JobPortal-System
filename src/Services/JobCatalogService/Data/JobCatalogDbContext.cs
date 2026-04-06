using JobCatalogService.Models;
using Microsoft.EntityFrameworkCore;
using System.Text.Json;

namespace JobCatalogService.Data;

public class JobCatalogDbContext(DbContextOptions<JobCatalogDbContext> options) : DbContext(options)
{
    public DbSet<Job> Jobs => Set<Job>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Job>(e =>
        {
            e.HasKey(j => j.Id);
            e.Property(j => j.Title).IsRequired().HasMaxLength(300);
            e.Property(j => j.Company).IsRequired().HasMaxLength(200);
            e.Property(j => j.Status).HasMaxLength(50);
            // Store skills as JSON
            e.Property(j => j.RequiredSkills)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new());
        });
    }
}
