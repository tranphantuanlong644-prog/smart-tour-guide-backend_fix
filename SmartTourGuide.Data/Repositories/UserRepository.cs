using System.Collections.Concurrent;
using SmartTourGuide.Data.Entities;
using SmartTourGuide.Data.Repositories.Interfaces;

namespace SmartTourGuide.Data.Repositories;

/// <summary>
/// Keeps registered accounts in process memory because the application schema
/// intentionally contains no Users table. Accounts are cleared on restart.
/// </summary>
public sealed class UserRepository : IUserRepository
{
    private readonly ConcurrentDictionary<string, User> _users = new(StringComparer.Ordinal);

    public Task<bool> ExistsByNormalizedEmailAsync(
        string normalizedEmail, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);
        return Task.FromResult(_users.ContainsKey(normalizedEmail));
    }

    public Task<User?> GetByNormalizedEmailAsync(
        string normalizedEmail, CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(normalizedEmail);
        _users.TryGetValue(normalizedEmail, out var user);
        return Task.FromResult(user);
    }

    public Task AddAsync(User user, CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);
        if (!_users.TryAdd(user.NormalizedEmail, user))
            throw new InvalidOperationException("An account with this email already exists.");
        return Task.CompletedTask;
    }

    public Task SaveChangesAsync(CancellationToken cancellationToken = default) => Task.CompletedTask;
}
