using PathfinderPhotography.Models;
using PathfinderPhotography.Data;
using Microsoft.EntityFrameworkCore;
#pragma warning disable CA1862

namespace PathfinderPhotography.Services;

public class VotingService(IDbContextFactory<ApplicationDbContext> contextFactory)
{
    private const double KFactor = 32.0; // Standard ELO K-factor

    /// <summary>
    /// Get two random photos for comparison that the user has NOT already voted on.
    /// Excludes the current user's own submissions and any submissions that have been graded as Fail.
    /// Returns (null,null) when no new pairs remain.
    /// Pairs are only formed between photos that share the same CompositionRuleId.
    /// </summary>
    public async Task<(PhotoSubmission?, PhotoSubmission?)> GetRandomPhotoPairAsync(string currentUserEmail)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();

        // Load all other users' submissions (exclude failed submissions)
        List<PhotoSubmission> submissions = await context.PhotoSubmissions
            .Where(s => s.PathfinderEmail.ToLower() != currentUserEmail.ToLower() && s.GradeStatus != GradeStatus.Fail)
            .ToListAsync();

        if (submissions.Count < 2)
        {
            return (null, null);
        }

        // Build set of already voted pair keys (unordered)
        List<PhotoVote> userVotes = await context.PhotoVotes
            .Where(v => v.VoterEmail.ToLower() == currentUserEmail.ToLower())
            .ToListAsync();

        HashSet<string> votedPairs = new();
        foreach (PhotoVote vote in userVotes)
        {
            int a = Math.Min(vote.WinnerPhotoId, vote.LoserPhotoId);
            int b = Math.Max(vote.WinnerPhotoId, vote.LoserPhotoId);
            votedPairs.Add($"{a}-{b}");
        }

        // Generate all possible unseen pairs but only between photos with the same CompositionRuleId
        List<(PhotoSubmission first, PhotoSubmission second)> candidatePairs = new();
        for (int i = 0; i < submissions.Count - 1; i++)
        {
            for (int j = i + 1; j < submissions.Count; j++)
            {
                PhotoSubmission first = submissions[i];
                PhotoSubmission second = submissions[j];

                // Ensure composition rule matches
                if (first.CompositionRuleId != second.CompositionRuleId)
                {
                    continue;
                }

                int a = Math.Min(first.Id, second.Id);
                int b = Math.Max(first.Id, second.Id);
                string key = $"{a}-{b}";
                if (!votedPairs.Contains(key))
                {
                    candidatePairs.Add((first, second));
                }
            }
        }

        if (candidatePairs.Count == 0)
        {
            // User has voted on all possible same-rule pairs
            return (null, null);
        }

