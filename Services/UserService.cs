using Microsoft.EntityFrameworkCore;
using PathfinderPhotography.Data;
using PathfinderPhotography.Models;
#pragma warning disable CA1862

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
        if (user == null)
        {
            return;
        }

        if (user.Role == UserRole.Admin)
        {
            throw new InvalidOperationException("Cannot modify admin user roles through API. Use direct database updates.");
        }

        user.Role = role;
        await context.SaveChangesAsync();
    }

    public async Task DeleteUserAsync(string email)
    {
        await using ApplicationDbContext context = await contextFactory.CreateDbContextAsync();
        User? user = await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());

        if (user == null)
        {
            throw new InvalidOperationException($"User with email {email} not found.");
        }

        if (user.Role == UserRole.Admin)
        {
            throw new InvalidOperationException("Cannot delete admin users through API. Use direct database updates.");
        }

        // Get all photo submissions by this user (to be deleted)
        List<PhotoSubmission> submissions = await context.PhotoSubmissions
            .Where(s => s.PathfinderEmail.ToLower() == email.ToLower())
            .ToListAsync();

        List<int> photoIdsBeingDeleted = submissions.Select(s => s.Id).ToList();

        // Votes that will be deleted:
        // 1) votes involving the user's photos
        List<PhotoVote> votesInvolvingUserPhotos = await context.PhotoVotes
            .Where(v => photoIdsBeingDeleted.Contains(v.WinnerPhotoId) || photoIdsBeingDeleted.Contains(v.LoserPhotoId))
            .ToListAsync();

        // 2) votes made by the user
        List<PhotoVote> votesMadeByUser = await context.PhotoVotes
            .Where(v => v.VoterEmail.ToLower() == email.ToLower())
            .ToListAsync();

        // Union the votes to delete (avoid duplicates; EF tracks the same instance within the same context)
        List<PhotoVote> allVotesToDelete = votesInvolvingUserPhotos
            .Concat(votesMadeByUser)
            .Distinct()
            .ToList();

        // Any remaining photo that appears in a vote that is about to be removed must be recalculated.
        HashSet<int> photosNeedingRecalculation = [];
        foreach (PhotoVote vote in allVotesToDelete)
        {
            if (!photoIdsBeingDeleted.Contains(vote.WinnerPhotoId))
            {
                photosNeedingRecalculation.Add(vote.WinnerPhotoId);
            }
            
            if (!photoIdsBeingDeleted.Contains(vote.LoserPhotoId))
            {
                photosNeedingRecalculation.Add(vote.LoserPhotoId);
            }
        }

        // Recalculate ELO ratings BEFORE deleting votes (so we can replay remaining votes),
        // explicitly excluding the votes that are about to be removed.
        if (photosNeedingRecalculation.Count > 0)
        {
            List<int> voteIdsToExclude = allVotesToDelete.Select(v => v.Id).ToList();
            await votingService.RecalculateEloRatingsForPhotosAsync(photosNeedingRecalculation.ToList(), voteIdsToExclude);
        }

        // Delete all votes and submissions owned by the user, then the user
        context.PhotoVotes.RemoveRange(allVotesToDelete);
        context.PhotoSubmissions.RemoveRange(submissions);
        context.Users.Remove(user);

        await context.SaveChangesAsync();
    }
}
