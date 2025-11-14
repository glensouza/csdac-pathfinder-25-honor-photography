using Microsoft.EntityFrameworkCore;
using PathfinderPhotography.Data;
using PathfinderPhotography.Models;

namespace PathfinderPhotography.Services;

public class CertificateService(
    IDbContextFactory<ApplicationDbContext> contextFactory,
    PdfExportService pdfExportService,
    EmailNotificationService emailService,
    CompositionRuleService ruleService,
    ILogger<CertificateService> logger)
{
    /// <summary>
    /// Check if a pathfinder has completed all composition rules
    /// </summary>
    public async Task<bool> HasCompletedAllRulesAsync(string pathfinderEmail)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        
        List<CompositionRule> allRules = ruleService.GetAllRules();
        int totalRules = allRules.Count;

        int passedRulesCount = await context.PhotoSubmissions
            .Where(s => s.PathfinderEmail == pathfinderEmail && s.GradeStatus == GradeStatus.Pass)
            .Select(s => s.CompositionRuleId)
            .Distinct()
            .CountAsync();

        return passedRulesCount >= totalRules;
    }

    /// <summary>
    /// Generate and store a completion certificate for a pathfinder
    /// </summary>
    public async Task<CompletionCertificate> GenerateAndStoreCertificateAsync(string pathfinderEmail)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        
        // Check if certificate already exists
        CompletionCertificate? existingCertificate = await context.CompletionCertificates
            .Where(c => c.PathfinderEmail == pathfinderEmail)
            .FirstOrDefaultAsync();

        if (existingCertificate != null)
        {
            return existingCertificate;
        }

        // Verify completion
        bool hasCompleted = await this.HasCompletedAllRulesAsync(pathfinderEmail);
        if (!hasCompleted)
        {
            throw new InvalidOperationException("Pathfinder has not completed all composition rules.");
        }

        // Generate certificate PDF
        byte[] certificatePdf = await pdfExportService.GenerateCompletionCertificateAsync(pathfinderEmail);

        // Get pathfinder name
        string pathfinderName = await context.PhotoSubmissions
            .Where(s => s.PathfinderEmail == pathfinderEmail)
            .Select(s => s.PathfinderName)
            .FirstOrDefaultAsync() ?? "Unknown";

        // Get completion date (date of last passed submission)
        DateTime completionDate = await context.PhotoSubmissions
            .Where(s => s.PathfinderEmail == pathfinderEmail && s.GradeStatus == GradeStatus.Pass)
            .MaxAsync(s => s.GradedDate ?? s.SubmissionDate);

        // Store certificate
        CompletionCertificate certificate = new CompletionCertificate
        {
            PathfinderEmail = pathfinderEmail,
            PathfinderName = pathfinderName,
            CompletionDate = completionDate,
            CertificatePdfData = certificatePdf,
            IssuedDate = DateTime.UtcNow,
            EmailSent = false
        };

        context.CompletionCertificates.Add(certificate);
        await context.SaveChangesAsync();

        logger.LogInformation("Certificate generated and stored for {Email}", pathfinderEmail);

        return certificate;
    }

    /// <summary>
    /// Get certificate for a pathfinder
    /// </summary>
    public async Task<CompletionCertificate?> GetCertificateAsync(string pathfinderEmail)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        
        return await context.CompletionCertificates
            .Where(c => c.PathfinderEmail == pathfinderEmail)
            .FirstOrDefaultAsync();
    }

    /// <summary>
    /// Send certificate via email
    /// </summary>
    public async Task SendCertificateEmailAsync(string pathfinderEmail)
    {
        CompletionCertificate? certificate = await this.GetCertificateAsync(pathfinderEmail);
        
        if (certificate == null)
        {
            // Generate certificate if it doesn't exist
            certificate = await this.GenerateAndStoreCertificateAsync(pathfinderEmail);
        }

        // Send email
        await emailService.SendCompletionCertificateAsync(
            certificate.PathfinderEmail,
            certificate.PathfinderName,
            certificate.CertificatePdfData);

        // Update email sent flag
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        CompletionCertificate? dbCertificate = await context.CompletionCertificates
            .Where(c => c.Id == certificate.Id)
            .FirstOrDefaultAsync();

        if (dbCertificate != null)
        {
            dbCertificate.EmailSent = true;
            dbCertificate.EmailSentDate = DateTime.UtcNow;
            await context.SaveChangesAsync();
        }

        logger.LogInformation("Certificate email sent to {Email}", pathfinderEmail);
    }

    /// <summary>
    /// Check for newly completed pathfinders and generate/send certificates
    /// </summary>
    public async Task ProcessNewCompletionsAsync()
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        
        // Get all pathfinders who have passed all rules
        List<CompositionRule> allRules = ruleService.GetAllRules();
        int totalRules = allRules.Count;

        List<string> completedPathfinders = await context.PhotoSubmissions
            .Where(s => s.GradeStatus == GradeStatus.Pass)
            .GroupBy(s => s.PathfinderEmail)
            .Where(g => g.Select(s => s.CompositionRuleId).Distinct().Count() >= totalRules)
            .Select(g => g.Key)
            .ToListAsync();

        // Get pathfinders who already have certificates
        HashSet<string> pathfindersWithCertificates = (await context.CompletionCertificates
            .Select(c => c.PathfinderEmail)
            .ToListAsync())
            .ToHashSet();

        // Process new completions
        foreach (string pathfinderEmail in completedPathfinders)
        {
            if (pathfindersWithCertificates.Contains(pathfinderEmail))
            {
                continue;
            }

            try
            {
                await this.GenerateAndStoreCertificateAsync(pathfinderEmail);
                await this.SendCertificateEmailAsync(pathfinderEmail);
                logger.LogInformation("Processed new completion for {Email}", pathfinderEmail);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to process completion for {Email}", pathfinderEmail);
            }
        }
    }

    /// <summary>
    /// Send top photos report to pathfinders who have photos in top 3
    /// </summary>
    public async Task SendTopPhotosReportsAsync()
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        
        List<CompositionRule> allRules = ruleService.GetAllRules();
        
        // Get top 3 photos for each rule
        HashSet<string> pathfindersWithTopPhotos = new HashSet<string>();
        
        foreach (CompositionRule rule in allRules)
        {
            List<string> topPathfinders = await context.PhotoSubmissions
                .Where(s => s.CompositionRuleId == rule.Id)
                .OrderByDescending(s => s.EloRating)
                .ThenByDescending(s => s.SubmissionDate)
                .Take(3)
                .Select(s => s.PathfinderEmail)
                .ToListAsync();
            
            foreach (string email in topPathfinders)
            {
                pathfindersWithTopPhotos.Add(email);
            }
        }

        // Send reports to each pathfinder
        foreach (string pathfinderEmail in pathfindersWithTopPhotos)
        {
            try
            {
                byte[] reportPdf = await pdfExportService.GenerateTopPhotosReportAsync(pathfinderEmail);
                
                string pathfinderName = await context.PhotoSubmissions
                    .Where(s => s.PathfinderEmail == pathfinderEmail)
                    .Select(s => s.PathfinderName)
                    .FirstOrDefaultAsync() ?? "Unknown";

                await emailService.SendTopPhotosReportAsync(
                    pathfinderEmail,
                    pathfinderName,
                    reportPdf);

                logger.LogInformation("Top photos report sent to {Email}", pathfinderEmail);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to send top photos report to {Email}", pathfinderEmail);
            }
        }
    }
}
