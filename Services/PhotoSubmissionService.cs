using PathfinderPhotography.Models;
using PathfinderPhotography.Data;
using Microsoft.EntityFrameworkCore;

namespace PathfinderPhotography.Services;

public class PhotoSubmissionService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private readonly IWebHostEnvironment _env;

    public PhotoSubmissionService(IDbContextFactory<ApplicationDbContext> contextFactory, IWebHostEnvironment env)
    {
        _contextFactory = contextFactory;
        _env = env;
    }

    public async Task<List<PhotoSubmission>> GetAllSubmissionsAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PhotoSubmissions
            .OrderByDescending(s => s.SubmissionDate)
            .ToListAsync();
    }

    public async Task<List<PhotoSubmission>> GetSubmissionsByPathfinderAsync(string pathfinderEmail)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PhotoSubmissions
            .Where(s => s.PathfinderEmail.ToLower() == pathfinderEmail.ToLower())
            .OrderByDescending(s => s.SubmissionDate)
            .ToListAsync();
    }

    public async Task<PhotoSubmission?> GetLatestSubmissionForRuleAsync(string pathfinderEmail, int compositionRuleId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PhotoSubmissions
            .Where(s => s.PathfinderEmail.ToLower() == pathfinderEmail.ToLower() 
                     && s.CompositionRuleId == compositionRuleId)
            .OrderByDescending(s => s.SubmissionVersion)
            .FirstOrDefaultAsync();
    }

    public async Task AddSubmissionAsync(PhotoSubmission submission)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        submission.SubmissionDate = DateTime.UtcNow;
        context.PhotoSubmissions.Add(submission);
        await context.SaveChangesAsync();
    }

    public async Task<string> SaveUploadedFileAsync(Stream fileStream, string fileName)
    {
        var uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        var uploadsPath = Path.Combine(_env.WebRootPath, "uploads");
        var filePath = Path.Combine(uploadsPath, uniqueFileName);
        
        if (!Directory.Exists(uploadsPath))
        {
            Directory.CreateDirectory(uploadsPath);
        }

        using (var fileStreamOutput = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileStreamOutput);
        }

        return uniqueFileName;
    }

    public async Task<(byte[] imageData, string contentType)> SaveImageDataAsync(Stream fileStream, string fileName)
    {
        using var memoryStream = new MemoryStream();
        await fileStream.CopyToAsync(memoryStream);
        var imageData = memoryStream.ToArray();
        
        // Validate file content by checking magic bytes/file signatures
        var detectedContentType = DetectImageTypeFromMagicBytes(imageData);
        if (detectedContentType == null)
        {
            throw new InvalidOperationException("Invalid image file. The file content does not match any supported image format.");
        }
        
        // Validate that extension matches detected content type
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var expectedContentType = extension switch
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

    private string? DetectImageTypeFromMagicBytes(byte[] data)
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
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var submission = await context.PhotoSubmissions.FindAsync(submissionId);
        if (submission == null)
        {
            throw new InvalidOperationException($"Submission with ID {submissionId} not found.");
        }
        
        submission.GradeStatus = status;
        submission.GradedBy = gradedBy;
        submission.GradedDate = DateTime.UtcNow;
        await context.SaveChangesAsync();
    }

    public async Task<List<PhotoSubmission>> GetSubmissionsForGradingAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PhotoSubmissions
            .OrderBy(s => s.GradeStatus)
            .ThenByDescending(s => s.SubmissionDate)
            .ToListAsync();
    }

    public async Task<PhotoSubmission?> GetSubmissionByIdAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PhotoSubmissions.FindAsync(id);
    }
}
