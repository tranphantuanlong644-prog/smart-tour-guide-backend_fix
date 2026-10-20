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
                    "(" +
                    "[IdentityType] = 1 " +
                    "AND [UserId] IS NOT NULL " +
                    "AND [GuestIdentityId] IS NULL" +
                    ") OR (" +
                    "[IdentityType] = 2 " +
                    "AND [UserId] IS NULL " +
                    "AND [GuestIdentityId] IS NOT NULL" +
                    ")");

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

        builder.Property(session => session.UserId);

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
                        session.UserId,
                        session.Status
                    })
            .HasDatabaseName(
                "IX_TourSessions_UserId_Status");

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

        builder.HasOne(session => session.Tour)
            .WithMany(tour => tour.Sessions)
            .HasForeignKey(session => session.TourId)
            .OnDelete(DeleteBehavior.Restrict);

        builder.HasOne(session => session.User)
            .WithMany(user => user.TourSessions)
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Restrict);

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

        builder.HasMany(session => session.Visits)
            .WithOne(visit => visit.TourSession)
            .HasForeignKey(
                visit => visit.TourSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(
                session =>
                    session.PlaybackProgresses)
            .WithOne(
                progress =>
                    progress.TourSession)
            .HasForeignKey(
                progress =>
                    progress.TourSessionId)
            .OnDelete(DeleteBehavior.Cascade);

        builder.HasMany(
                session =>
                    session.LocationDetectionLogs)
            .WithOne(log => log.TourSession)
            .HasForeignKey(
                log => log.TourSessionId)
            .OnDelete(DeleteBehavior.Cascade);
    }
}