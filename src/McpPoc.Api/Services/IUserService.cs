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
    private readonly List<User> _users = new()
    {
        new User { Id = 1, Name = "Alice Smith", Email = "alice@example.com" },
        new User { Id = 2, Name = "Bob Jones", Email = "bob@example.com" },
        new User { Id = 3, Name = "Carol White", Email = "carol@example.com" }
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
            Email = email
        };
        _users.Add(user);
        return Task.FromResult(user);
    }
}
