using Microsoft.EntityFrameworkCore;
using PathfinderPhotography.Models;

namespace PathfinderPhotography.Data;

public class ApplicationDbContext : DbContext
{
    public ApplicationDbContext(DbContextOptions<ApplicationDbContext> options)
        : base(options)
    {
    }

    public DbSet<PhotoSubmission> PhotoSubmissions { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure PhotoSubmission entity
        modelBuilder.Entity<PhotoSubmission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PathfinderName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CompositionRuleName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ImagePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.SubmissionDate).IsRequired();
            
            entity.HasIndex(e => e.PathfinderName);
            entity.HasIndex(e => e.CompositionRuleId);
        });
    }
}
