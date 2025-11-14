namespace PathfinderPhotography.Services;

public class AiAnalysisRequest
{
    public int SubmissionId { get; set; }
    public byte[] ImageData { get; set; } = Array.Empty<byte>();
    public string ImagePath { get; set; } = string.Empty;
    public string CompositionRule { get; set; } = string.Empty;
}
