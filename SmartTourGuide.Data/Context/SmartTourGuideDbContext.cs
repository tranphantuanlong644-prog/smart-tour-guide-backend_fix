using Microsoft.EntityFrameworkCore;
using SmartTourGuide.Data.Entities;

namespace SmartTourGuide.Data.Context;

public sealed class SmartTourGuideDbContext
    : DbContext
{
    public SmartTourGuideDbContext(
        DbContextOptions<SmartTourGuideDbContext>
            options)
        : base(options)
    {
    }

    public DbSet<GuestIdentity> GuestIdentities =>
        Set<GuestIdentity>();

    public DbSet<TourLocation> TourLocations =>
        Set<TourLocation>();

    public DbSet<TourLocationTranslation>
        TourLocationTranslations =>
        Set<TourLocationTranslation>();

    public DbSet<TourQrCode> TourQrCodes =>
        Set<TourQrCode>();

    public DbSet<TourSession> TourSessions =>
        Set<TourSession>();


    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(
            modelBuilder);

        base.OnModelCreating(modelBuilder);

        ConfigureModel(modelBuilder);
    }

    internal static void ConfigureModel(ModelBuilder modelBuilder)
    {
        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SmartTourGuideDbContext).Assembly);
    }
}
