namespace SmartTourGuide.Shared.Constants;

public static class GpsDefaults
{
    public const double TriggerRadiusMeters =
        20.0;

    public const double MaximumAccuracyMeters =
        30.0;

    public const int LocationPollingIntervalSeconds =
        5;

    public const int SameLocationCooldownMinutes =
        5;

    public const double MinimumLatitude =
        -90.0;

    public const double MaximumLatitude =
        90.0;

    public const double MinimumLongitude =
        -180.0;

    public const double MaximumLongitude =
        180.0;
}