using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTourGuide.Data.Entities;

namespace SmartTourGuide.Data.Configurations;

public sealed class AudioPlaybackProgressConfiguration
    : IEntityTypeConfiguration<AudioPlaybackProgress>
{
    public void Configure(
        EntityTypeBuilder<AudioPlaybackProgress> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "AudioPlaybackProgresses",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_AudioPlaybackProgresses_" +
                    "PositionSeconds",
                    "[PositionSeconds] >= 0 " +
                    "AND [PositionSeconds] <= " +
                    "[DurationSeconds]");

                tableBuilder.HasCheckConstraint(
                    "CK_AudioPlaybackProgresses_" +
                    "DurationSeconds",
                    "[DurationSeconds] > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_AudioPlaybackProgresses_" +
                    "CompletionPercentage",
                    "[CompletionPercentage] >= 0 " +
                    "AND [CompletionPercentage] <= 100");

                tableBuilder.HasCheckConstraint(
                    "CK_AudioPlaybackProgresses_" +
                    "CompletedAt",
                    "([IsCompleted] = 0 " +
                    "AND [CompletedAt] IS NULL) " +
                    "OR " +
                    "([IsCompleted] = 1 " +
                    "AND [CompletedAt] IS NOT NULL)");
            });

        builder.HasKey(progress => progress.Id);

        builder.Property(progress => progress.Id)
            .ValueGeneratedNever();

        builder.Property(
                progress => progress.TourSessionId)
            .IsRequired();

        builder.Property(
                progress => progress.TourLocationId)
            .IsRequired();

        builder.Property(
                progress =>
                    progress.TourLocationTranslationId)
            .IsRequired();

        builder.Property(progress => progress.LanguageCode)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("vi");

        builder.Property(progress => progress.PositionSeconds)
            .IsRequired()
            .HasPrecision(10, 2)
            .HasDefaultValue(0);

        builder.Property(progress => progress.DurationSeconds)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(
                progress =>
                    progress.CompletionPercentage)
            .IsRequired()
            .HasPrecision(5, 2)
            .HasDefaultValue(0);

        builder.Property(progress => progress.IsCompleted)
            .IsRequired()
            .HasDefaultValue(false);

        builder.Property(progress => progress.CompletedAt);

        builder.Property(progress => progress.LastListenedAt)
            .IsRequired();

        builder.Property(progress => progress.CreatedAt)
            .IsRequired();

        builder.Property(progress => progress.UpdatedAt);

        builder.HasIndex(
                progress =>
                    new
                    {
                        progress.TourSessionId,
                        progress
                            .TourLocationTranslationId
                    })
            .IsUnique()
            .HasDatabaseName(
                "UX_AudioPlaybackProgresses_" +
                "Session_Translation");

        builder.HasIndex(
                progress =>
                    new
                    {
                        progress.TourSessionId,
                        progress.LastListenedAt
                    })
            .HasDatabaseName(
                "IX_AudioPlaybackProgresses_" +
                "Session_LastListenedAt");

        builder.HasIndex(
                progress =>
                    new
                    {
                        progress.TourLocationId,
                        progress.LanguageCode
                    })
            .HasDatabaseName(
                "IX_AudioPlaybackProgresses_" +
                "Location_Language");

        builder.HasOne(progress => progress.TourSession)
            .WithMany(
                session =>
                    session.PlaybackProgresses)
            .HasForeignKey(
                progress =>
                    progress.TourSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasOne(progress => progress.TourLocation)
            .WithMany(
                location =>
                    location.PlaybackProgresses)
            .HasForeignKey(
                progress =>
                    progress.TourLocationId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(progress => progress.Translation)
            .WithMany(
                translation =>
                    translation.PlaybackProgresses)
            .HasForeignKey(
                progress =>
                    progress
                        .TourLocationTranslationId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}