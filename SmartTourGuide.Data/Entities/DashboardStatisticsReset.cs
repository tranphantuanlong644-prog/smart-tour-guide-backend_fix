namespace SmartTourGuide.Data.Entities;

/// <summary>
/// Stores a reset baseline without deleting guest history or changing QR usage limits.
/// </summary>
public sealed class DashboardStatisticsReset
{
    public int Id { get; set; } = 1;

    public DateTimeOffset ResetAt { get; set; }

    public int ScanCountAtReset { get; set; }
}
