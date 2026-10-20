using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTourGuide.Data.Entities;

namespace SmartTourGuide.Data.Configurations;

public sealed class TourLocationTranslationConfiguration
    : IEntityTypeConfiguration<TourLocationTranslation>
{
    public void Configure(
        EntityTypeBuilder<TourLocationTranslation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "TourLocationTranslations",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_TourLocationTranslations_" +
                    "DurationSeconds",
                    "[DurationSeconds] > 0");
            });

        builder.HasKey(
            translation => translation.Id);

        builder.Property(
                translation => translation.Id)
            .ValueGeneratedNever();

        builder.Property(
                translation =>
                    translation.TourLocationId)
            .IsRequired();

        builder.Property(
                translation =>
                    translation.LanguageCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(
                translation =>
                    translation.Title)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(
                translation =>
                    translation.Description)
            .IsRequired()
            .HasMaxLength(2_000);

        builder.Property(
                translation =>
                    translation.NarrationText)
            .IsRequired()
            .HasMaxLength(10_000);

        builder.Property(
                translation =>
                    translation.AudioUrl)
            .IsRequired()
            .HasMaxLength(2_000);

        builder.Property(
                translation =>
                    translation.DurationSeconds)
            .IsRequired()
            .HasPrecision(10, 2);

        builder.Property(
                translation =>
                    translation.NarratorName)
            .HasMaxLength(200);

        builder.Property(
                translation =>
                    translation.CreatedAt)
            .IsRequired();

        builder.Property(
            translation =>
                translation.UpdatedAt);

        builder.HasIndex(
                translation =>
                    new
                    {
                        translation.TourLocationId,
                        translation.LanguageCode
                    })
            .IsUnique()
            .HasDatabaseName(
                "UX_TourLocationTranslations_" +
                "TourLocationId_LanguageCode");

        builder.HasIndex(
                translation =>
                    translation.LanguageCode)
            .HasDatabaseName(
                "IX_TourLocationTranslations_" +
                "LanguageCode");

        builder.HasOne(
                translation =>
                    translation.TourLocation)
            .WithMany(
                location =>
                    location.Translations)
            .HasForeignKey(
                translation =>
                    translation.TourLocationId)
            .OnDelete(
                DeleteBehavior.Cascade);

        builder.HasMany(
                translation =>
                    translation.PlaybackProgresses)
            .WithOne(
                progress =>
                    progress.Translation)
            .HasForeignKey(
                progress =>
                    progress.TourLocationTranslationId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}