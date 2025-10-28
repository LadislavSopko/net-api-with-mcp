namespace McpPoc.Api.Models;

public enum UserRole
{
    Member = 1,
    Manager = 2,
    Admin = 3
}

public class User
{
    public int Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string Email { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public UserRole Role { get; set; } = UserRole.Member;
}
