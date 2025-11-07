using Microsoft.EntityFrameworkCore;
using PathfinderPhotography.Data;
using PathfinderPhotography.Models;

namespace PathfinderPhotography.Services;

public class UserService(IDbContextFactory<ApplicationDbContext> contextFactory, VotingService votingService)
{
    public async Task<User?> GetUserByEmailAsync(string email)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        return await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<User> GetOrCreateUserAsync(string email, string name)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();

        User? user = await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        if (user != null) return user;

        bool isFirstUser = await context.Users.CountAsync() == 0;
        user = new User
        {
            Email = email,
            Name = name,
            Role = isFirstUser ? UserRole.Admin : UserRole.Pathfinder,
            CreatedDate = DateTime.UtcNow
        };

        context.Users.Add(user);
        await context.SaveChangesAsync();
        return user;
    }

    public async Task<bool> IsInstructorAsync(string email)
    {
        User? user = await this.GetUserByEmailAsync(email);
        return user?.Role == UserRole.Instructor;
    }

    public async Task<bool> IsAdminAsync(string email)
    {
        User? user = await this.GetUserByEmailAsync(email);
        return user?.Role == UserRole.Admin;
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        return await context.Users.OrderBy(u => u.Name).ToListAsync();
    }

    public async Task<List<User>> GetInstructorsAndAdminsAsync()
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        return await context.Users
            .Where(u => u.Role == UserRole.Instructor || u.Role == UserRole.Admin)
            .OrderBy(u => u.Name)
            .ToListAsync();
    }

    public async Task SetUserRoleAsync(string email, UserRole role)
    {
        if (role == UserRole.Admin)
            throw new InvalidOperationException("Admin role can only be set through direct database updates for security purposes.");

        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        User? user = await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        if (user == null) return;

        if (user.Role == UserRole.Admin)
            throw new InvalidOperationException("Cannot modify admin user roles through API. Use direct database updates.");

        user.Role = role;
        await context.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(string email)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        User? user = await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        
        if (user == null)
            throw new InvalidOperationException($"User with email {email} not found.");

        if (user.Role == UserRole.Admin)
            throw new InvalidOperationException("Cannot delete admin users through API. Use direct database updates.");

        // Get all photo submissions by this user
        List<PhotoSubmission> submissions = await context.PhotoSubmissions
            .Where(s => s.PathfinderEmail.ToLower() == email.ToLower())
            .ToListAsync();

        // Get all photo IDs to delete votes
        List<int> photoIds = submissions.Select(s => s.Id).ToList();

        // Find all votes involving these photos (will be deleted)
        List<PhotoVote> votesInvolvingUserPhotos = await context.PhotoVotes
            .Where(v => photoIds.Contains(v.WinnerPhotoId) || photoIds.Contains(v.LoserPhotoId))
            .ToListAsync();

        // Find all votes made BY this user (will be deleted and need ELO recalculation)
        List<PhotoVote> votesMadeByUser = await context.PhotoVotes
            .Where(v => v.VoterEmail.ToLower() == email.ToLower())
            .ToListAsync();

        // Collect all photo IDs that need ELO recalculation (photos involved in votes made by this user)
        // Only recalculate if BOTH photos in the vote still exist (not being deleted)
        HashSet<int> photosNeedingRecalculation = new HashSet<int>();
        foreach (PhotoVote vote in votesMadeByUser)
        {
            if (!photoIds.Contains(vote.WinnerPhotoId) && !photoIds.Contains(vote.LoserPhotoId))
            {
                photosNeedingRecalculation.Add(vote.WinnerPhotoId);
                photosNeedingRecalculation.Add(vote.LoserPhotoId);
            }
        }

        // Recalculate ELO ratings BEFORE deleting votes (so votes still exist for replay)
        if (photosNeedingRecalculation.Count > 0)
        {
            await votingService.RecalculateEloRatingsForPhotosAsync(photosNeedingRecalculation.ToList(), votesMadeByUser.Select(v => v.Id).ToList());
        }

        // Delete all votes involving user's photos and votes made by the user (avoid duplicates)
        List<PhotoVote> allVotesToDelete = votesInvolvingUserPhotos
            .Concat(votesMadeByUser)
            .Distinct()
            .ToList();
        context.PhotoVotes.RemoveRange(allVotesToDelete);

        // Delete all photo submissions
        context.PhotoSubmissions.RemoveRange(submissions);

        // Delete the user
        context.Users.Remove(user);

        await context.SaveChangesAsync();
    }
}
