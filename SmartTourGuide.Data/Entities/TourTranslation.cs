namespace SmartTourGuide.Data.Entities;

public sealed class TourTranslation
{
    public Guid Id { get; set; }

    public Guid TourId { get; set; }

    public string LanguageCode { get; set; } =
        string.Empty;

    public string Name { get; set; } =
        string.Empty;

    public string Description { get; set; } =
        string.Empty;

    public string? WelcomeMessage { get; set; }

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public Tour Tour { get; set; } =
        null!;
}