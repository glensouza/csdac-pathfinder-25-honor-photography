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

    public async Task<List<PhotoSubmission>> GetSubmissionsByPathfinderAsync(string pathfinderName)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.PhotoSubmissions
            .Where(s => s.PathfinderName.ToLower() == pathfinderName.ToLower())
            .OrderByDescending(s => s.SubmissionDate)
            .ToListAsync();
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
}
