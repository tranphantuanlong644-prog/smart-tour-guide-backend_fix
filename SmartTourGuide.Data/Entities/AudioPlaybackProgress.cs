namespace SmartTourGuide.Data.Entities;

public sealed class AudioPlaybackProgress
{
    public Guid Id { get; set; }

    public Guid TourSessionId { get; set; }

    public Guid TourLocationId { get; set; }

    public Guid TourLocationTranslationId { get; set; }

    public string LanguageCode { get; set; } =
        "vi";

    public double PositionSeconds { get; set; }

    public double DurationSeconds { get; set; }

    public double CompletionPercentage { get; set; }

    public bool IsCompleted { get; set; }

    public DateTimeOffset? CompletedAt { get; set; }

    public DateTimeOffset LastListenedAt { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public TourSession TourSession { get; set; } =
        null!;

    public TourLocation TourLocation { get; set; } =
        null!;

    public TourLocationTranslation Translation
        { get; set; } =
        null!;
}