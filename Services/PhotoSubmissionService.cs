using PathfinderPhotography.Models;
using PathfinderPhotography.Data;
using Microsoft.EntityFrameworkCore;

namespace PathfinderPhotography.Services;

public class PhotoSubmissionService(
    IDbContextFactory<ApplicationDbContext> contextFactory, 
    EmailNotificationService emailService,
    UserService userService,
    AiProcessingBackgroundService aiProcessingService,
    CertificateService certificateService,
    ILogger<PhotoSubmissionService> logger)
{
    public async Task<List<PhotoSubmission>> GetAllSubmissionsAsync()
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        return await context.PhotoSubmissions
            .OrderByDescending(s => s.SubmissionDate)
            .ToListAsync();
    }

    public async Task<List<PhotoSubmission>> GetSubmissionsByPathfinderAsync(string pathfinderEmail)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        return await context.PhotoSubmissions
            .Where(s => s.PathfinderEmail.ToLower() == pathfinderEmail.ToLower())
            .OrderByDescending(s => s.SubmissionDate)
            .ToListAsync();
    }

    public async Task<PhotoSubmission?> GetLatestSubmissionForRuleAsync(string pathfinderEmail, int compositionRuleId)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        return await context.PhotoSubmissions
            .Where(s => s.PathfinderEmail.ToLower() == pathfinderEmail.ToLower()
                        && s.CompositionRuleId == compositionRuleId)
            .OrderByDescending(s => s.SubmissionVersion)
            .FirstOrDefaultAsync();
    }

    public async Task AddSubmissionAsync(PhotoSubmission submission)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        submission.SubmissionDate = DateTime.UtcNow;
        
        // Set initial AI processing status
        if (submission.ImageData != null && submission.ImageData.Length > 0)
        {
            submission.AiProcessingStatus = Models.AiProcessingStatus.Queued;
        }
        
        context.PhotoSubmissions.Add(submission);
        await context.SaveChangesAsync();

        // Send notification to instructors asynchronously (don't wait)
        #pragma warning disable CS4014
        Task.Run(() => this.SendNewSubmissionNotificationAsync(submission))
            .ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    logger.LogWarning(task.Exception, "Failed to send new submission email notification for rule {Rule}", submission.CompositionRuleName);
                }
            }, TaskContinuationOptions.OnlyOnFaulted);
        #pragma warning restore CS4014

        // Queue AI analysis in background service
        if (submission.ImageData != null && submission.ImageData.Length > 0)
        {
            await aiProcessingService.QueueAnalysisAsync(new AiAnalysisRequest
            {
                SubmissionId = submission.Id,
                ImageData = submission.ImageData,
                ImagePath = submission.ImagePath,
                CompositionRule = submission.CompositionRuleName
            });
        }
    }

    private async Task SendNewSubmissionNotificationAsync(PhotoSubmission submission)
    {
        try
        {
            List<User> instructors = await userService.GetInstructorsAndAdminsAsync();
            List<string> instructorEmails = instructors.Select(i => i.Email).ToList();

            if (instructorEmails.Any())
            {
                await emailService.SendNewSubmissionNotificationAsync(
                    submission.PathfinderEmail,
                    submission.PathfinderName,
                    submission.CompositionRuleName,
                    instructorEmails);
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException)
        {
            // Email notifications are non-critical, log and continue
            logger.LogWarning(ex, "Failed to send new submission email notification for rule {Rule}", submission.CompositionRuleName);
        }
    }

    public async Task<(byte[] imageData, string contentType)> SaveImageDataAsync(Stream fileStream, string fileName)
    {
        using MemoryStream memoryStream = new();
        await fileStream.CopyToAsync(memoryStream);
        byte[] imageData = memoryStream.ToArray();
        
        // Validate file content by checking magic bytes/file signatures
        string? detectedContentType = DetectImageTypeFromMagicBytes(imageData);
        if (detectedContentType == null)
        {
            throw new InvalidOperationException("Invalid image file. The file content does not match any supported image format.");
        }
        
        // Validate that extension matches detected content type
        string extension = Path.GetExtension(fileName).ToLowerInvariant();
        string? expectedContentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => null
        };
        
        if (expectedContentType != null && expectedContentType != detectedContentType)
        {
            throw new InvalidOperationException($"File extension '{extension}' does not match detected image type '{detectedContentType}'.");
        }

        return (imageData, detectedContentType);
    }

    private static string? DetectImageTypeFromMagicBytes(byte[] data)
    {
        if (data.Length < 12) return null;

        // JPEG: FF D8 FF
        if (data[0] == 0xFF && data[1] == 0xD8 && data[2] == 0xFF)
            return "image/jpeg";

        // PNG: 89 50 4E 47 0D 0A 1A 0A
        if (data[0] == 0x89 && data[1] == 0x50 && data[2] == 0x4E && data[3] == 0x47 &&
            data[4] == 0x0D && data[5] == 0x0A && data[6] == 0x1A && data[7] == 0x0A)
            return "image/png";

        // GIF: GIF87a or GIF89a
        if (data[0] == 0x47 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x38 &&
            (data[4] == 0x37 || data[4] == 0x39) && data[5] == 0x61)
            return "image/gif";

        // BMP: 42 4D
        if (data[0] == 0x42 && data[1] == 0x4D)
            return "image/bmp";

        // WebP: RIFF....WEBP
        if (data[0] == 0x52 && data[1] == 0x49 && data[2] == 0x46 && data[3] == 0x46 &&
            data[8] == 0x57 && data[9] == 0x45 && data[10] == 0x42 && data[11] == 0x50)
            return "image/webp";

        return null;
    }

    public async Task GradeSubmissionAsync(int submissionId, GradeStatus status, string gradedBy)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        
        PhotoSubmission? submission = await context.PhotoSubmissions.FindAsync(submissionId);
        if (submission == null)
        {
            throw new InvalidOperationException($"Submission with ID {submissionId} not found.");
        }
        
        submission.GradeStatus = status;
        submission.GradedBy = gradedBy;
        submission.GradedDate = DateTime.UtcNow;
        await context.SaveChangesAsync();

        // Send notification to pathfinder asynchronously (don't wait)
        #pragma warning disable CS4014
        Task.Run(() => this.SendGradingNotificationAsync(submission, status, gradedBy))
            .ContinueWith(task =>
            {
                if (task.Exception != null)
                {
                    logger.LogWarning(task.Exception, "Failed to send grading email notification for rule {Rule}", submission.CompositionRuleName);
                }
            }, TaskContinuationOptions.OnlyOnFaulted);

        // If graded as Pass, check if pathfinder has completed all rules and issue certificate
        if (status == GradeStatus.Pass)
        {
            Task.Run(() => this.CheckAndIssueCertificateAsync(submission.PathfinderEmail))
                .ContinueWith(task =>
                {
                    if (task.Exception != null)
                    {
                        logger.LogWarning(task.Exception, "Failed to check/issue completion certificate for {Email}", submission.PathfinderEmail);
                    }
                }, TaskContinuationOptions.OnlyOnFaulted);
        }
        #pragma warning restore CS4014
    }

    private async Task CheckAndIssueCertificateAsync(string pathfinderEmail)
    {
        try
        {
            bool hasCompleted = await certificateService.HasCompletedAllRulesAsync(pathfinderEmail);
            if (hasCompleted)
            {
                CompletionCertificate? existingCertificate = await certificateService.GetCertificateAsync(pathfinderEmail);
                if (existingCertificate == null)
                {
                    await certificateService.GenerateAndStoreCertificateAsync(pathfinderEmail);
                    await certificateService.SendCertificateEmailAsync(pathfinderEmail);
                    logger.LogInformation("Completion certificate issued to {Email}", pathfinderEmail);
                }
            }
        }
        catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException)
        {
            logger.LogWarning(ex, "Failed to check/issue certificate for {Email}", pathfinderEmail);
        }
    }

    private async Task SendGradingNotificationAsync(PhotoSubmission submission, GradeStatus status, string gradedBy)
    {
        try
        {
            await emailService.SendGradingNotificationAsync(
                submission.PathfinderEmail,
                submission.PathfinderName,
                submission.CompositionRuleName,
                status,
                gradedBy);
        }
        catch (Exception ex) when (ex is not OutOfMemoryException && ex is not StackOverflowException && ex is not OperationCanceledException)
        {
            // Email notifications are non-critical, log and continue
            logger.LogWarning(ex, "Failed to send grading email notification for rule {Rule}", submission.CompositionRuleName);
        }
    }

    public async Task<List<PhotoSubmission>> GetSubmissionsForGradingAsync()
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        return await context.PhotoSubmissions
            .OrderBy(s => s.GradeStatus)
            .ThenByDescending(s => s.SubmissionDate)
            .ToListAsync();
    }

    public async Task<PhotoSubmission?> GetSubmissionByIdAsync(int id)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        return await context.PhotoSubmissions.FindAsync(id);
    }

    public async Task RetryAiAnalysisAsync(int submissionId)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        PhotoSubmission? submission = await context.PhotoSubmissions.FindAsync(submissionId);
        
        if (submission == null)
        {
            throw new InvalidOperationException($"Submission with ID {submissionId} not found.");
        }

        if (submission.ImageData == null || submission.ImageData.Length == 0)
        {
            throw new InvalidOperationException($"Submission {submissionId} has no image data.");
        }

        logger.LogInformation("Manually retrying AI analysis for submission {Id}", submissionId);

        // Reset status to Queued
        submission.AiProcessingStatus = Models.AiProcessingStatus.Queued;
        submission.AiProcessingError = null;
        await context.SaveChangesAsync();

        // Queue for processing via background service
        await aiProcessingService.QueueAnalysisAsync(new AiAnalysisRequest
        {
            SubmissionId = submission.Id,
            ImageData = submission.ImageData,
            ImagePath = submission.ImagePath,
            CompositionRule = submission.CompositionRuleName
        });
    }

}
