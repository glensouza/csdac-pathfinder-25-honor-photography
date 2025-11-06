using PathfinderPhotography.Models;
using PathfinderPhotography.Data;
using Microsoft.EntityFrameworkCore;

namespace PathfinderPhotography.Services;

public class UserService
{
    private readonly IDbContextFactory<ApplicationDbContext> _contextFactory;

    public UserService(IDbContextFactory<ApplicationDbContext> contextFactory)
    {
        _contextFactory = contextFactory;
    }

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
    }

    public async Task<User> GetOrCreateUserAsync(string email, string name)
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        
        if (user == null)
        {
            // Check if this is the first user in the system
            var userCount = await context.Users.CountAsync();
            var isFirstUser = userCount == 0;
            
            user = new User
            {
                Email = email,
                Name = name,
                Role = isFirstUser ? UserRole.Admin : UserRole.Pathfinder,
                CreatedDate = DateTime.UtcNow
            };
            
            context.Users.Add(user);
            await context.SaveChangesAsync();
        }
        
        return user;
    }

    public async Task<bool> IsInstructorAsync(string email)
    {
        var user = await GetUserByEmailAsync(email);
        return user?.Role == UserRole.Instructor;
    }

    public async Task<bool> IsAdminAsync(string email)
    {
        var user = await GetUserByEmailAsync(email);
        return user?.Role == UserRole.Admin;
    }

    public async Task<bool> IsInstructorOrAdminAsync(string email)
    {
        var user = await GetUserByEmailAsync(email);
        return user?.Role == UserRole.Instructor || user?.Role == UserRole.Admin;
    }

    public async Task SetUserRoleAsync(string email, UserRole role)
    {
        // Prevent setting admin role through API - admin can only be set via direct database update
        if (role == UserRole.Admin)
        {
            throw new InvalidOperationException("Admin role can only be set through direct database updates for security purposes.");
        }

        using var context = await _contextFactory.CreateDbContextAsync();
        
        var user = await context.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        
        if (user != null)
        {
            // Prevent modifying admin users through API
            if (user.Role == UserRole.Admin)
            {
                throw new InvalidOperationException("Cannot modify admin user roles through API. Use direct database updates.");
            }

            user.Role = role;
            await context.SaveChangesAsync();
        }
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        using var context = await _contextFactory.CreateDbContextAsync();
        return await context.Users.OrderBy(u => u.Name).ToListAsync();
    }
}
