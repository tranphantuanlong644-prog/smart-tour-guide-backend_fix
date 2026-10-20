namespace SmartTourGuide.Data.Entities;

public sealed class TourLocationTranslation
{
    public Guid Id { get; set; }

    public Guid TourLocationId { get; set; }

    public string LanguageCode { get; set; } =
        string.Empty;

    public string Title { get; set; } =
        string.Empty;

    public string Description { get; set; } =
        string.Empty;

    public string NarrationText { get; set; } =
        string.Empty;

    public string AudioUrl { get; set; } =
        string.Empty;

    public double DurationSeconds { get; set; }

    public string? NarratorName { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public TourLocation TourLocation { get; set; } =
        null!;

    public ICollection<AudioPlaybackProgress>
        PlaybackProgresses { get; set; } =
        new List<AudioPlaybackProgress>();
}