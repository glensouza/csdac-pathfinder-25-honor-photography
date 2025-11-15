using Google.Api.Gax.ResourceNames;
using Google.Cloud.AIPlatform.V1;

namespace PathfinderPhotography.Services;

public class GeminiClientProvider : IGeminiClientProvider
{
    private readonly PredictionServiceClient client;
    private readonly string projectId;
    private readonly string location;

    public GeminiClientProvider(IConfiguration configuration, ILogger<GeminiClientProvider> logger)
    {
        string? apiKey = configuration["AI:Gemini:ApiKey"];
        
        if (string.IsNullOrWhiteSpace(apiKey))
        {
            throw new InvalidOperationException("Gemini API key is not configured. Please set AI:Gemini:ApiKey in configuration.");
        }

        this.projectId = configuration["AI:Gemini:ProjectId"] ?? throw new InvalidOperationException("Project ID is required");
        this.location = configuration["AI:Gemini:Location"] ?? "us-central1";
        
        logger.LogInformation("Initializing Gemini client for project: {ProjectId}, location: {Location}", 
            this.projectId, this.location);

        // Initialize the Vertex AI client with API key authentication
        PredictionServiceClientBuilder builder = new()
        {
            Endpoint = $"{this.location}-aiplatform.googleapis.com"
        };

        this.client = builder.Build();
    }

    public PredictionServiceClient GetPredictionClient() => this.client;
    
    public string GetProjectId() => this.projectId;
    
    public string GetLocation() => this.location;
}
