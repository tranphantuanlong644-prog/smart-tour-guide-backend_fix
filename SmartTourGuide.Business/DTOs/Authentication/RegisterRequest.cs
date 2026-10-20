namespace SmartTourGuide.Business.DTOs.Authentication;

public sealed class RegisterRequest
{
    public string Email { get; set; } =
        string.Empty;

    public string Password { get; set; } =
        string.Empty;

    public string DisplayName { get; set; } =
        string.Empty;

    public string PreferredLanguageCode { get; set; } =
        "vi";
}