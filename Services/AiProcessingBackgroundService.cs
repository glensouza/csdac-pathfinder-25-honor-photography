using System.Text.Json;
using System.Threading.Channels;
using PathfinderPhotography.Data;
using PathfinderPhotography.Models;
using Microsoft.EntityFrameworkCore;

namespace PathfinderPhotography.Services;

public class AiProcessingBackgroundService(
    IServiceProvider serviceProvider,
    ILogger<AiProcessingBackgroundService> logger)
    : BackgroundService
{
    private readonly Channel<AiAnalysisRequest> queue = Channel.CreateUnbounded<AiAnalysisRequest>(new UnboundedChannelOptions
    {
        SingleReader = true, // Only one background worker reads
        SingleWriter = false // Multiple threads can enqueue
    });

    private int queuedCount = 0;
    private readonly Lock countLock = new();

    // Create unbounded channel for queueing requests
    // Only one background worker reads
    // Multiple threads can enqueue

    public async Task QueueAnalysisAsync(AiAnalysisRequest request)
    {
        await this.queue.Writer.WriteAsync(request);
        lock (this.countLock)
        {
            this.queuedCount++;
        }

        logger.LogInformation("Queued AI analysis for submission {SubmissionId}", request.SubmissionId);
    }

    public int GetQueuedCount()
    {
        lock (this.countLock)
        {
            return this.queuedCount;
        }
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        logger.LogInformation("AI Processing Background Service started");

        await foreach (AiAnalysisRequest request in this.queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                logger.LogInformation("Processing AI analysis for submission {SubmissionId}", request.SubmissionId);
                
                using (IServiceScope scope = serviceProvider.CreateScope())
                {
                    PhotoAnalysisService photoAnalysisService = scope.ServiceProvider.GetRequiredService<PhotoAnalysisService>();
                    IDbContextFactory<ApplicationDbContext> contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                    
                    // Update status to Processing
                    await this.UpdateStatusAsync(contextFactory, request.SubmissionId, 
                        AiProcessingStatus.Processing, startTime: DateTime.UtcNow);
                    
                    // Perform AI analysis
                    PhotoAnalysisResult result = await photoAnalysisService.AnalyzePhotoAsync(
                        request.ImageData,
                        request.ImagePath,
                        request.CompositionRule);
                    
                    // Save results
                    await this.SaveResultsAsync(contextFactory, request.SubmissionId, result);
                    
                    lock (this.countLock)
                    {
                        this.queuedCount--;
                    }

                    logger.LogInformation("Completed AI analysis for submission {SubmissionId}: Title='{Title}', Rating={Rating}",
                        request.SubmissionId, result.Title, result.Rating);
                }
            }
            catch (HttpRequestException ex)
            {
                logger.LogError(ex, "Network error while analyzing submission {SubmissionId}: Ollama may be unavailable", request.SubmissionId);
                
                using (IServiceScope scope = serviceProvider.CreateScope())
                {
                    IDbContextFactory<ApplicationDbContext> contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                    await this.UpdateStatusAsync(contextFactory, request.SubmissionId,
                        AiProcessingStatus.Failed, error: "Ollama service unavailable", completedTime: DateTime.UtcNow);
                }
                
                lock (this.countLock)
                {
                    this.queuedCount--;
                }
            }
            catch (JsonException ex)
            {
                logger.LogError(ex, "Failed to parse AI response for submission {SubmissionId}", request.SubmissionId);
                
                using (IServiceScope scope = serviceProvider.CreateScope())
                {
                    IDbContextFactory<ApplicationDbContext> contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                    await this.UpdateStatusAsync(contextFactory, request.SubmissionId,
                        AiProcessingStatus.Failed, error: "AI response parsing failed", completedTime: DateTime.UtcNow);
                }
                
                lock (this.countLock)
                {
                    this.queuedCount--;
                }
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "AI analysis failed for submission {SubmissionId}", request.SubmissionId);
                
                using (IServiceScope scope = serviceProvider.CreateScope())
                {
                    IDbContextFactory<ApplicationDbContext> contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                    await this.UpdateStatusAsync(contextFactory, request.SubmissionId,
                        AiProcessingStatus.Failed, error: ex.Message, completedTime: DateTime.UtcNow);
                }
                
                lock (this.countLock)
                {
                    this.queuedCount--;
                }
            }
        }

        logger.LogInformation("AI Processing Background Service stopped");
    }

    private async Task UpdateStatusAsync(IDbContextFactory<ApplicationDbContext> contextFactory, 
        int submissionId, AiProcessingStatus status, string? error = null, 
        DateTime? startTime = null, DateTime? completedTime = null)
    {
        try
        {
            await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
            PhotoSubmission? submission = await context.PhotoSubmissions.FindAsync(submissionId);
            
            if (submission != null)
            {
                submission.AiProcessingStatus = status;
                
                if (error != null)
                {
                    submission.AiProcessingError = error;
                }
                
                if (startTime.HasValue)
                {
                    submission.AiProcessingStartTime = startTime.Value;
                }
                
                if (completedTime.HasValue)
                {
                    submission.AiProcessingCompletedTime = completedTime.Value;
                }
                
                await context.SaveChangesAsync();
            }
        }
        catch (DbUpdateException ex)
        {
            logger.LogWarning(ex, "Database update failed while updating AI processing status for submission {SubmissionId}", submissionId);
        }
        catch (InvalidOperationException ex)
        {
            logger.LogWarning(ex, "Invalid operation while updating AI processing status for submission {SubmissionId}", submissionId);
        }
    }

    private async Task SaveResultsAsync(IDbContextFactory<ApplicationDbContext> contextFactory,
        int submissionId, PhotoAnalysisResult result)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        PhotoSubmission? submission = await context.PhotoSubmissions.FindAsync(submissionId);
        
        if (submission != null)
        {
            submission.AiTitle = result.Title;
            submission.AiDescription = result.Description;
            submission.AiRating = result.Rating;
            submission.AiMarketingHeadline = result.MarketingHeadline;
            submission.AiMarketingCopy = result.MarketingCopy;
            submission.AiSuggestedPrice = result.SuggestedPrice;
            submission.AiSocialMediaText = result.SocialMediaText;
            submission.AiProcessingStatus = AiProcessingStatus.Completed;
            submission.AiProcessingCompletedTime = DateTime.UtcNow;
            submission.AiProcessingError = null; // Clear any previous error
            
            await context.SaveChangesAsync();
        }
    }
}
