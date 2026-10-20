using Microsoft.EntityFrameworkCore;
using SmartTourGuide.Data.Context;
using SmartTourGuide.Data.Entities;
using SmartTourGuide.Data.Repositories.Interfaces;

namespace SmartTourGuide.Data.Repositories;

public sealed class UserRepository
    : IUserRepository
{
    private readonly SmartTourGuideDbContext _dbContext;

    public UserRepository(
        SmartTourGuideDbContext dbContext)
    {
        ArgumentNullException.ThrowIfNull(
            dbContext);

        _dbContext = dbContext;
    }

    public Task<bool> ExistsByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            normalizedEmail);

        return _dbContext.Users.AnyAsync(
            user =>
                user.NormalizedEmail ==
                    normalizedEmail,
            cancellationToken);
    }

    public Task<User?> GetByNormalizedEmailAsync(
        string normalizedEmail,
        CancellationToken cancellationToken = default)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            normalizedEmail);

        return _dbContext.Users.FirstOrDefaultAsync(
            user =>
                user.NormalizedEmail ==
                    normalizedEmail,
            cancellationToken);
    }

    public async Task AddAsync(
        User user,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(user);

        await _dbContext.Users.AddAsync(
            user,
            cancellationToken);
    }

    public async Task SaveChangesAsync(
        CancellationToken cancellationToken = default)
    {
        await _dbContext.SaveChangesAsync(
            cancellationToken);
    }
}