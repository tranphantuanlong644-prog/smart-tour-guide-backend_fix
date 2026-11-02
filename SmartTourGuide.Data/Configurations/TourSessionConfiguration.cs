using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTourGuide.Data.Entities;

namespace SmartTourGuide.Data.Configurations;

public sealed class TourSessionConfiguration
    : IEntityTypeConfiguration<TourSession>
{
    public void Configure(
        EntityTypeBuilder<TourSession> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "TourSessions",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_TourSessions_Expiration",
                    "[ExpiresAt] > [StartedAt]");

                tableBuilder.HasCheckConstraint(
                    "CK_TourSessions_Identity",
                    "([IdentityType] = 1 " +
                    "AND [GuestIdentityId] IS NULL) OR (" +
                    "[IdentityType] = 2 " +
                    "AND [GuestIdentityId] IS NOT NULL)");

                tableBuilder.HasCheckConstraint(
                    "CK_TourSessions_EndTime",
                    "[EndedAt] IS NULL OR " +
                    "[EndedAt] >= [StartedAt]");
            });

        builder.HasKey(session => session.Id);

        builder.Property(session => session.Id)
            .ValueGeneratedNever();

        builder.Property(session => session.TourId)
            .IsRequired();

        builder.Property(session => session.IdentityType)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(
            session => session.GuestIdentityId);

        builder.Property(
            session => session.SourceQrCodeId);

        builder.Property(session => session.LanguageCode)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("vi");

        builder.Property(session => session.Status)
            .IsRequired()
            .HasConversion<int>();

        builder.Property(session => session.StartedAt)
            .IsRequired();

        builder.Property(session => session.ExpiresAt)
            .IsRequired();

        builder.Property(session => session.EndedAt);

        builder.Property(
            session => session.LastActivityAt);

        builder.HasIndex(
                session =>
                    new
                    {
                        session.GuestIdentityId,
                        session.Status
                    })
            .HasDatabaseName(
                "IX_TourSessions_" +
                "GuestIdentityId_Status");

        builder.HasIndex(
                session =>
                    new
                    {
                        session.TourId,
                        session.Status
                    })
            .HasDatabaseName(
                "IX_TourSessions_TourId_Status");

        builder.HasIndex(session => session.ExpiresAt)
            .HasDatabaseName(
                "IX_TourSessions_ExpiresAt");

        builder.HasOne(session => session.GuestIdentity)
            .WithMany(
                guestIdentity =>
                    guestIdentity.TourSessions)
            .HasForeignKey(
                session =>
                    session.GuestIdentityId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(session => session.SourceQrCode)
            .WithMany(qrCode => qrCode.Sessions)
            .HasForeignKey(
                session => session.SourceQrCodeId)
            .OnDelete(DeleteBehavior.Restrict);

    }
}
