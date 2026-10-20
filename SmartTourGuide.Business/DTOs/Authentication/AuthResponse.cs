namespace SmartTourGuide.Business.DTOs.Authentication;

public sealed class AuthResponse
{
    public string AccessToken { get; set; } =
        string.Empty;

    public string TokenType { get; set; } =
        "Bearer";

    public DateTimeOffset ExpiresAt { get; set; }

    public AuthenticatedUserResponse User { get; set; } =
        new();
}