        // Pick a random unseen pair
        int selectedIndex = Random.Shared.Next(candidatePairs.Count);
        (PhotoSubmission firstPhoto, PhotoSubmission secondPhoto) = candidatePairs[selectedIndex];
        return (firstPhoto, secondPhoto);
    }

    /// <summary>
    /// Record a vote and update ELO ratings
    /// </summary>
    public async Task RecordVoteAsync(string voterEmail, int winnerPhotoId, int loserPhotoId)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();

        // Prevent duplicate votes on the same pair (any orientation)
        PhotoVote? existingVote = await context.PhotoVotes
            .FirstOrDefaultAsync(v => v.VoterEmail.ToLower() == voterEmail.ToLower() &&
                ((v.WinnerPhotoId == winnerPhotoId && v.LoserPhotoId == loserPhotoId) ||
                 (v.WinnerPhotoId == loserPhotoId && v.LoserPhotoId == winnerPhotoId)));

        if (existingVote != null)
        {
            return;
        }

        PhotoSubmission? winnerPhoto = await context.PhotoSubmissions.FindAsync(winnerPhotoId);
        PhotoSubmission? loserPhoto = await context.PhotoSubmissions.FindAsync(loserPhotoId);

        if (winnerPhoto == null || loserPhoto == null)
        {
            throw new InvalidOperationException("One or both photos not found.");
        }

        (double newWinnerRating, double newLoserRating) = CalculateEloRatings(
            winnerPhoto.EloRating,
            loserPhoto.EloRating
        );

        winnerPhoto.EloRating = newWinnerRating;
        loserPhoto.EloRating = newLoserRating;

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

    private static (double newWinnerRating, double newLoserRating) CalculateEloRatings(
        double winnerRating,
        double loserRating)
    {
        double expectedWinner = 1.0 / (1.0 + Math.Pow(10, (loserRating - winnerRating) / 400.0));
        double expectedLoser = 1.0 / (1.0 + Math.Pow(10, (winnerRating - loserRating) / 400.0));

        double newWinnerRating = winnerRating + KFactor * (1.0 - expectedWinner);
        double newLoserRating = loserRating + KFactor * (0.0 - expectedLoser);

        return (newWinnerRating, newLoserRating);
    }

    public async Task<List<PhotoSubmission>> GetTopPhotosByRatingAsync(int count = 20)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();

        return await context.PhotoSubmissions
            .Where(s => s.GradeStatus != GradeStatus.Fail)
            .OrderByDescending(s => s.EloRating)
            .ThenByDescending(s => s.SubmissionDate)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// Get top photos by ELO rating filtered by grade status
    /// </summary>
    public async Task<List<PhotoSubmission>> GetTopPhotosByRatingAndGradeStatusAsync(GradeStatus gradeStatus, int count = 20)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        
        return await context.PhotoSubmissions
            .Where(s => s.GradeStatus == gradeStatus)
            .OrderByDescending(s => s.EloRating)
            .ThenByDescending(s => s.SubmissionDate)
            .Take(count)
            .ToListAsync();
    }

    /// <summary>
    /// Get top photos for a specific composition rule, optionally filtered by grade status
    /// </summary>
    public async Task<List<PhotoSubmission>> GetTopPhotosByRuleAsync(int compositionRuleId, GradeStatus? gradeStatus = null, int count = 20)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();

        IQueryable<PhotoSubmission> query = context.PhotoSubmissions
            .Where(s => s.CompositionRuleId == compositionRuleId);

        if (gradeStatus != null)
        {
            query = query.Where(s => s.GradeStatus == gradeStatus.Value);
        }
        else
        {
            // Default behavior: exclude failed submissions from top lists
            query = query.Where(s => s.GradeStatus != GradeStatus.Fail);
        }

        return await query
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

    public async Task<bool> CanUserVoteAsync(string userEmail)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();

        // Only allow voting if there exists at least one composition rule with 2+ non-failed submissions from other users
        List<object?> otherSubmissionsByRule = await context.PhotoSubmissions
            .Where(s => s.PathfinderEmail.ToLower() != userEmail.ToLower() && s.GradeStatus != GradeStatus.Fail)
            .GroupBy(s => s.CompositionRuleId)
            .Select(g => new { CompositionRuleId = g.Key, Count = g.Count() })
            .ToListAsync<object?>();

        // Evaluate count condition
        foreach (object? item in otherSubmissionsByRule)
        {
            // Use reflection to read Count property because we selected an anonymous type
            int count = (int)item?.GetType().GetProperty("Count")!.GetValue(item)!;
            if (count >= 2)
            {
                return true;
            }
        }

        return false;
    }

    public async Task RecalculateEloRatingsForPhotosAsync(List<int> photoIds, List<int>? voteIdsToExclude = null)
    {
        if (photoIds == null || photoIds.Count == 0)
        {
            return;
        }

        voteIdsToExclude ??= new List<int>();

        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();

        List<PhotoVote> votes = await context.PhotoVotes
            .Where(v => (photoIds.Contains(v.WinnerPhotoId) || photoIds.Contains(v.LoserPhotoId))
                     && !voteIdsToExclude.Contains(v.Id))
            .OrderBy(v => v.VoteDate)
            .ToListAsync();

        HashSet<int> allPhotoIdsInVotes = new(photoIds);
        foreach (PhotoVote vote in votes)
        {
            allPhotoIdsInVotes.Add(vote.WinnerPhotoId);
            allPhotoIdsInVotes.Add(vote.LoserPhotoId);
        }

        Dictionary<int, PhotoSubmission> photosDict = await context.PhotoSubmissions
            .Where(p => allPhotoIdsInVotes.Contains(p.Id))
            .ToDictionaryAsync(p => p.Id);

        foreach (int photoId in photoIds)
        {
            if (photosDict.TryGetValue(photoId, out PhotoSubmission? photo))
            {
                photo.EloRating = 1000.0;
            }
        }

        foreach (PhotoVote vote in votes)
        {
            if (!photosDict.TryGetValue(vote.WinnerPhotoId, out PhotoSubmission? winnerPhoto) ||
                !photosDict.TryGetValue(vote.LoserPhotoId, out PhotoSubmission? loserPhoto))
            {
                continue;
            }

            (double newWinnerRating, double newLoserRating) = CalculateEloRatings(
                winnerPhoto.EloRating,
                loserPhoto.EloRating
            );

            winnerPhoto.EloRating = newWinnerRating;
            loserPhoto.EloRating = newLoserRating;
        }

        await context.SaveChangesAsync();
    }

    /// <summary>
    /// Get top photos for many rules in a single query using a window function.
    /// Returns a dictionary keyed by CompositionRuleId with up to perRuleCount items each.
    /// If gradeStatus is null, failed submissions are excluded (so Top Photos page never shows failed photos by default).
    /// </summary>
    public async Task<Dictionary<int, List<PhotoSubmission>>> GetTopPhotosByRuleBulkAsync(GradeStatus? gradeStatus, int perRuleCount = 3)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();

        IQueryable<PhotoSubmission> query = context.PhotoSubmissions.AsNoTracking();

        if (gradeStatus == null)
        {
            query = query.Where(s => s.GradeStatus != GradeStatus.Fail);
        }
        else
        {
            query = query.Where(s => s.GradeStatus == gradeStatus.Value);
        }

        List<PhotoSubmission> submissions = await query
            .OrderByDescending(s => s.EloRating)
            .ThenByDescending(s => s.SubmissionDate)
            .ToListAsync();

        Dictionary<int, List<PhotoSubmission>> grouped = new Dictionary<int, List<PhotoSubmission>>();

        foreach (PhotoSubmission submission in submissions)
        {
            int key = submission.CompositionRuleId;
            if (!grouped.TryGetValue(key, out List<PhotoSubmission>? list))
            {
                list = new List<PhotoSubmission>();
                grouped[key] = list;
            }

            if (list.Count < perRuleCount)
            {
                list.Add(submission);
            }
        }

        return grouped;
    }

    // --- Admin helpers for votes ---

    public async Task<List<PhotoVote>> GetAllVotesAsync()
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        return await context.PhotoVotes
            .OrderByDescending(v => v.VoteDate)
            .ToListAsync();
    }

    /// <summary>
    /// Delete a vote by id and recalculate ELO ratings for affected photos
    /// </summary>
    public async Task DeleteVoteAsync(int voteId, string? actorEmail = null)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        PhotoVote? vote = await context.PhotoVotes.FindAsync(voteId);
        if (vote == null)
        {
            throw new InvalidOperationException($"Vote with ID {voteId} not found.");
        }

        int winnerId = vote.WinnerPhotoId;
        int loserId = vote.LoserPhotoId;

        context.PhotoVotes.Remove(vote);

        // Audit
        try
        {
            context.AuditLogs.Add(new AuditLog
            {
                Action = "DeleteVote",
                EntityId = voteId,
                Details = $"Deleted vote {voteId} (winner:{winnerId}, loser:{loserId})",
                ActorEmail = actorEmail,
                Timestamp = DateTime.UtcNow
            });
        }
        catch
        {
            // ignore
        }

        await context.SaveChangesAsync();

        List<int> affected = [winnerId, loserId];
        await this.RecalculateEloRatingsForPhotosAsync(affected);
    }
}
