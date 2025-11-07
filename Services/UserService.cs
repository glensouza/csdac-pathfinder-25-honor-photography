using PathfinderPhotography.Models;
using PathfinderPhotography.Data;
using Microsoft.EntityFrameworkCore;

namespace PathfinderPhotography.Services;

public class UserService(IDbContextFactory<ApplicationDbContext> contextFactory)
{
    private readonly ApplicationDbContext context = contextFactory.CreateDbContext();

    public async Task<User?> GetUserByEmailAsync(string email)
    {
        return await this.context.Users.FirstOrDefaultAsync(u => u.Email.Equals(email, StringComparison.CurrentCultureIgnoreCase));
    }

    public async Task<User> GetOrCreateUserAsync(string email, string name)
    {
        User? user = await this.context.Users.FirstOrDefaultAsync(u => u.Email.Equals(email, StringComparison.CurrentCultureIgnoreCase));
        if (user != null)
        {
            return user;
        }

        // Check if this is the first user in the system
        int userCount = await this.context.Users.CountAsync();
        bool isFirstUser = userCount == 0;
        user = new User
        {
            Email = email,
            Name = name,
            Role = isFirstUser ? UserRole.Admin : UserRole.Pathfinder,
            CreatedDate = DateTime.UtcNow
        };

        this.context.Users.Add(user);
        await this.context.SaveChangesAsync();

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

    public async Task<bool> IsInstructorOrAdminAsync(string email)
    {
        User? user = await this.GetUserByEmailAsync(email);
        return user?.Role is UserRole.Instructor or UserRole.Admin;
    }

    public async Task SetUserRoleAsync(string email, UserRole role)
    {
        // Prevent setting admin role through API - admin can only be set via direct database update
        if (role == UserRole.Admin)
        {
            throw new InvalidOperationException("Admin role can only be set through direct database updates for security purposes.");
        }

        await using ApplicationDbContext applicationDbContext = await contextFactory.CreateDbContextAsync();
        
        User? user = await applicationDbContext.Users.FirstOrDefaultAsync(u => u.Email.ToLower() == email.ToLower());
        
        if (user != null)
        {
            // Prevent modifying admin users through API
            if (user.Role == UserRole.Admin)
            {
                throw new InvalidOperationException("Cannot modify admin user roles through API. Use direct database updates.");
            }

            user.Role = role;
            await applicationDbContext.SaveChangesAsync();
        }
    }

    public async Task<List<User>> GetAllUsersAsync()
    {
        return await this.context.Users.OrderBy(u => u.Name).ToListAsync();
    }

    public async Task<List<User>> GetInstructorsAndAdminsAsync()
    {
        return await this.context.Users
            .Where(u => u.Role == UserRole.Instructor || u.Role == UserRole.Admin)
            .OrderBy(u => u.Name)
            .ToListAsync();
    }
}
