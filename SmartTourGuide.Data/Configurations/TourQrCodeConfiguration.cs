using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTourGuide.Data.Entities;
using SmartTourGuide.Shared.Enums;

namespace SmartTourGuide.Data.Configurations;

public sealed class TourQrCodeConfiguration
    : IEntityTypeConfiguration<TourQrCode>
{
    public void Configure(
        EntityTypeBuilder<TourQrCode> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "TourQrCodes",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_TourQrCodes_UsageCount",
                    "[UsageCount] >= 0");

                tableBuilder.HasCheckConstraint(
                    "CK_TourQrCodes_MaximumUsageCount",
                    "[MaximumUsageCount] IS NULL OR " +
                    "[MaximumUsageCount] > 0");

                tableBuilder.HasCheckConstraint(
                    "CK_TourQrCodes_UsageLimit",
                    "[MaximumUsageCount] IS NULL OR " +
                    "[UsageCount] <= " +
                    "[MaximumUsageCount]");

                tableBuilder.HasCheckConstraint(
                    "CK_TourQrCodes_Expiration",
                    "[ExpiresAt] > [CreatedAt]");

                tableBuilder.HasCheckConstraint(
                    "CK_TourQrCodes_SingleUse",
                    "[Type] <> 2 OR " +
                    "[MaximumUsageCount] IS NULL OR " +
                    "[MaximumUsageCount] = 1");
            });

        builder.HasKey(qrCode => qrCode.Id);

        builder.Property(qrCode => qrCode.Id)
            .ValueGeneratedNever();

        builder.Property(qrCode => qrCode.TourId)
            .IsRequired();

        builder.Property(qrCode => qrCode.AccessCode)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(qrCode => qrCode.Type)
    .IsRequired()
    .HasConversion<int>();

        builder.Property(qrCode => qrCode.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(qrCode => qrCode.UsageCount)
            .IsRequired()
            .HasDefaultValue(0);

        builder.Property(
            qrCode => qrCode.MaximumUsageCount);

        builder.Property(qrCode => qrCode.CreatedAt)
            .IsRequired();

        builder.Property(qrCode => qrCode.ExpiresAt)
            .IsRequired();

        builder.Property(qrCode => qrCode.UsedAt);

        builder.Property(qrCode => qrCode.LastUsedAt);

        builder.Property(qrCode => qrCode.DeactivatedAt);

        builder.HasIndex(qrCode => qrCode.AccessCode)
            .IsUnique()
            .HasDatabaseName(
                "UX_TourQrCodes_AccessCode");

        builder.HasIndex(
                qrCode =>
                    new
                    {
                        qrCode.TourId,
                        qrCode.IsActive
                    })
            .HasDatabaseName(
                "IX_TourQrCodes_TourId_IsActive");

        builder.HasIndex(qrCode => qrCode.ExpiresAt)
            .HasDatabaseName(
                "IX_TourQrCodes_ExpiresAt");

        builder.HasOne(qrCode => qrCode.Tour)
            .WithMany(tour => tour.QrCodes)
            .HasForeignKey(qrCode => qrCode.TourId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(qrCode => qrCode.Sessions)
            .WithOne(session => session.SourceQrCode)
            .HasForeignKey(
                session => session.SourceQrCodeId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
