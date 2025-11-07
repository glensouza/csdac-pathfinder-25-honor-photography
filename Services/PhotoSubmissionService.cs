using PathfinderPhotography.Models;
using PathfinderPhotography.Data;
using Microsoft.EntityFrameworkCore;

namespace PathfinderPhotography.Services;

public class PhotoSubmissionService(
    IDbContextFactory<ApplicationDbContext> contextFactory, 
    IWebHostEnvironment env,
    EmailNotificationService emailService,
    UserService userService,
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
            .Where(s => s.PathfinderEmail.Equals(pathfinderEmail, StringComparison.CurrentCultureIgnoreCase))
            .OrderByDescending(s => s.SubmissionDate)
            .ToListAsync();
    }

    public async Task<PhotoSubmission?> GetLatestSubmissionForRuleAsync(string pathfinderEmail, int compositionRuleId)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        return await context.PhotoSubmissions
            .Where(s => s.PathfinderEmail.Equals(pathfinderEmail, StringComparison.CurrentCultureIgnoreCase) 
                     && s.CompositionRuleId == compositionRuleId)
            .OrderByDescending(s => s.SubmissionVersion)
            .FirstOrDefaultAsync();
    }

    public async Task AddSubmissionAsync(PhotoSubmission submission)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        submission.SubmissionDate = DateTime.UtcNow;
        context.PhotoSubmissions.Add(submission);
        await context.SaveChangesAsync();

        // Send notification to instructors asynchronously (don't wait)
        _ = Task.Run(async () =>
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
            catch (Exception ex)
            {
                // Email notifications are non-critical, log and continue
                logger.LogWarning(ex, "Failed to send new submission email notification for rule {Rule}", submission.CompositionRuleName);
            }
        });
    }

    public async Task<string> SaveUploadedFileAsync(Stream fileStream, string fileName)
    {
        string uniqueFileName = $"{Guid.CreateVersion7()}_{fileName}";
        string uploadsPath = Path.Combine(env.WebRootPath, "uploads");
        string filePath = Path.Combine(uploadsPath, uniqueFileName);
        
        if (!Directory.Exists(uploadsPath))
        {
            Directory.CreateDirectory(uploadsPath);
        }

        await using FileStream fileStreamOutput = new(filePath, FileMode.Create);
        await fileStream.CopyToAsync(fileStreamOutput);

        return uniqueFileName;
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
        _ = Task.Run(async () =>
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
            catch (Exception ex)
            {
                // Email notifications are non-critical, log and continue
                logger.LogWarning(ex, "Failed to send grading email notification for rule {Rule}", submission.CompositionRuleName);
            }
        });
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
}
