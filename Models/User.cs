namespace PathfinderPhotography.Models;

public class User
{
    public int Id { get; set; }
    public string Email { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public UserRole Role { get; set; } = UserRole.Pathfinder;
    public DateTime CreatedDate { get; set; } = DateTime.UtcNow;
}

public enum UserRole
{
    Pathfinder = 0,
    Instructor = 1,
    Admin = 2
}
