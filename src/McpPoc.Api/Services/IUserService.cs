using McpPoc.Api.Models;

namespace McpPoc.Api.Services;

public interface IUserService
{
    Task<User?> GetByIdAsync(int id);
    Task<List<User>> GetAllAsync();
    Task<User> CreateAsync(string name, string email);
}

public class UserService : IUserService
{
    // IMPORTANT: Username must match Keycloak's preferred_username claim
    // For "admin" login -> preferred_username = "admin" (NOT "admin@mcppoc.com")
    // For "alice@example.com" login -> preferred_username = "alice@example.com"
    private readonly List<User> _users = new()
    {
        new User { Id = 1, Name = "Alice Smith", Email = "alice@example.com", Role = UserRole.Member },
        new User { Id = 2, Name = "Bob Jones", Email = "bob@example.com", Role = UserRole.Manager },
        new User { Id = 3, Name = "Carol White", Email = "carol@example.com", Role = UserRole.Admin },
        new User { Id = 100, Name = "Admin User", Email = "admin", Role = UserRole.Admin },
        new User { Id = 101, Name = "Regular User", Email = "user", Role = UserRole.Member },
        new User { Id = 102, Name = "Viewer User", Email = "viewer", Role = UserRole.Viewer }
    };

    public Task<User?> GetByIdAsync(int id)
    {
        var user = _users.FirstOrDefault(u => u.Id == id);
        return Task.FromResult(user);
    }

    public Task<List<User>> GetAllAsync()
    {
        return Task.FromResult(_users.ToList());
    }

    public Task<User> CreateAsync(string name, string email)
    {
        var user = new User
        {
            Id = _users.Max(u => u.Id) + 1,
            Name = name,
            Email = email,
            Role = UserRole.Member  // Default new users to Member role
        };
        _users.Add(user);
        return Task.FromResult(user);
    }
}
