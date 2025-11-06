using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Design;

namespace PathfinderPhotography.Data;

public class ApplicationDbContextFactory : IDesignTimeDbContextFactory<ApplicationDbContext>
{
    public ApplicationDbContext CreateDbContext(string[] args)
    {
        DbContextOptionsBuilder<ApplicationDbContext> optionsBuilder = new DbContextOptionsBuilder<ApplicationDbContext>();
        
        // Use a dummy connection string for migrations
        optionsBuilder.UseNpgsql("Host=localhost;Database=pathfinder_photography;Username=postgres;Password=postgres");
        
        return new ApplicationDbContext(optionsBuilder.Options);
    }
}
