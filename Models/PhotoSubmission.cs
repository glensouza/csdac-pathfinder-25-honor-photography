namespace PathfinderPhotography.Models;

public class PhotoSubmission
{
    public int Id { get; set; }
    public string PathfinderName { get; set; } = string.Empty;
    public int CompositionRuleId { get; set; }
    public string CompositionRuleName { get; set; } = string.Empty;
    public string ImagePath { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public DateTime SubmissionDate { get; set; } = DateTime.Now;
}
