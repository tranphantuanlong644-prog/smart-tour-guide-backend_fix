using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTourGuide.Data.Context;
using SmartTourGuide.Data.Entities;
using SmartTourGuide.Shared.Enums;

namespace SmartTourGuide.API.Controllers;

[ApiController]
[Authorize(Policy = "AdminDashboardAccess")]
[Route("api/admin")]
public sealed class AdminDashboardController : ControllerBase
{
    private readonly SmartTourGuideDbContext _db;

    public AdminDashboardController(SmartTourGuideDbContext db) => _db = db;

    private IQueryable<TourSession> ActiveGuestSessions(DateTimeOffset now)
    {
        // Guest heartbeats arrive every 3 seconds; admin polls every 2 seconds.
        var activeSince = now.AddSeconds(-13);
        return _db.TourSessions
            .Where(x => x.IdentityType == IdentityType.Guest
                && x.Status == TourSessionStatus.Active
                && x.ExpiresAt > now
                && x.LastActivityAt > activeSince
                && x.GuestIdentityId.HasValue);
    }

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var activeGuests = await ActiveGuestSessions(now)
            .Select(x => x.GuestIdentityId!.Value)
            .Distinct()
            .CountAsync(cancellationToken);

        return Ok(new
        {
            tours = await _db.TourQrCodes.Select(x => x.TourId).Distinct().CountAsync(cancellationToken),
            activeQrCodes = await _db.TourQrCodes.CountAsync(x => x.IsActive && x.ExpiresAt > now, cancellationToken),
            scans = await _db.TourQrCodes.SumAsync(x => (int?)x.UsageCount, cancellationToken) ?? 0,
            guestSessions = await _db.TourSessions.CountAsync(
                x => x.IdentityType == IdentityType.Guest && !x.ExcludedFromGuestTotal, cancellationToken),
            activeSessions = activeGuests
        });
    }

    [HttpPost("overview/reset")]
    public async Task<IActionResult> ResetOverview(CancellationToken cancellationToken)
    {
        var strategy = _db.Database.CreateExecutionStrategy();
        await strategy.ExecuteAsync(async () =>
        {
            await using var transaction = await _db.Database.BeginTransactionAsync(cancellationToken);
            await _db.TourQrCodes.ExecuteUpdateAsync(
                update => update.SetProperty(x => x.UsageCount, 0), cancellationToken);
            await _db.TourSessions
                .Where(x => x.IdentityType == IdentityType.Guest && !x.ExcludedFromGuestTotal)
                .ExecuteUpdateAsync(update => update.SetProperty(x => x.ExcludedFromGuestTotal, true), cancellationToken);
            await transaction.CommitAsync(cancellationToken);
        });
        return Ok(new { resetAt = DateTimeOffset.UtcNow });
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetGuestSessions(CancellationToken cancellationToken)
    {
        var activeIds = ActiveGuestSessions(DateTimeOffset.UtcNow).Select(x => x.Id);
        var sessions = await _db.TourSessions.AsNoTracking()
            .Where(x => x.IdentityType == IdentityType.Guest && !x.ExcludedFromGuestTotal)
            .Include(x => x.GuestIdentity)
            .Include(x => x.SourceQrCode)
            .OrderByDescending(x => x.StartedAt)
            .Take(100)
            .Select(x => new
            {
                x.Id, TourName = x.SourceQrCode == null ? "Tour" : x.SourceQrCode.TourName,
                Guest = x.GuestIdentity == null ? "Khách" : x.GuestIdentity.DisplayName,
                x.LanguageCode, x.Status, x.StartedAt, x.LastActivityAt,
                IsActive = activeIds.Contains(x.Id),
                x.ExpiresAt, x.SourceQrCodeId
            }).ToListAsync(cancellationToken);
        return Ok(sessions);
    }
}
