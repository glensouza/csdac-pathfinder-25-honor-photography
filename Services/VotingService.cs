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

    /// <summary>
    /// Recalculate ELO ratings for specific photos by replaying all votes involving those photos
    /// </summary>
    /// <param name="photoIds">List of photo IDs that need ELO recalculation</param>
    /// <param name="voteIdsToExclude">List of vote IDs to exclude from recalculation (votes being deleted)</param>
    public async Task RecalculateEloRatingsForPhotosAsync(List<int> photoIds, List<int>? voteIdsToExclude = null)
    {
        if (photoIds == null || photoIds.Count == 0) return;

        voteIdsToExclude ??= new List<int>();

        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        
        // Get all votes involving these photos (excluding votes that will be deleted), ordered by date
        List<PhotoVote> votes = await context.PhotoVotes
            .Where(v => (photoIds.Contains(v.WinnerPhotoId) || photoIds.Contains(v.LoserPhotoId))
                     && !voteIdsToExclude.Contains(v.Id))
            .OrderBy(v => v.VoteDate)
            .ToListAsync();

        // Get all photo IDs that participate in these votes (not just the ones we're recalculating)
        HashSet<int> allPhotoIdsInVotes = new HashSet<int>(photoIds);
        foreach (PhotoVote vote in votes)
        {
            allPhotoIdsInVotes.Add(vote.WinnerPhotoId);
            allPhotoIdsInVotes.Add(vote.LoserPhotoId);
        }

        // Load all photos involved in the votes
        Dictionary<int, PhotoSubmission> photosDict = await context.PhotoSubmissions
            .Where(p => allPhotoIdsInVotes.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        // Reset ELO ratings to default ONLY for photos that were in the original photoIds list
        foreach (int photoId in photoIds)
        {
            if (photosDict.TryGetValue(photoId, out PhotoSubmission? photo))
            {
                photo.EloRating = 1000.0;
            }
        }

        // Replay all votes to recalculate ratings
        foreach (PhotoVote vote in votes)
        {
            if (photosDict.TryGetValue(vote.WinnerPhotoId, out PhotoSubmission? winnerPhoto) &&
                photosDict.TryGetValue(vote.LoserPhotoId, out PhotoSubmission? loserPhoto))
            {
                (double newWinnerRating, double newLoserRating) = CalculateEloRatings(
                    winnerPhoto.EloRating,
                    loserPhoto.EloRating
                );

                winnerPhoto.EloRating = newWinnerRating;
                loserPhoto.EloRating = newLoserRating;
            }
        }

        await context.SaveChangesAsync();
    }
}
