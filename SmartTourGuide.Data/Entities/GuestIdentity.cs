namespace SmartTourGuide.Data.Entities;

public sealed class GuestIdentity
{
    public Guid Id { get; set; }

    public string DisplayName { get; set; } =
        string.Empty;

    public string PreferredLanguageCode { get; set; } =
        "vi";

    public string DeviceId { get; set; } =
        string.Empty;

    public bool IsActive { get; set; } =
        true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? LastAccessedAt { get; set; }

    public ICollection<TourSession> TourSessions
        { get; set; } =
        new List<TourSession>();
}