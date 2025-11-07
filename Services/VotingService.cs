using PathfinderPhotography.Models;
using PathfinderPhotography.Data;
using Microsoft.EntityFrameworkCore;

namespace PathfinderPhotography.Services;

public class VotingService(IDbContextFactory<ApplicationDbContext> contextFactory)
{
    private const double KFactor = 32.0; // Standard ELO K-factor

    /// <summary>
    /// Get two random photos for comparison, excluding photos from the current user
    /// </summary>
    public async Task<(PhotoSubmission?, PhotoSubmission?)> GetRandomPhotoPairAsync(string currentUserEmail)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        
        // Get all submissions excluding current user's photos
        List<PhotoSubmission> submissions = await context.PhotoSubmissions
            .Where(s => s.PathfinderEmail.ToLower() != currentUserEmail.ToLower())
            .ToListAsync();

        if (submissions.Count < 2)
        {
            return (null, null);
        }

        // Randomly select two different photos using Random.Shared for better randomness
        int photo1Index = Random.Shared.Next(submissions.Count);
        List<int> remainingIndices = Enumerable.Range(0, submissions.Count).Where(i => i != photo1Index).ToList();
        int photo2Index = remainingIndices[Random.Shared.Next(remainingIndices.Count)];

        return (submissions[photo1Index], submissions[photo2Index]);
    }

    /// <summary>
    /// Record a vote and update ELO ratings
    /// </summary>
    public async Task RecordVoteAsync(string voterEmail, int winnerPhotoId, int loserPhotoId)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        
        // Check if user already voted on this pair
        PhotoVote? existingVote = await context.PhotoVotes
            .FirstOrDefaultAsync(v => v.VoterEmail.Equals(voterEmail, StringComparison.CurrentCultureIgnoreCase) &&
                ((v.WinnerPhotoId == winnerPhotoId && v.LoserPhotoId == loserPhotoId) ||
                 (v.WinnerPhotoId == loserPhotoId && v.LoserPhotoId == winnerPhotoId)));

        if (existingVote != null)
        {
            // User already voted on this pair, don't record duplicate
            return;
        }

        // Get the photos
        PhotoSubmission? winnerPhoto = await context.PhotoSubmissions.FindAsync(winnerPhotoId);
        PhotoSubmission? loserPhoto = await context.PhotoSubmissions.FindAsync(loserPhotoId);

        if (winnerPhoto == null || loserPhoto == null)
        {
            throw new InvalidOperationException("One or both photos not found.");
        }

        // Calculate ELO changes
        (double newWinnerRating, double newLoserRating) = CalculateEloRatings(
            winnerPhoto.EloRating, 
            loserPhoto.EloRating
        );

        // Update ratings
        winnerPhoto.EloRating = newWinnerRating;
        loserPhoto.EloRating = newLoserRating;

        // Record the vote
        PhotoVote vote = new()
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
    private static (double newWinnerRating, double newLoserRating) CalculateEloRatings(
        double winnerRating, 
        double loserRating)
    {
        // Expected score for winner
        double expectedWinner = 1.0 / (1.0 + Math.Pow(10, (loserRating - winnerRating) / 400.0));
        
        // Expected score for loser
        double expectedLoser = 1.0 / (1.0 + Math.Pow(10, (winnerRating - loserRating) / 400.0));

        // New ratings (winner gets 1 point, loser gets 0 points)
        double newWinnerRating = winnerRating + KFactor * (1.0 - expectedWinner);
        double newLoserRating = loserRating + KFactor * (0.0 - expectedLoser);

        return (newWinnerRating, newLoserRating);
    }

    /// <summary>
    /// Get top photos by ELO rating
    /// </summary>
    public async Task<List<PhotoSubmission>> GetTopPhotosByRatingAsync(int count = 20)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        
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
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        
        return await context.PhotoVotes
            .Where(v => v.WinnerPhotoId == photoId || v.LoserPhotoId == photoId)
            .CountAsync();
    }

    /// <summary>
    /// Check if user can vote (has submissions to compare)
    /// </summary>
    public async Task<bool> CanUserVoteAsync(string userEmail)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        
        int otherSubmissionsCount = await context.PhotoSubmissions
            .Where(s => s.PathfinderEmail.ToLower() != userEmail.ToLower())
            .CountAsync();

        return otherSubmissionsCount >= 2;
    }
}
