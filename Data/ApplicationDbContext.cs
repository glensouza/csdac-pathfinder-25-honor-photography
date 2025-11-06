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
    public DbSet<User> Users { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        // Configure PhotoSubmission entity
        modelBuilder.Entity<PhotoSubmission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PathfinderName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PathfinderEmail).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CompositionRuleName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ImagePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.SubmissionDate).IsRequired();
            entity.Property(e => e.GradeStatus).IsRequired();
            entity.Property(e => e.GradedBy).HasMaxLength(200);
            entity.Property(e => e.SubmissionVersion).IsRequired();
            
            entity.HasIndex(e => e.PathfinderName);
            entity.HasIndex(e => e.PathfinderEmail);
            entity.HasIndex(e => e.CompositionRuleId);
            entity.HasIndex(e => e.GradeStatus);
        });

        // Configure User entity
        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.CreatedDate).IsRequired();
            
            entity.HasIndex(e => e.Email).IsUnique();
        });
    }
}
