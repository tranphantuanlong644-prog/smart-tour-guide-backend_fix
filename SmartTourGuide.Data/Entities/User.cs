using SmartTourGuide.Shared.Enums;

namespace SmartTourGuide.Data.Entities;

public sealed class User
{
    public Guid Id { get; set; }

    public string Email { get; set; } =
        string.Empty;

    public string NormalizedEmail { get; set; } =
        string.Empty;

    public string PasswordHash { get; set; } =
        string.Empty;

    public string DisplayName { get; set; } =
        string.Empty;

    public UserRole Role { get; set; } =
        UserRole.Visitor;

    public bool IsActive { get; set; } =
        true;

    public string PreferredLanguageCode { get; set; } =
        "vi";

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public DateTimeOffset? LastLoginAt { get; set; }

public ICollection<TourSession> TourSessions
    { get; set; } =
    new List<TourSession>();
}