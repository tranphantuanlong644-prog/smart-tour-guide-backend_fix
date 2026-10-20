namespace SmartTourGuide.Data.Entities;

public sealed class Tour
{
    public Guid Id { get; set; }

    public string Code { get; set; } =
        string.Empty;

    public string DefaultName { get; set; } =
        string.Empty;

    public string? CoverImageUrl { get; set; }

    public string? Address { get; set; }

    public double? CenterLatitude { get; set; }

    public double? CenterLongitude { get; set; }

    public bool IsActive { get; set; } =
        true;

    public DateTimeOffset CreatedAt { get; set; }

    public DateTimeOffset? UpdatedAt { get; set; }

    public ICollection<TourTranslation> Translations
        { get; set; } =
        new List<TourTranslation>();

    public ICollection<TourLocation> Locations
        { get; set; } =
        new List<TourLocation>();

    public ICollection<TourQrCode> QrCodes
        { get; set; } =
        new List<TourQrCode>();

    public ICollection<TourSession> Sessions
        { get; set; } =
        new List<TourSession>();
}