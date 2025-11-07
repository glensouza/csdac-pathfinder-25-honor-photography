using Microsoft.EntityFrameworkCore;
using PathfinderPhotography.Data;
using PathfinderPhotography.Models;

namespace PathfinderPhotography.Services;

public class UserService(IDbContextFactory<ApplicationDbContext> contextFactory)
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
}
