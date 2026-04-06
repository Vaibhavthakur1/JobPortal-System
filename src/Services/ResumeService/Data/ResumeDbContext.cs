using Microsoft.EntityFrameworkCore;
using ResumeService.Models;
using System.Text.Json;

namespace ResumeService.Data;

public class ResumeDbContext(DbContextOptions<ResumeDbContext> options) : DbContext(options)
{
    public DbSet<Resume> Resumes => Set<Resume>();
    public DbSet<PersonalInfo> PersonalInfos => Set<PersonalInfo>();
    public DbSet<Education> Educations => Set<Education>();
    public DbSet<Experience> Experiences => Set<Experience>();
    public DbSet<Project> Projects => Set<Project>();

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        modelBuilder.Entity<Resume>(e =>
        {
            e.HasKey(r => r.Id);
            e.HasIndex(r => r.UserId);
            e.Property(r => r.Template).HasMaxLength(50);
            e.Property(r => r.Skills)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new());
            e.HasOne(r => r.Personal).WithOne()
                .HasForeignKey<PersonalInfo>(p => p.ResumeId);
            e.HasMany(r => r.Educations).WithOne()
                .HasForeignKey(ed => ed.ResumeId);
            e.HasMany(r => r.Experiences).WithOne()
                .HasForeignKey(ex => ex.ResumeId);
            e.HasMany(r => r.Projects).WithOne()
                .HasForeignKey(p => p.ResumeId);
        });

        modelBuilder.Entity<Project>(e =>
        {
            e.Property(p => p.Technologies)
                .HasConversion(
                    v => JsonSerializer.Serialize(v, (JsonSerializerOptions?)null),
                    v => JsonSerializer.Deserialize<List<string>>(v, (JsonSerializerOptions?)null) ?? new());
        });
    }
}
