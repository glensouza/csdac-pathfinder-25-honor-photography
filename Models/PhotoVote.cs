namespace PathfinderPhotography.Models;

public class PhotoVote
{
    public int Id { get; set; }
    public string VoterEmail { get; set; } = string.Empty;
    public int WinnerPhotoId { get; set; }
    public int LoserPhotoId { get; set; }
    public DateTime VoteDate { get; set; } = DateTime.UtcNow;
}
