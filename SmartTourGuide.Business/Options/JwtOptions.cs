namespace SmartTourGuide.Business.Options;

public sealed class JwtOptions
{
    public const string SectionName =
        "Jwt";

    public string Issuer { get; set; } =
        string.Empty;

    public string Audience { get; set; } =
        string.Empty;

    public string Key { get; set; } =
        string.Empty;

    public int RegisteredUserExpirationMinutes
        { get; set; } = 120;

    public int GuestExpirationMinutes
        { get; set; } = 240;
}