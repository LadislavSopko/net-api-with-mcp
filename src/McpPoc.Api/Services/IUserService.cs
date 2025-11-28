using McpPoc.Api.Models;

namespace McpPoc.Api.Services;

public interface IUserService
{
    Task<User?> GetByIdAsync(int id);
    Task<List<User>> GetAllAsync();
    Task<User> CreateAsync(string name, string email);
}

/// <summary>
/// Singleton store for user data persistence across requests.
/// HACK: In-memory persistence until EF Core is wired up.
/// </summary>
public class UserStore
{
    private List<User> _users;
    private readonly object _lock = new();

    public UserStore()
    {
        _users = CreateSeedData();
    }

    private static List<User> CreateSeedData() => new()
    {
        new User { Id = 1, Name = "Alice Smith", Email = "alice@example.com", Role = UserRole.Member },
        new User { Id = 2, Name = "Bob Jones", Email = "bob@example.com", Role = UserRole.Manager },
        new User { Id = 3, Name = "Carol White", Email = "carol@example.com", Role = UserRole.Admin },
        new User { Id = 100, Name = "Admin User", Email = "admin", Role = UserRole.Admin },
        new User { Id = 101, Name = "Regular User", Email = "user", Role = UserRole.Member },
        new User { Id = 102, Name = "Viewer User", Email = "viewer", Role = UserRole.Viewer }
    };

    public User? GetById(int id)
    {
        lock (_lock) return _users.FirstOrDefault(u => u.Id == id);
    }

    public List<User> GetAll()
    {
        lock (_lock) return _users.ToList();
    }

    public User Add(User user)
    {
        lock (_lock)
        {
            user.Id = _users.Max(u => u.Id) + 1;
            _users.Add(user);
            return user;
        }
    }

    public User? Update(int id, string name, string email)
    {
        lock (_lock)
        {
            var user = _users.FirstOrDefault(u => u.Id == id);
            if (user != null)
            {
                user.Name = name;
                user.Email = email;
            }
            return user;
        }
    }

    /// <summary>
    /// Reset to seed data. Used for test isolation.
    /// </summary>
    public void Reset()
    {
        lock (_lock)
        {
            _users = CreateSeedData();
        }
    }
}

public class UserService : IUserService
{
    private readonly UserStore _store;

    public UserService(UserStore store)
    {
        _store = store;
    }

    public Task<User?> GetByIdAsync(int id)
    {
        return Task.FromResult(_store.GetById(id));
    }

    public Task<List<User>> GetAllAsync()
    {
        return Task.FromResult(_store.GetAll());
    }

    public Task<User> CreateAsync(string name, string email)
    {
        var user = new User
        {
            Name = name,
            Email = email,
            Role = UserRole.Member
        };
        return Task.FromResult(_store.Add(user));
    }
}
