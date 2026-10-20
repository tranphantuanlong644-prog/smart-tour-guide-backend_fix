using SmartTourGuide.Shared.Enums;

namespace SmartTourGuide.Data.Entities;

public sealed class TourVisit
{
    public Guid Id { get; set; }

    public Guid TourSessionId { get; set; }

    public Guid TourLocationId { get; set; }

    public DetectionMethod DetectionMethod { get; set; }

    public string LanguageCode { get; set; } =
        "vi";

    public DateTimeOffset VisitedAt { get; set; }

    public double? DistanceMeters { get; set; }

    public double? AccuracyMeters { get; set; }

    public double? DetectedLatitude { get; set; }

    public double? DetectedLongitude { get; set; }

    public bool AudioTriggered { get; set; }

    public DateTimeOffset? AudioTriggeredAt { get; set; }

    public TourSession TourSession { get; set; } =
        null!;

    public TourLocation TourLocation { get; set; } =
        null!;
}