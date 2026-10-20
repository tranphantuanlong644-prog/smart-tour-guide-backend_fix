using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTourGuide.Data.Entities;

namespace SmartTourGuide.Data.Configurations;

public sealed class TourVisitConfiguration
    : IEntityTypeConfiguration<TourVisit>
{
    public void Configure(
        EntityTypeBuilder<TourVisit> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "TourVisits",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_TourVisits_DistanceMeters",
                    "[DistanceMeters] IS NULL OR " +
                    "[DistanceMeters] >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_TourVisits_AccuracyMeters",
                    "[AccuracyMeters] IS NULL OR " +
                    "[AccuracyMeters] >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_TourVisits_DetectedLatitude",
                    "[DetectedLatitude] IS NULL OR " +
                    "([DetectedLatitude] >= -90 " +
                    "AND [DetectedLatitude] <= 90)");

                tableBuilder.HasCheckConstraint(
                    "CK_TourVisits_DetectedLongitude",
                    "[DetectedLongitude] IS NULL OR " +
                    "([DetectedLongitude] >= -180 " +
                    "AND [DetectedLongitude] <= 180)");

                tableBuilder.HasCheckConstraint(
                    "CK_TourVisits_AudioTriggeredAt",
                    "([AudioTriggered] = 0 " +
                    "AND [AudioTriggeredAt] IS NULL) " +
                    "OR " +
                    "([AudioTriggered] = 1 " +
                    "AND [AudioTriggeredAt] IS NOT NULL)");
            });

        builder.HasKey(visit => visit.Id);

        builder.Property(visit => visit.Id)
            .ValueGeneratedNever();

        builder.Property(visit => visit.TourSessionId)
            .IsRequired();

        builder.Property(visit => visit.TourLocationId)
            .IsRequired();

        builder.Property(visit => visit.DetectionMethod)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(visit => visit.LanguageCode)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("vi");

        builder.Property(visit => visit.VisitedAt)
            .IsRequired();

        builder.Property(visit => visit.DistanceMeters)
            .HasPrecision(10, 2);

        builder.Property(visit => visit.AccuracyMeters)
            .HasPrecision(10, 2);

        builder.Property(visit => visit.DetectedLatitude)
            .HasPrecision(9, 6);

        builder.Property(visit => visit.DetectedLongitude)
            .HasPrecision(9, 6);

        builder.Property(visit => visit.AudioTriggered)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(visit => visit.AudioTriggeredAt);

        builder.HasIndex(
                visit =>
                    new
                    {
                        visit.TourSessionId,
                        visit.VisitedAt
                    })
            .HasDatabaseName(
                "IX_TourVisits_" +
                "TourSessionId_VisitedAt");

        builder.HasIndex(
                visit =>
                    new
                    {
                        visit.TourLocationId,
                        visit.VisitedAt
                    })
            .HasDatabaseName(
                "IX_TourVisits_" +
                "TourLocationId_VisitedAt");

        builder.HasIndex(
                visit =>
                    new
                    {
                        visit.TourSessionId,
                        visit.TourLocationId,
                        visit.VisitedAt
                    })
            .HasDatabaseName(
                "IX_TourVisits_Session_" +
                "Location_VisitedAt");

        builder.HasOne(visit => visit.TourSession)
            .WithMany(session => session.Visits)
            .HasForeignKey(
                visit => visit.TourSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(visit => visit.TourLocation)
            .WithMany(location => location.Visits)
            .HasForeignKey(
                visit => visit.TourLocationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}