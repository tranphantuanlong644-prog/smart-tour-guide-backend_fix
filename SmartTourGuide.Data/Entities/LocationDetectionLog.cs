namespace SmartTourGuide.Data.Entities;

public sealed class LocationDetectionLog
{
    public Guid Id { get; set; }

    public Guid TourSessionId { get; set; }

    public Guid? DetectedTourLocationId { get; set; }

    public double Latitude { get; set; }

    public double Longitude { get; set; }

    public double AccuracyMeters { get; set; }

    public DateTimeOffset DeviceRecordedAt { get; set; }

    public DateTimeOffset ReceivedAt { get; set; }

    public bool LocationDetected { get; set; }

    public double? DistanceMeters { get; set; }

    public bool WithinTriggerRadius { get; set; }

    public bool AudioTriggerRequested { get; set; }

    public string? RejectionReason { get; set; }

    public TourSession TourSession { get; set; } =
        null!;

    public TourLocation? DetectedTourLocation
        { get; set; }
}