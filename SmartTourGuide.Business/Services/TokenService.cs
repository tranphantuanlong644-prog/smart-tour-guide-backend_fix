using System.IdentityModel.Tokens.Jwt;
using System.Security.Claims;
using System.Text;
using Microsoft.Extensions.Options;
using Microsoft.IdentityModel.Tokens;
using SmartTourGuide.Business.DTOs.Authentication;
using SmartTourGuide.Business.Interfaces;
using SmartTourGuide.Business.Options;
using SmartTourGuide.Data.Entities;
using SmartTourGuide.Shared.Enums;

namespace SmartTourGuide.Business.Services;

public sealed class TokenService
    : ITokenService
{
    private readonly JwtOptions _jwtOptions;

    public TokenService(
        IOptions<JwtOptions> jwtOptions)
    {
        ArgumentNullException.ThrowIfNull(
            jwtOptions);

        _jwtOptions = jwtOptions.Value;

        ValidateOptions(_jwtOptions);
    }

    public TokenResult CreateRegisteredUserToken(
        User user)
    {
        ArgumentNullException.ThrowIfNull(user);

        if (user.Id == Guid.Empty)
        {
            throw new ArgumentException(
                "User ID must not be empty.",
                nameof(user));
        }

        if (string.IsNullOrWhiteSpace(user.Email))
        {
            throw new ArgumentException(
                "User email must not be empty.",
                nameof(user));
        }

        var issuedAt =
            DateTimeOffset.UtcNow;

        var expiresAt =
            issuedAt.AddMinutes(
                _jwtOptions
                    .RegisteredUserExpirationMinutes);

        var claims =
            new List<Claim>
            {
                new(
                    JwtRegisteredClaimNames.Sub,
                    user.Id.ToString()),

                new(
                    JwtRegisteredClaimNames.Jti,
                    Guid.NewGuid().ToString()),

                new(
                    JwtRegisteredClaimNames.Iat,
                    issuedAt.ToUnixTimeSeconds()
                        .ToString(),
                    ClaimValueTypes.Integer64),

                new(
                    ClaimTypes.NameIdentifier,
                    user.Id.ToString()),

                new(
                    ClaimTypes.Email,
                    user.Email),

                new(
                    ClaimTypes.Name,
                    user.DisplayName),

                new(
                    ClaimTypes.Role,
                    user.Role.ToString()),

                new(
                    "identity_type",
                    IdentityType
                        .RegisteredUser
                        .ToString()),

                new(
                    "preferred_language",
                    user.PreferredLanguageCode)
            };

        var signingKey =
            new SymmetricSecurityKey(
                Encoding.UTF8.GetBytes(
                    _jwtOptions.Key));

        var signingCredentials =
            new SigningCredentials(
                signingKey,
                SecurityAlgorithms
                    .HmacSha256);

        var token =
            new JwtSecurityToken(
                issuer: _jwtOptions.Issuer,
                audience: _jwtOptions.Audience,
                claims: claims,
                notBefore: issuedAt.UtcDateTime,
                expires: expiresAt.UtcDateTime,
                signingCredentials:
                    signingCredentials);

        var accessToken =
            new JwtSecurityTokenHandler()
                .WriteToken(token);

        return new TokenResult
        {
            AccessToken = accessToken,
            ExpiresAt = expiresAt
        };
    }

    private static void ValidateOptions(
        JwtOptions options)
    {
        if (string.IsNullOrWhiteSpace(
                options.Issuer))
        {
            throw new InvalidOperationException(
                "JWT issuer was not configured.");
        }

        if (string.IsNullOrWhiteSpace(
                options.Audience))
        {
            throw new InvalidOperationException(
                "JWT audience was not configured.");
        }

        if (string.IsNullOrWhiteSpace(
                options.Key))
        {
            throw new InvalidOperationException(
                "JWT signing key was not configured.");
        }

        if (options.Key.Length < 32)
        {
            throw new InvalidOperationException(
                "JWT signing key must contain at " +
                "least 32 characters.");
        }

        if (
            options.RegisteredUserExpirationMinutes
            <= 0)
        {
            throw new InvalidOperationException(
                "Registered user token expiration " +
                "must be greater than zero.");
        }
    }
}