using System.Threading.Channels;
using PathfinderPhotography.Data;
using PathfinderPhotography.Models;
using Microsoft.EntityFrameworkCore;

namespace PathfinderPhotography.Services;

public class AiProcessingBackgroundService : BackgroundService
{
    private readonly Channel<AiAnalysisRequest> _queue;
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<AiProcessingBackgroundService> _logger;

    public AiProcessingBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<AiProcessingBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
        
        // Create unbounded channel for queueing requests
        _queue = Channel.CreateUnbounded<AiAnalysisRequest>(new UnboundedChannelOptions
        {
            SingleReader = true, // Only one background worker reads
            SingleWriter = false // Multiple threads can enqueue
        });
    }

    public async Task QueueAnalysisAsync(AiAnalysisRequest request)
    {
        await _queue.Writer.WriteAsync(request);
        _logger.LogInformation("Queued AI analysis for submission {SubmissionId}", request.SubmissionId);
    }

    public int GetQueuedCount()
    {
        return _queue.Reader.Count;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("AI Processing Background Service started");

        await foreach (AiAnalysisRequest request in _queue.Reader.ReadAllAsync(stoppingToken))
        {
            try
            {
                _logger.LogInformation("Processing AI analysis for submission {SubmissionId}", request.SubmissionId);
                
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    PhotoAnalysisService photoAnalysisService = scope.ServiceProvider.GetRequiredService<PhotoAnalysisService>();
                    IDbContextFactory<ApplicationDbContext> contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                    
                    // Update status to Processing
                    await UpdateStatusAsync(contextFactory, request.SubmissionId, 
                        AiProcessingStatus.Processing, startTime: DateTime.UtcNow);
                    
                    // Perform AI analysis
                    PhotoAnalysisResult result = await photoAnalysisService.AnalyzePhotoAsync(
                        request.ImageData,
                        request.ImagePath,
                        request.CompositionRule);
                    
                    // Save results
                    await SaveResultsAsync(contextFactory, request.SubmissionId, result);
                    
                    _logger.LogInformation("Completed AI analysis for submission {SubmissionId}: Title='{Title}', Rating={Rating}",
                        request.SubmissionId, result.Title, result.Rating);
                }
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "AI analysis failed for submission {SubmissionId}", request.SubmissionId);
                
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    IDbContextFactory<ApplicationDbContext> contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                    await UpdateStatusAsync(contextFactory, request.SubmissionId,
                        AiProcessingStatus.Failed, error: ex.Message, completedTime: DateTime.UtcNow);
                }
            }
        }
        
        _logger.LogInformation("AI Processing Background Service stopped");
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
        catch (Exception ex)
        {
            _logger.LogWarning(ex, "Failed to update AI processing status for submission {SubmissionId}", submissionId);
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
            submission.AiMarketingImageData = result.MarketingImageData;
            submission.AiMarketingImagePrompt = result.MarketingImagePrompt;
            submission.AiProcessingStatus = AiProcessingStatus.Completed;
            submission.AiProcessingCompletedTime = DateTime.UtcNow;
            submission.AiProcessingError = null; // Clear any previous error
            
            await context.SaveChangesAsync();
        }
    }
}
