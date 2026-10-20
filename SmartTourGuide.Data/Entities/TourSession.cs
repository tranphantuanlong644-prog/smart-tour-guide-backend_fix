using SmartTourGuide.Shared.Enums;

namespace SmartTourGuide.Data.Entities;

public sealed class TourSession
{
    public Guid Id { get; set; }

    public Guid TourId { get; set; }

    public IdentityType IdentityType { get; set; }

    public Guid? UserId { get; set; }

    public Guid? GuestIdentityId { get; set; }

    public Guid? SourceQrCodeId { get; set; }

    public string LanguageCode { get; set; } =
        "vi";

    public TourSessionStatus Status { get; set; } =
        TourSessionStatus.Active;

    public DateTimeOffset StartedAt { get; set; }

    public DateTimeOffset ExpiresAt { get; set; }

    public DateTimeOffset? EndedAt { get; set; }

    public DateTimeOffset? LastActivityAt { get; set; }

    public Tour Tour { get; set; } =
        null!;

    public User? User { get; set; }

    public GuestIdentity? GuestIdentity { get; set; }

    public TourQrCode? SourceQrCode { get; set; }

    public ICollection<TourVisit> Visits
        { get; set; } =
        new List<TourVisit>();

    public ICollection<AudioPlaybackProgress>
        PlaybackProgresses { get; set; } =
        new List<AudioPlaybackProgress>();

    public ICollection<LocationDetectionLog>
        LocationDetectionLogs { get; set; } =
        new List<LocationDetectionLog>();
}