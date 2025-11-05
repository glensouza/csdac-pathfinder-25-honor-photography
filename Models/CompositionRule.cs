namespace PathfinderPhotography.Models;

public class CompositionRule
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string SampleImagePath { get; set; } = string.Empty;
    public string DetailedExplanation { get; set; } = string.Empty;
}
