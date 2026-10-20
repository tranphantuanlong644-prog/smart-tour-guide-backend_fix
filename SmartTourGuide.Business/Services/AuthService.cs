using System.Net.Mail;
using Microsoft.AspNetCore.Identity;
using SmartTourGuide.Business.DTOs.Authentication;
using SmartTourGuide.Business.Exceptions;
using SmartTourGuide.Business.Interfaces;
using SmartTourGuide.Data.Entities;
using SmartTourGuide.Data.Repositories.Interfaces;
using SmartTourGuide.Shared.Constants;
using SmartTourGuide.Shared.Enums;

namespace SmartTourGuide.Business.Services;

public sealed class AuthService : IAuthService
{
    private const int MinimumPasswordLength = 8;
    private const int MaximumPasswordLength = 100;
    private const int MaximumDisplayNameLength = 100;

    private readonly IUserRepository _userRepository;
    private readonly IPasswordHasher<User> _passwordHasher;
    private readonly ITokenService _tokenService;

    public AuthService(
        IUserRepository userRepository,
        IPasswordHasher<User> passwordHasher,
        ITokenService tokenService)
    {
        ArgumentNullException.ThrowIfNull(userRepository);
        ArgumentNullException.ThrowIfNull(passwordHasher);
        ArgumentNullException.ThrowIfNull(tokenService);

        _userRepository = userRepository;
        _passwordHasher = passwordHasher;
        _tokenService = tokenService;
    }

    public async Task<AuthResponse> RegisterAsync(
        RegisterRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email =
            NormalizeAndValidateEmail(request.Email);

        var normalizedEmail =
            NormalizeEmailForLookup(email);

        var displayName =
            NormalizeAndValidateDisplayName(
                request.DisplayName);

        ValidatePassword(request.Password);

        var languageCode =
            NormalizeAndValidateLanguageCode(
                request.PreferredLanguageCode);

        var emailExists =
            await _userRepository
                .ExistsByNormalizedEmailAsync(
                    normalizedEmail,
                    cancellationToken);

        if (emailExists)
        {
            throw new EmailAlreadyExistsException();
        }

        var now = DateTimeOffset.UtcNow;

        var user = new User
        {
            Id = Guid.NewGuid(),
            Email = email,
            NormalizedEmail = normalizedEmail,
            DisplayName = displayName,
            Role = UserRole.Visitor,
            IsActive = true,
            PreferredLanguageCode = languageCode,
            CreatedAt = now
        };

        user.PasswordHash =
            _passwordHasher.HashPassword(
                user,
                request.Password);

        await _userRepository.AddAsync(
            user,
            cancellationToken);

        await _userRepository.SaveChangesAsync(
            cancellationToken);

        return CreateAuthResponse(user);
    }

    public async Task<AuthResponse> LoginAsync(
        LoginRequest request,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(request);

        var email =
            NormalizeAndValidateEmail(request.Email);

        if (string.IsNullOrWhiteSpace(request.Password))
        {
            throw new InvalidCredentialsException();
        }

        var normalizedEmail =
            NormalizeEmailForLookup(email);

        var user =
            await _userRepository
                .GetByNormalizedEmailAsync(
                    normalizedEmail,
                    cancellationToken);

        if (user is null)
        {
            throw new InvalidCredentialsException();
        }

        if (!user.IsActive)
        {
            throw new AccountInactiveException();
        }

        var verificationResult =
            _passwordHasher.VerifyHashedPassword(
                user,
                user.PasswordHash,
                request.Password);

        if (
            verificationResult ==
            PasswordVerificationResult.Failed)
        {
            throw new InvalidCredentialsException();
        }

        if (
            verificationResult ==
            PasswordVerificationResult.SuccessRehashNeeded)
        {
            user.PasswordHash =
                _passwordHasher.HashPassword(
                    user,
                    request.Password);
        }

        user.LastLoginAt = DateTimeOffset.UtcNow;

        await _userRepository.SaveChangesAsync(
            cancellationToken);

        return CreateAuthResponse(user);
    }

    private AuthResponse CreateAuthResponse(
        User user)
    {
        var tokenResult =
            _tokenService.CreateRegisteredUserToken(
                user);

        return new AuthResponse
        {
            AccessToken = tokenResult.AccessToken,
            TokenType = "Bearer",
            ExpiresAt = tokenResult.ExpiresAt,
            User = new AuthenticatedUserResponse
            {
                Id = user.Id,
                Email = user.Email,
                DisplayName = user.DisplayName,
                Role = user.Role.ToString(),
                PreferredLanguageCode =
                    user.PreferredLanguageCode
            }
        };
    }

    private static string NormalizeAndValidateEmail(
        string email)
    {
        if (string.IsNullOrWhiteSpace(email))
        {
            throw new InvalidRequestException(
                "Email is required.");
        }

        var trimmedEmail = email.Trim();

        if (trimmedEmail.Length > 320)
        {
            throw new InvalidRequestException(
                "Email must not exceed 320 characters.");
        }

        if (
            !MailAddress.TryCreate(
                trimmedEmail,
                out var parsedAddress) ||
            !string.Equals(
                parsedAddress.Address,
                trimmedEmail,
                StringComparison.OrdinalIgnoreCase))
        {
            throw new InvalidRequestException(
                "Email format is invalid.");
        }

        return trimmedEmail;
    }

    private static string NormalizeEmailForLookup(
        string email)
    {
        return email
            .Trim()
            .ToUpperInvariant();
    }

    private static string NormalizeAndValidateDisplayName(
        string displayName)
    {
        if (string.IsNullOrWhiteSpace(displayName))
        {
            throw new InvalidRequestException(
                "Display name is required.");
        }

        var normalizedDisplayName =
            displayName.Trim();

        if (
            normalizedDisplayName.Length >
            MaximumDisplayNameLength)
        {
            throw new InvalidRequestException(
                "Display name must not exceed " +
                $"{MaximumDisplayNameLength} characters.");
        }

        return normalizedDisplayName;
    }

    private static void ValidatePassword(
        string password)
    {
        if (string.IsNullOrWhiteSpace(password))
        {
            throw new InvalidRequestException(
                "Password is required.");
        }

        if (password.Length < MinimumPasswordLength)
        {
            throw new InvalidRequestException(
                "Password must contain at least " +
                $"{MinimumPasswordLength} characters.");
        }

        if (password.Length > MaximumPasswordLength)
        {
            throw new InvalidRequestException(
                "Password must not exceed " +
                $"{MaximumPasswordLength} characters.");
        }

        if (!password.Any(char.IsUpper))
        {
            throw new InvalidRequestException(
                "Password must contain at least " +
                "one uppercase letter.");
        }

        if (!password.Any(char.IsLower))
        {
            throw new InvalidRequestException(
                "Password must contain at least " +
                "one lowercase letter.");
        }

        if (!password.Any(char.IsDigit))
        {
            throw new InvalidRequestException(
                "Password must contain at least " +
                "one number.");
        }

        if (!password.Any(
                character =>
                    !char.IsLetterOrDigit(character)))
        {
            throw new InvalidRequestException(
                "Password must contain at least " +
                "one special character.");
        }
    }

    private static string
        NormalizeAndValidateLanguageCode(
            string languageCode)
    {
        if (
            !SupportedLanguageCodes.IsSupported(
                languageCode))
        {
            throw new InvalidRequestException(
                "The preferred language is not supported.");
        }

        return SupportedLanguageCodes.Normalize(
            languageCode);
    }
}