namespace SmartTourGuide.Business.DTOs.Authentication;

public sealed class TokenResult
{
    public string AccessToken { get; set; } =
        string.Empty;

    public DateTimeOffset ExpiresAt { get; set; }
}