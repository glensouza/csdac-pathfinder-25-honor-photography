using Microsoft.EntityFrameworkCore;
using PathfinderPhotography.Models;

namespace PathfinderPhotography.Data;

public class ApplicationDbContext(DbContextOptions<ApplicationDbContext> options) : DbContext(options)
{
    public DbSet<PhotoSubmission> PhotoSubmissions { get; set; }
    public DbSet<User> Users { get; set; }
    public DbSet<PhotoVote> PhotoVotes { get; set; }
    public DbSet<CompletionCertificate> CompletionCertificates { get; set; }

    protected override void OnModelCreating(ModelBuilder modelBuilder)
    {
        base.OnModelCreating(modelBuilder);

        modelBuilder.HasPostgresExtension("citext");

        modelBuilder.Entity<PhotoSubmission>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PathfinderName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.PathfinderEmail).IsRequired().HasMaxLength(200).HasColumnType("citext");
            entity.Property(e => e.CompositionRuleName).IsRequired().HasMaxLength(100);
            entity.Property(e => e.ImagePath).IsRequired().HasMaxLength(500);
            entity.Property(e => e.ImageData);
            entity.Property(e => e.ImageContentType).HasMaxLength(100);
            entity.Property(e => e.Description).IsRequired().HasMaxLength(2000);
            entity.Property(e => e.SubmissionDate).IsRequired();
            entity.Property(e => e.GradeStatus).IsRequired();
            entity.Property(e => e.GradedBy).HasMaxLength(200);
            entity.Property(e => e.SubmissionVersion).IsRequired();
            entity.Property(e => e.EloRating).IsRequired().HasDefaultValue(1000.0);
            
            entity.HasIndex(e => e.PathfinderName);
            entity.HasIndex(e => e.PathfinderEmail);
            entity.HasIndex(e => e.CompositionRuleId);
            entity.HasIndex(e => e.GradeStatus);
        });

        modelBuilder.Entity<User>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.Email).IsRequired().HasMaxLength(200).HasColumnType("citext");
            entity.Property(e => e.Name).IsRequired().HasMaxLength(200);
            entity.Property(e => e.Role).IsRequired();
            entity.Property(e => e.CreatedDate).IsRequired();
            
            entity.HasIndex(e => e.Email).IsUnique();
        });

        modelBuilder.Entity<PhotoVote>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.VoterEmail).IsRequired().HasMaxLength(200).HasColumnType("citext");
            entity.Property(e => e.WinnerPhotoId).IsRequired();
            entity.Property(e => e.LoserPhotoId).IsRequired();
            entity.Property(e => e.VoteDate).IsRequired();
            
            entity.HasIndex(e => e.VoterEmail);
            entity.HasIndex(e => new { e.VoterEmail, e.WinnerPhotoId, e.LoserPhotoId });
        });

        modelBuilder.Entity<CompletionCertificate>(entity =>
        {
            entity.HasKey(e => e.Id);
            entity.Property(e => e.PathfinderEmail).IsRequired().HasMaxLength(200).HasColumnType("citext");
            entity.Property(e => e.PathfinderName).IsRequired().HasMaxLength(200);
            entity.Property(e => e.CompletionDate).IsRequired();
            entity.Property(e => e.CertificatePdfData).IsRequired();
            entity.Property(e => e.IssuedDate).IsRequired();
            entity.Property(e => e.EmailSent).IsRequired();
            
            entity.HasIndex(e => e.PathfinderEmail);
            entity.HasIndex(e => e.CompletionDate);
        });
    }
}
