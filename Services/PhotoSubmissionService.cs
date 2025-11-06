using PathfinderPhotography.Models;
using PathfinderPhotography.Data;
using Microsoft.EntityFrameworkCore;

namespace PathfinderPhotography.Services;

public class PhotoSubmissionService(IDbContextFactory<ApplicationDbContext> contextFactory, IWebHostEnvironment env)
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
        context.PhotoSubmissions.Add(submission);
        await context.SaveChangesAsync();
    }

    public async Task<string> SaveUploadedFileAsync(Stream fileStream, string fileName)
    {
        string uniqueFileName = $"{Guid.NewGuid()}_{fileName}";
        string uploadsPath = Path.Combine(env.WebRootPath, "uploads");
        string filePath = Path.Combine(uploadsPath, uniqueFileName);
        
        if (!Directory.Exists(uploadsPath))
        {
            Directory.CreateDirectory(uploadsPath);
        }

        await using (FileStream fileStreamOutput = new FileStream(filePath, FileMode.Create))
        {
            await fileStream.CopyToAsync(fileStreamOutput);
        }

        return uniqueFileName;
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
