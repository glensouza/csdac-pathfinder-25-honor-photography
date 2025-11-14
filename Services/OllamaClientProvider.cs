using OllamaSharp;

namespace PathfinderPhotography.Services;

public class OllamaClientProvider : IOllamaClientProvider
{
    private readonly IOllamaApiClient _client;

    public OllamaClientProvider(IConfiguration configuration, ILogger<OllamaClientProvider> logger)
    {
        string endpoint = configuration["AI:Ollama:Endpoint"] ?? "http://localhost:11434";
        
        logger.LogInformation("Initializing Ollama client with endpoint: {Endpoint}", endpoint);
        
        _client = new OllamaApiClient(new Uri(endpoint));
    }

    public IOllamaApiClient GetClient() => _client;
}
