using SmartTourGuide.Shared.Enums;

namespace SmartTourGuide.Data.Entities;

public sealed class TourQrCode
{
    public Guid Id { get; set; }

    public Guid TourId { get; set; }

    public string TourCode { get; set; } = string.Empty;

    public string TourName { get; set; } = string.Empty;

    public string? TourDescription { get; set; }

    public string? WelcomeMessage { get; set; }

    public string? CoverImageUrl { get; set; }

    public string? Address { get; set; }

    public string AccessCode { get; set; } =
        string.Empty;

    public QrCodeType Type { get; set; } =
        QrCodeType.Reusable;

    public bool IsActive { get; set; } =
        true;

    public int UsageCount { get; set; }

    public int? MaximumUsageCount { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? UsedAt { get; set; }

    public DateTimeOffset? LastUsedAt { get; set; }

    public DateTimeOffset? DeactivatedAt { get; set; }

    public ICollection<TourSession> Sessions
        { get; set; } =
        new List<TourSession>();
}
