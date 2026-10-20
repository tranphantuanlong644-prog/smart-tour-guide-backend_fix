using SmartTourGuide.Shared.Constants;

namespace SmartTourGuide.Data.Entities;

public sealed class TourLocation
{
    public Guid Id { get; set; }

    public Guid TourId { get; set; }

    public string Code { get; set; } =
        string.Empty;

    public string DefaultName { get; set; } =
        string.Empty;

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double TriggerRadiusMeters { get; set; } =
        GpsDefaults.TriggerRadiusMeters;

    public int DisplayOrder { get; set; }

    public string? ImageUrl { get; set; }

    public bool IsActive { get; set; } =
        true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Tour Tour { get; set; } =
        null!;

    public ICollection<TourLocationTranslation>
        Translations { get; set; } =
        new List<TourLocationTranslation>();

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