using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTourGuide.Data.Entities;

namespace SmartTourGuide.Data.Configurations;

public sealed class TourTranslationConfiguration
    : IEntityTypeConfiguration<TourTranslation>
{
    public void Configure(
        EntityTypeBuilder<TourTranslation> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("TourTranslations");

        builder.HasKey(
            translation => translation.Id);

        builder.Property(
                translation => translation.Id)
            .ValueGeneratedNever();

        builder.Property(
                translation =>
                    translation.TourId)
            .IsRequired();

        builder.Property(
                translation =>
                    translation.LanguageCode)
            .IsRequired()
            .HasMaxLength(10);

        builder.Property(
                translation =>
                    translation.Name)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(
                translation =>
                    translation.Description)
            .IsRequired()
            .HasMaxLength(4_000);

        builder.Property(
                translation =>
                    translation.WelcomeMessage)
            .HasMaxLength(2_000);

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
                        translation.TourId,
                        translation.LanguageCode
                    })
            .IsUnique()
            .HasDatabaseName(
                "UX_TourTranslations_" +
                "TourId_LanguageCode");

        builder.HasIndex(
                translation =>
                    translation.LanguageCode)
            .HasDatabaseName(
                "IX_TourTranslations_" +
                "LanguageCode");

        builder.HasOne(
                translation =>
                    translation.Tour)
            .WithMany(
                tour =>
                    tour.Translations)
            .HasForeignKey(
                translation =>
                    translation.TourId)
            .OnDelete(
                DeleteBehavior.Cascade);
    }
}