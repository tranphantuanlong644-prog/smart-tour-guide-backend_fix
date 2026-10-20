using Microsoft.AspNetCore.Identity;
using SmartTourGuide.Business.DTOs.Authentication;
using SmartTourGuide.Business.Exceptions;
using SmartTourGuide.Business.Interfaces;
using SmartTourGuide.Business.Services;
using SmartTourGuide.Data.Entities;
using SmartTourGuide.Data.Repositories.Interfaces;
using SmartTourGuide.Shared.Enums;

namespace SmartTourGuide.Tests;

public sealed class AuthServiceTests
{
    private const string ValidPassword = "StrongPass123!";

    [Fact]
    public async Task RegisterAsync_WithValidRequest_CreatesUserAndReturnsBearerToken()
    {
        var repository = new FakeUserRepository();
        var passwordHasher = new FakePasswordHasher();
        var tokenService = new FakeTokenService();
        var service = new AuthService(repository, passwordHasher, tokenService);

        var result = await service.RegisterAsync(new RegisterRequest
        {
            Email = "  Long@example.com  ",
            Password = ValidPassword,
            DisplayName = "  Test User  ",
            PreferredLanguageCode = "EN"
        });

        var savedUser = Assert.Single(repository.Users);
        Assert.Equal("Long@example.com", savedUser.Email);
        Assert.Equal("LONG@EXAMPLE.COM", savedUser.NormalizedEmail);
        Assert.Equal("Test User", savedUser.DisplayName);
        Assert.Equal(UserRole.Visitor, savedUser.Role);
        Assert.Equal("en", savedUser.PreferredLanguageCode);
        Assert.NotEqual(ValidPassword, savedUser.PasswordHash);
        Assert.Equal("test-access-token", result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.Equal(savedUser.Id, result.User.Id);
        Assert.Equal(1, repository.SaveChangesCount);
        Assert.Equal(1, tokenService.CreateTokenCallCount);
    }

    [Fact]
    public async Task RegisterAsync_WhenEmailAlreadyExists_ThrowsEmailAlreadyExistsException()
    {
        var repository = new FakeUserRepository();
        repository.AddExisting(CreateUser("existing@example.com", "EXISTING@EXAMPLE.COM"));
        var service = CreateService(repository);

        await Assert.ThrowsAsync<EmailAlreadyExistsException>(() => service.RegisterAsync(new RegisterRequest
        {
            Email = "EXISTING@example.com",
            Password = ValidPassword,
            DisplayName = "Another User",
            PreferredLanguageCode = "vi"
        }));

        Assert.Single(repository.Users);
        Assert.Equal(0, repository.SaveChangesCount);
    }

    [Fact]
    public async Task RegisterAsync_WithInvalidEmail_ThrowsInvalidRequestException()
    {
        var service = CreateService(new FakeUserRepository());

        await Assert.ThrowsAsync<InvalidRequestException>(() => service.RegisterAsync(new RegisterRequest
        {
            Email = "not a valid email",
            Password = ValidPassword,
            DisplayName = "Test User",
            PreferredLanguageCode = "vi"
        }));
    }

    [Fact]
    public async Task RegisterAsync_WithWeakPassword_ThrowsInvalidRequestException()
    {
        var service = CreateService(new FakeUserRepository());

        await Assert.ThrowsAsync<InvalidRequestException>(() => service.RegisterAsync(new RegisterRequest
        {
            Email = "test@example.com",
            Password = "weak",
            DisplayName = "Test User",
            PreferredLanguageCode = "vi"
        }));
    }

    [Fact]
    public async Task RegisterAsync_WithUnsupportedLanguage_ThrowsInvalidRequestException()
    {
        var service = CreateService(new FakeUserRepository());

        await Assert.ThrowsAsync<InvalidRequestException>(() => service.RegisterAsync(new RegisterRequest
        {
            Email = "test@example.com",
            Password = ValidPassword,
            DisplayName = "Test User",
            PreferredLanguageCode = "fr"
        }));
    }

    [Fact]
    public async Task LoginAsync_WithWrongPassword_ThrowsInvalidCredentialsException()
    {
        var repository = new FakeUserRepository();
        var hasher = new FakePasswordHasher();
        var user = CreateUser("test@example.com", "TEST@EXAMPLE.COM");
        user.PasswordHash = hasher.HashPassword(user, ValidPassword);
        repository.AddExisting(user);
        var service = new AuthService(repository, hasher, new FakeTokenService());

        await Assert.ThrowsAsync<InvalidCredentialsException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "test@example.com",
            Password = "WrongPassword123!"
        }));

        Assert.Equal(0, repository.SaveChangesCount);
    }

    [Fact]
    public async Task LoginAsync_WhenAccountIsInactive_ThrowsAccountInactiveException()
    {
        var repository = new FakeUserRepository();
        var hasher = new FakePasswordHasher();
        var user = CreateUser("inactive@example.com", "INACTIVE@EXAMPLE.COM");
        user.PasswordHash = hasher.HashPassword(user, ValidPassword);
        user.IsActive = false;
        repository.AddExisting(user);
        var service = new AuthService(repository, hasher, new FakeTokenService());

        await Assert.ThrowsAsync<AccountInactiveException>(() => service.LoginAsync(new LoginRequest
        {
            Email = "inactive@example.com",
            Password = ValidPassword
        }));

        Assert.Equal(0, repository.SaveChangesCount);
    }

    [Fact]
    public async Task LoginAsync_WithValidCredentials_UpdatesLastLoginAndReturnsToken()
    {
        var repository = new FakeUserRepository();
        var hasher = new FakePasswordHasher();
        var user = CreateUser("test@example.com", "TEST@EXAMPLE.COM");
        user.PasswordHash = hasher.HashPassword(user, ValidPassword);
        repository.AddExisting(user);
        var tokenService = new FakeTokenService();
        var service = new AuthService(repository, hasher, tokenService);

        var result = await service.LoginAsync(new LoginRequest
        {
            Email = " TEST@example.com ",
            Password = ValidPassword
        });

        Assert.Equal("test-access-token", result.AccessToken);
        Assert.Equal("Bearer", result.TokenType);
        Assert.NotNull(user.LastLoginAt);
        Assert.Equal(1, repository.SaveChangesCount);
        Assert.Equal(1, tokenService.CreateTokenCallCount);
    }

    private static AuthService CreateService(FakeUserRepository repository)
    {
        return new AuthService(repository, new FakePasswordHasher(), new FakeTokenService());
    }

    private static User CreateUser(string email, string normalizedEmail)
    {
        return new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            DisplayName = "Test User",
            Role = UserRole.Visitor,
            IsActive = true,
            PreferredLanguageCode = "vi",
            CreatedAt = DateTimeOffset.UtcNow
        };
    }

    private sealed class FakeUserRepository : IUserRepository
    {
        private readonly List<User> _users = [];

        public IReadOnlyList<User> Users => _users;
        public int SaveChangesCount { get; private set; }

        public void AddExisting(User user) => _users.Add(user);

        public Task<bool> ExistsByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.Any(user => user.NormalizedEmail == normalizedEmail));
        }

        public Task<User?> GetByNormalizedEmailAsync(string normalizedEmail, CancellationToken cancellationToken = default)
        {
            return Task.FromResult(_users.FirstOrDefault(user => user.NormalizedEmail == normalizedEmail));
        }

        public Task AddAsync(User user, CancellationToken cancellationToken = default)
        {
            _users.Add(user);
            return Task.CompletedTask;
        }

        public Task SaveChangesAsync(CancellationToken cancellationToken = default)
        {
            SaveChangesCount++;
            return Task.CompletedTask;
        }
    }

    private sealed class FakePasswordHasher : IPasswordHasher<User>
    {
        public string HashPassword(User user, string password) => $"TEST_HASH::{password}";

        public PasswordVerificationResult VerifyHashedPassword(User user, string hashedPassword, string providedPassword)
        {
            return string.Equals(hashedPassword, HashPassword(user, providedPassword), StringComparison.Ordinal)
                ? PasswordVerificationResult.Success
                : PasswordVerificationResult.Failed;
        }
    }

    private sealed class FakeTokenService : ITokenService
    {
        public int CreateTokenCallCount { get; private set; }

        public TokenResult CreateRegisteredUserToken(User user)
        {
            CreateTokenCallCount++;
            return new TokenResult
            {
                AccessToken = "test-access-token",
                ExpiresAt = DateTimeOffset.UtcNow.AddHours(1)
            };
        }
    }
}
