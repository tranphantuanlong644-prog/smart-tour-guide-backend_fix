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

    public DbSet<User> Users =>
        Set<User>();

    public DbSet<GuestIdentity> GuestIdentities =>
        Set<GuestIdentity>();

    public DbSet<Tour> Tours =>
        Set<Tour>();

    public DbSet<TourTranslation> TourTranslations =>
        Set<TourTranslation>();

    public DbSet<TourLocation> TourLocations =>
        Set<TourLocation>();

    public DbSet<TourLocationTranslation>
        TourLocationTranslations =>
        Set<TourLocationTranslation>();

    public DbSet<TourQrCode> TourQrCodes =>
        Set<TourQrCode>();

    public DbSet<TourSession> TourSessions =>
        Set<TourSession>();

    public DbSet<TourVisit> TourVisits =>
        Set<TourVisit>();

    public DbSet<AudioPlaybackProgress>
        AudioPlaybackProgresses =>
        Set<AudioPlaybackProgress>();

    public DbSet<LocationDetectionLog>
        LocationDetectionLogs =>
        Set<LocationDetectionLog>();

    protected override void OnModelCreating(
        ModelBuilder modelBuilder)
    {
        ArgumentNullException.ThrowIfNull(
            modelBuilder);

        base.OnModelCreating(modelBuilder);

        modelBuilder.ApplyConfigurationsFromAssembly(
            typeof(SmartTourGuideDbContext)
                .Assembly);
    }
}