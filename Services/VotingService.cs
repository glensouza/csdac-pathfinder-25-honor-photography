using PathfinderPhotography.Models;
using PathfinderPhotography.Data;
using Microsoft.EntityFrameworkCore;

namespace PathfinderPhotography.Services;

public class VotingService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;
    private const double K_FACTOR = 32.0; // Standard ELO K-factor

    public VotingService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    /// <summary>
    /// Get two random photos for comparison, excluding photos from the current user
    /// </summary>
    public async Task<(PhotoSubmission?, PhotoSubmission?)> GetRandomPhotoPairAsync(string currentUserEmail)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // Get all submissions excluding current user's photos
        var submissions = await context.PhotoSubmissions
            .Where(s => s.PathfinderEmail.ToLower() != currentUserEmail.ToLower())
            .ToListAsync();

        if (submissions.Count < 2)
        {
            return (null, null);
        }

        // Randomly select two different photos
        var random = new Random();
        var photo1Index = random.Next(submissions.Count);
        var photo2Index = random.Next(submissions.Count);
        
        // Ensure we get two different photos
        while (photo2Index == photo1Index)
        {
            photo2Index = random.Next(submissions.Count);
        }

        return (submissions[photo1Index], submissions[photo2Index]);
    }

    /// <summary>
    /// Record a vote and update ELO ratings
    /// </summary>
    public async Task RecordVoteAsync(string voterEmail, int winnerPhotoId, int loserPhotoId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        // Check if user already voted on this pair
        var existingVote = await context.PhotoVotes
            .FirstOrDefaultAsync(v => v.VoterEmail.ToLower() == voterEmail.ToLower() &&
                ((v.WinnerPhotoId == winnerPhotoId && v.LoserPhotoId == loserPhotoId) ||
                 (v.WinnerPhotoId == loserPhotoId && v.LoserPhotoId == winnerPhotoId)));

        if (existingVote != null)
        {
            // User already voted on this pair, don't record duplicate
            return;
        }

        // Get the photos
        var winnerPhoto = await context.PhotoSubmissions.FindAsync(winnerPhotoId);
        var loserPhoto = await context.PhotoSubmissions.FindAsync(loserPhotoId);

        if (winnerPhoto == null || loserPhoto == null)
        {
            throw new InvalidOperationException("One or both photos not found.");
        }

        // Calculate ELO changes
        var (newWinnerRating, newLoserRating) = CalculateEloRatings(
            winnerPhoto.EloRating, 
            loserPhoto.EloRating
        );

        // Update ratings
        winnerPhoto.EloRating = newWinnerRating;
        loserPhoto.EloRating = newLoserRating;

        // Record the vote
        var vote = new PhotoVote
        {
            VoterEmail = voterEmail,
            WinnerPhotoId = winnerPhotoId,
            LoserPhotoId = loserPhotoId,
            VoteDate = DateTime.UtcNow
        };

        context.PhotoVotes.Add(vote);
        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Calculate new ELO ratings after a match
    /// </summary>
    private (double newWinnerRating, double newLoserRating) CalculateEloRatings(
        double winnerRating, 
        double loserRating)
    {
        // Expected score for winner
        var expectedWinner = 1.0 / (1.0 + Math.Pow(10, (loserRating - winnerRating) / 400.0));
        
        // Expected score for loser
        var expectedLoser = 1.0 / (1.0 + Math.Pow(10, (winnerRating - loserRating) / 400.0));

        // New ratings (winner gets 1 point, loser gets 0 points)
        var newWinnerRating = winnerRating + K_FACTOR * (1.0 - expectedWinner);
        var newLoserRating = loserRating + K_FACTOR * (0.0 - expectedLoser);

        return (newWinnerRating, newLoserRating);
    }

    /// <summary>
    /// Get top photos by ELO rating
    /// </summary>
    public async Task<List<PhotoSubmission>> GetTopPhotosByRatingAsync(int count = 20)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.PhotoSubmissions
            .OrderByDescending(s => s.EloRating)
            .ThenByDescending(s => s.SubmissionDate)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// Get vote count for a specific photo
    /// </summary>
    public async Task<int> GetVoteCountForPhotoAsync(int photoId)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        return await context.PhotoVotes
            .Where(v => v.WinnerPhotoId == photoId || v.LoserPhotoId == photoId)
            .CountAsync();
    }

    /// <summary>
    /// Check if user can vote (has submissions to compare)
    /// </summary>
    public async Task<bool> CanUserVoteAsync(string userEmail)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var otherSubmissionsCount = await context.PhotoSubmissions
            .Where(s => s.PathfinderEmail.ToLower() != userEmail.ToLower())
            .CountAsync();

        return otherSubmissionsCount >= 2;
    }
}
