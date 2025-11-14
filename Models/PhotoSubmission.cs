namespace PathfinderPhotography.Models;

public class PhotoSubmission
{
    public int Id { get; set; }
    public string PathfinderName { get; set; } = string.Empty;
    public string PathfinderEmail { get; set; } = string.Empty;
    public int CompositionRuleId { get; set; }
    public string CompositionRuleName { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public byte[]? ImageData { get; set; }
    public string? ImageContentType { get; set; }
    public string Description { get; set; } = string.Empty;
    public DateTime SubmissionDate { get; set; } = DateTime.Now;
    public GradeStatus GradeStatus { get; set; } = GradeStatus.NotGraded;
    public string? GradedBy { get; set; }
    public DateTime? GradedDate { get; set; }
    public int SubmissionVersion { get; set; } = 1;
    public int? PreviousSubmissionId { get; set; }
    public double EloRating { get; set; } = 1000.0;
    public string? AiTitle { get; set; }
    public string? AiDescription { get; set; }
    public int? AiRating { get; set; }
    public string? AiMarketingHeadline { get; set; }
    public string? AiMarketingCopy { get; set; }
    public decimal? AiSuggestedPrice { get; set; }
    public string? AiSocialMediaText { get; set; }
}

public enum GradeStatus
{
    NotGraded = 0,
    Pass = 1,
    Fail = 2
}
