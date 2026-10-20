using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTourGuide.Data.Entities;

namespace SmartTourGuide.Data.Configurations;

public sealed class GuestIdentityConfiguration
    : IEntityTypeConfiguration<GuestIdentity>
{
    public void Configure(
        EntityTypeBuilder<GuestIdentity> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("GuestIdentities");

        builder.HasKey(
            guestIdentity => guestIdentity.Id);

        builder.Property(
                guestIdentity => guestIdentity.Id)
            .ValueGeneratedNever();

        builder.Property(
                guestIdentity =>
                    guestIdentity.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

        builder.Property(
                guestIdentity =>
                    guestIdentity.PreferredLanguageCode)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("vi");

        builder.Property(
                guestIdentity =>
                    guestIdentity.DeviceId)
            .IsRequired()
            .HasMaxLength(200);

        builder.Property(
                guestIdentity =>
                    guestIdentity.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(
                guestIdentity =>
                    guestIdentity.CreatedAt)
            .IsRequired();

        builder.Property(
                guestIdentity =>
                    guestIdentity.ExpiresAt)
            .IsRequired();

        builder.Property(
            guestIdentity =>
                guestIdentity.LastAccessedAt);

        builder.HasIndex(
                guestIdentity =>
                    guestIdentity.DeviceId)
            .HasDatabaseName(
                "IX_GuestIdentities_DeviceId");

        builder.HasIndex(
                guestIdentity =>
                    guestIdentity.ExpiresAt)
            .HasDatabaseName(
                "IX_GuestIdentities_ExpiresAt");

        builder.HasMany(
                guestIdentity =>
                    guestIdentity.TourSessions)
            .WithOne(
                session =>
                    session.GuestIdentity)
            .HasForeignKey(
                session =>
                    session.GuestIdentityId)
            .OnDelete(
                DeleteBehavior.Restrict);
    }
}