using System.Text.Json;
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
    private int _queuedCount = 0;
    private readonly object _countLock = new object();

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
        lock (_countLock)
        {
            _queuedCount++;
        }
        _logger.LogInformation("Queued AI analysis for submission {SubmissionId}", request.SubmissionId);
    }

    public int GetQueuedCount()
    {
        lock (_countLock)
        {
            return _queuedCount;
        }
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
                    
                    lock (_countLock)
                    {
                        _queuedCount--;
                    }
                    
                    _logger.LogInformation("Completed AI analysis for submission {SubmissionId}: Title='{Title}', Rating={Rating}",
                        request.SubmissionId, result.Title, result.Rating);
                }
            }
            catch (HttpRequestException ex)
            {
                _logger.LogError(ex, "Network error while analyzing submission {SubmissionId}: Ollama may be unavailable", request.SubmissionId);
                
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    IDbContextFactory<ApplicationDbContext> contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                    await UpdateStatusAsync(contextFactory, request.SubmissionId,
                        AiProcessingStatus.Failed, error: "Ollama service unavailable", completedTime: DateTime.UtcNow);
                }
                
                lock (_countLock)
                {
                    _queuedCount--;
                }
            }
            catch (JsonException ex)
            {
                _logger.LogError(ex, "Failed to parse AI response for submission {SubmissionId}", request.SubmissionId);
                
                using (IServiceScope scope = _serviceProvider.CreateScope())
                {
                    IDbContextFactory<ApplicationDbContext> contextFactory = scope.ServiceProvider.GetRequiredService<IDbContextFactory<ApplicationDbContext>>();
                    await UpdateStatusAsync(contextFactory, request.SubmissionId,
                        AiProcessingStatus.Failed, error: "AI response parsing failed", completedTime: DateTime.UtcNow);
                }
                
                lock (_countLock)
                {
                    _queuedCount--;
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
                
                lock (_countLock)
                {
                    _queuedCount--;
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
        catch (DbUpdateException ex)
        {
            _logger.LogWarning(ex, "Database update failed while updating AI processing status for submission {SubmissionId}", submissionId);
        }
        catch (InvalidOperationException ex)
        {
            _logger.LogWarning(ex, "Invalid operation while updating AI processing status for submission {SubmissionId}", submissionId);
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
