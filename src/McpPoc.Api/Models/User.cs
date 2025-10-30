namespace McpPoc.Api.Models;

public enum UserRole
{
    Viewer = 0,   // Read-only access
    Member = 1,   // Read + Create
    Manager = 2,  // Read + Create + Update
    Admin = 3     // Everything
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public UserRole Role { get; set; } = UserRole.Member;
}
