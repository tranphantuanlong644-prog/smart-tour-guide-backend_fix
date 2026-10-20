using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTourGuide.Data.Entities;

namespace SmartTourGuide.Data.Configurations;

public sealed class LocationDetectionLogConfiguration
    : IEntityTypeConfiguration<LocationDetectionLog>
{
    public void Configure(
        EntityTypeBuilder<LocationDetectionLog> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "LocationDetectionLogs",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_LocationDetectionLogs_Latitude",
                    "[Latitude] >= -90 " +
                    "AND [Latitude] <= 90");

                tableBuilder.HasCheckConstraint(
                    "CK_LocationDetectionLogs_Longitude",
                    "[Longitude] >= -180 " +
                    "AND [Longitude] <= 180");

                tableBuilder.HasCheckConstraint(
                    "CK_LocationDetectionLogs_" +
                    "AccuracyMeters",
                    "[AccuracyMeters] >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_LocationDetectionLogs_" +
                    "DistanceMeters",
                    "[DistanceMeters] IS NULL OR " +
                    "[DistanceMeters] >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_LocationDetectionLogs_" +
                    "DeviceRecordedAt",
                    "[DeviceRecordedAt] <= " +
                    "DATEADD(MINUTE, 5, [ReceivedAt])");

                tableBuilder.HasCheckConstraint(
                    "CK_LocationDetectionLogs_" +
                    "DetectedLocation",
                    "([LocationDetected] = 0 " +
                    "AND [DetectedTourLocationId] " +
                    "IS NULL) " +
                    "OR " +
                    "([LocationDetected] = 1 " +
                    "AND [DetectedTourLocationId] " +
                    "IS NOT NULL)");

                tableBuilder.HasCheckConstraint(
                    "CK_LocationDetectionLogs_" +
                    "TriggerRadius",
                    "[WithinTriggerRadius] = 0 " +
                    "OR [LocationDetected] = 1");

                tableBuilder.HasCheckConstraint(
                    "CK_LocationDetectionLogs_" +
                    "AudioTrigger",
                    "[AudioTriggerRequested] = 0 " +
                    "OR (" +
                    "[LocationDetected] = 1 " +
                    "AND [WithinTriggerRadius] = 1" +
                    ")");
            });

        builder.HasKey(log => log.Id);

        builder.Property(log => log.Id)
            .ValueGeneratedNever();

        builder.Property(log => log.TourSessionId)
            .IsRequired();

        builder.Property(
            log => log.DetectedTourLocationId);

        builder.Property(log => log.Latitude)
            .IsRequired()
            .HasPrecision(9, 6);

        builder.Property(log => log.Longitude)
            .IsRequired()
            .HasPrecision(9, 6);

        builder.Property(log => log.AccuracyMeters)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(log => log.DeviceRecordedAt)
            .IsRequired();

        builder.Property(log => log.ReceivedAt)
            .IsRequired();

        builder.Property(log => log.LocationDetected)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(log => log.DistanceMeters)
            .HasPrecision(10, 2);

        builder.Property(log => log.WithinTriggerRadius)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(log => log.AudioTriggerRequested)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(log => log.RejectionReason)
            .HasMaxLength(100);

        builder.HasIndex(
                log =>
                    new
                    {
                        log.TourSessionId,
                        log.ReceivedAt
                    })
            .HasDatabaseName(
                "IX_LocationDetectionLogs_" +
                "Session_ReceivedAt");

        builder.HasIndex(
                log =>
                    new
                    {
                        log.DetectedTourLocationId,
                        log.ReceivedAt
                    })
            .HasDatabaseName(
                "IX_LocationDetectionLogs_" +
                "Location_ReceivedAt");

        builder.HasIndex(
                log =>
                    new
                    {
                        log.TourSessionId,
                        log.LocationDetected,
                        log.ReceivedAt
                    })
            .HasDatabaseName(
                "IX_LocationDetectionLogs_" +
                "Session_Detected_ReceivedAt");

        builder.HasOne(log => log.TourSession)
            .WithMany(
                session =>
                    session.LocationDetectionLogs)
            .HasForeignKey(
                log => log.TourSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(
                log =>
                    log.DetectedTourLocation)
            .WithMany(
                location =>
                    location.LocationDetectionLogs)
            .HasForeignKey(
                log =>
                    log.DetectedTourLocationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}