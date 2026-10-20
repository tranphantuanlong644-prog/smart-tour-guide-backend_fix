using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTourGuide.Data.Entities;
using SmartTourGuide.Shared.Enums;

namespace SmartTourGuide.Data.Configurations;

public sealed class UserConfiguration
    : IEntityTypeConfiguration<User>
{
    public void Configure(
        EntityTypeBuilder<User> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable("Users");

        builder.HasKey(user => user.Id);

        builder.Property(user => user.Id)
            .ValueGeneratedNever();

        builder.Property(user => user.Email)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(user => user.NormalizedEmail)
            .IsRequired()
            .HasMaxLength(320);

        builder.Property(user => user.PasswordHash)
            .IsRequired()
            .HasMaxLength(500);

        builder.Property(user => user.DisplayName)
            .IsRequired()
            .HasMaxLength(100);

       builder.Property(user => user.Role)
    .IsRequired()
    .HasConversion<int>();
        builder.Property(user => user.IsActive)
            .IsRequired()
            .HasDefaultValue(true);

        builder.Property(
                user => user.PreferredLanguageCode)
            .IsRequired()
            .HasMaxLength(10)
            .HasDefaultValue("vi");

        builder.Property(user => user.CreatedAt)
            .IsRequired();

        builder.Property(user => user.UpdatedAt);

        builder.Property(user => user.LastLoginAt);

        builder.HasIndex(user => user.NormalizedEmail)
            .IsUnique()
            .HasDatabaseName(
                "UX_Users_NormalizedEmail");

        builder.HasMany(user => user.TourSessions)
            .WithOne(session => session.User)
            .HasForeignKey(session => session.UserId)
            .OnDelete(DeleteBehavior.Restrict);
    }
}
