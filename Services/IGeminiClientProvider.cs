using Google.Cloud.AIPlatform.V1;

namespace PathfinderPhotography.Services;

public interface IGeminiClientProvider
{
    PredictionServiceClient GetPredictionClient();
    string GetProjectId();
    string GetLocation();
}
