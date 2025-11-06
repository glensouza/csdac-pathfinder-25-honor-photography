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
        
        var extension = Path.GetExtension(fileName).ToLowerInvariant();
        var contentType = extension switch
        {
            ".jpg" or ".jpeg" => "image/jpeg",
            ".png" => "image/png",
            ".gif" => "image/gif",
            ".bmp" => "image/bmp",
            ".webp" => "image/webp",
            _ => throw new InvalidOperationException($"Unsupported image format: {extension}. Supported formats are: .jpg, .jpeg, .png, .gif, .bmp, .webp")
        };

        return (imageData, contentType);
    }

    public async Task<PhotoSubmission?> GetSubmissionWithImageAsync(int id)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PhotoSubmissions.FindAsync(id);
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
