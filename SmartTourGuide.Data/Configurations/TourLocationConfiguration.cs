using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTourGuide.Data.Entities;
using SmartTourGuide.Shared.Constants;

namespace SmartTourGuide.Data.Configurations;

public sealed class TourLocationConfiguration
    : IEntityTypeConfiguration<TourLocation>
{
    public void Configure(
        EntityTypeBuilder<TourLocation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "TourLocations",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_TourLocations_Latitude",
                    "[Latitude] >= -90 " +
                    "AND [Latitude] <= 90");

                tableBuilder.HasCheckConstraint(
                    "CK_TourLocations_Longitude",
                    "[Longitude] >= -180 " +
                    "AND [Longitude] <= 180");

                tableBuilder.HasCheckConstraint(
                    "CK_TourLocations_TriggerRadius",
                    "[TriggerRadiusMeters] > 0 " +
                    "AND [TriggerRadiusMeters] <= 500");

                tableBuilder.HasCheckConstraint(
                    "CK_TourLocations_DisplayOrder",
                    "[DisplayOrder] > 0");
            });

        builder.HasKey(location => location.Id);

        builder.Property(location => location.Id)
            .ValueGeneratedNever();

        builder.Property(location => location.TourId)
            .IsRequired();

        builder.Property(location => location.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(location => location.DefaultName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(location => location.Latitude)
            .IsRequired()
            .HasPrecision(9, 6);

        builder.Property(location => location.Longitude)
            .IsRequired()
            .HasPrecision(9, 6);

        builder.Property(
                location =>
                    location.TriggerRadiusMeters)
            .IsRequired()
            .HasPrecision(8, 2)
            .HasDefaultValue(
                GpsDefaults.TriggerRadiusMeters);

        builder.Property(location => location.DisplayOrder)
            .IsRequired();

        builder.Property(location => location.ImageUrl)
            .HasMaxLength(2_000);

        builder.Property(location => location.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(location => location.CreatedAt)
            .IsRequired();

        builder.Property(location => location.UpdatedAt);

        builder.HasIndex(
                location =>
                    new
                    {
                        location.TourId,
                        location.Code
                    })
            .IsUnique()
            .HasDatabaseName(
                "UX_TourLocations_TourId_Code");

        builder.HasIndex(
                location =>
                    new
                    {
                        location.TourId,
                        location.DisplayOrder
                    })
            .IsUnique()
            .HasDatabaseName(
                "UX_TourLocations_" +
                "TourId_DisplayOrder");

        builder.HasIndex(
                location =>
                    new
                    {
                        location.Latitude,
                        location.Longitude
                    })
            .HasDatabaseName(
                "IX_TourLocations_" +
                "Latitude_Longitude");

        builder.HasIndex(location => location.IsActive)
            .HasDatabaseName(
                "IX_TourLocations_IsActive");

        builder.HasOne(location => location.Tour)
            .WithMany(tour => tour.Locations)
            .HasForeignKey(location => location.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(location => location.Translations)
            .WithOne(
                translation =>
                    translation.TourLocation)
            .HasForeignKey(
                translation =>
                    translation.TourLocationId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(location => location.Visits)
            .WithOne(visit => visit.TourLocation)
            .HasForeignKey(
                visit => visit.TourLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(
                location =>
                    location.PlaybackProgresses)
            .WithOne(
                progress =>
                    progress.TourLocation)
            .HasForeignKey(
                progress =>
                    progress.TourLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasMany(
                location =>
                    location.LocationDetectionLogs)
            .WithOne(
                log =>
                    log.DetectedTourLocation)
            .HasForeignKey(
                log =>
                    log.DetectedTourLocationId)
            .OnDelete(DeleteBehavior.SetNull);
    }
}