using OllamaSharp;

namespace PathfinderPhotography.Services;

public interface IOllamaClientProvider
{
    IOllamaApiClient GetClient();
}
