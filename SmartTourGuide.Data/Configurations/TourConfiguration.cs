using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTourGuide.Data.Entities;

namespace SmartTourGuide.Data.Configurations;

public sealed class TourConfiguration
    : IEntityTypeConfiguration<Tour>
{
    public void Configure(
        EntityTypeBuilder<Tour> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "Tours",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_Tours_CenterLatitude",
                    "[CenterLatitude] IS NULL OR " +
                    "([CenterLatitude] >= -90 " +
                    "AND [CenterLatitude] <= 90)");

                tableBuilder.HasCheckConstraint(
                    "CK_Tours_CenterLongitude",
                    "[CenterLongitude] IS NULL OR " +
                    "([CenterLongitude] >= -180 " +
                    "AND [CenterLongitude] <= 180)");
            });

        builder.HasKey(tour => tour.Id);

        builder.Property(tour => tour.Id)
            .ValueGeneratedNever();

        builder.Property(tour => tour.Code)
            .IsRequired()
            .HasMaxLength(50);

        builder.Property(tour => tour.DefaultName)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(tour => tour.CoverImageUrl)
            .HasMaxLength(2_000);

        builder.Property(tour => tour.Address)
            .HasMaxLength(500);

        builder.Property(tour => tour.CenterLatitude)
            .HasPrecision(9, 6);

        builder.Property(tour => tour.CenterLongitude)
            .HasPrecision(9, 6);

        builder.Property(tour => tour.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(tour => tour.CreatedAt)
            .IsRequired();

        builder.Property(tour => tour.UpdatedAt);

        builder.HasIndex(tour => tour.Code)
            .IsUnique()
            .HasDatabaseName(
                "UX_Tours_Code");

        builder.HasIndex(tour => tour.IsActive)
            .HasDatabaseName(
                "IX_Tours_IsActive");

        builder.HasMany(tour => tour.Translations)
            .WithOne(translation => translation.Tour)
            .HasForeignKey(
                translation => translation.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(tour => tour.Locations)
            .WithOne(location => location.Tour)
            .HasForeignKey(location => location.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(tour => tour.QrCodes)
            .WithOne(qrCode => qrCode.Tour)
            .HasForeignKey(qrCode => qrCode.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(tour => tour.Sessions)
            .WithOne(session => session.Tour)
            .HasForeignKey(session => session.TourId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}