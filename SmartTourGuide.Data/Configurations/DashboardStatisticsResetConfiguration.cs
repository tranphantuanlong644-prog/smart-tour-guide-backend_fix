using Microsoft.EntityFrameworkCore;
using Microsoft.EntityFrameworkCore.Metadata.Builders;
using SmartTourGuide.Data.Entities;

namespace SmartTourGuide.Data.Configurations;

public sealed class DashboardStatisticsResetConfiguration
    : IEntityTypeConfiguration<DashboardStatisticsReset>
{
    public void Configure(
        EntityTypeBuilder<DashboardStatisticsReset> builder)
    {
        ArgumentNullException.ThrowIfNull(builder);

        builder.ToTable(
            "DashboardStatisticsResets",
            tableBuilder =>
            {
                tableBuilder.HasCheckConstraint(
                    "CK_DashboardStatisticsResets_Singleton",
                    "[Id] = 1");

                tableBuilder.HasCheckConstraint(
                    "CK_DashboardStatisticsResets_ScanCount",
                    "[ScanCountAtReset] >= 0");
            });

        builder.HasKey(state => state.Id);
        builder.Property(state => state.Id).ValueGeneratedNever();
        builder.Property(state => state.ResetAt).IsRequired();
        builder.Property(state => state.ScanCountAtReset).IsRequired();
    }
}
