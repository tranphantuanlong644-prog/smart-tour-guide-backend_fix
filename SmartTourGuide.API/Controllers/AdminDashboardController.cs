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

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var activeSince = now.AddSeconds(-120);
        var resetState = await _db.DashboardStatisticsResets
            .AsNoTracking()
            .SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);
        var activeGuests = await _db.TourSessions
            .Where(x => x.IdentityType == IdentityType.Guest
                && x.Status == TourSessionStatus.Active
                && x.ExpiresAt > now
                && x.LastActivityAt >= activeSince
                && (resetState == null || x.LastActivityAt >= resetState.ResetAt)
                && x.GuestIdentityId.HasValue)
            .Select(x => x.GuestIdentityId!.Value)
            .Distinct()
            .CountAsync(cancellationToken);
        return Ok(new
        {
            tours = await _db.Tours.CountAsync(x => x.IsActive, cancellationToken),
            activeQrCodes = await _db.TourQrCodes.CountAsync(
                x => x.IsActive && x.ExpiresAt > now, cancellationToken),
            scans = Math.Max(
                0,
                (await _db.TourQrCodes.SumAsync(
                    x => (int?)x.UsageCount, cancellationToken) ?? 0)
                - (resetState?.ScanCountAtReset ?? 0)),
            guestSessions = await _db.TourSessions.CountAsync(
                x => x.IdentityType == IdentityType.Guest
                    && (resetState == null || x.StartedAt >= resetState.ResetAt),
                cancellationToken),
            activeSessions = activeGuests
        });
    }

    [HttpPost("overview/reset")]
    public async Task<IActionResult> ResetOverview(
        CancellationToken cancellationToken)
    {
        var now = DateTimeOffset.UtcNow;
        var scanCount = await _db.TourQrCodes.SumAsync(
            x => (int?)x.UsageCount,
            cancellationToken) ?? 0;

        var resetState = await _db.DashboardStatisticsResets
            .SingleOrDefaultAsync(x => x.Id == 1, cancellationToken);

        if (resetState is null)
        {
            resetState = new DashboardStatisticsReset
            {
                Id = 1,
                ResetAt = now,
                ScanCountAtReset = scanCount
            };
            _db.DashboardStatisticsResets.Add(resetState);
        }
        else
        {
            resetState.ResetAt = now;
            resetState.ScanCountAtReset = scanCount;
        }

        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { resetAt = resetState.ResetAt });
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetGuestSessions(CancellationToken cancellationToken)
    {
        var sessions = await _db.TourSessions.AsNoTracking()
            .Where(x => x.IdentityType == IdentityType.Guest)
            .OrderByDescending(x => x.StartedAt)
            .Take(100)
            .Select(x => new
            {
                x.Id, TourName = x.Tour.DefaultName,
                Guest = x.GuestIdentity == null ? "Khách" : x.GuestIdentity.DisplayName,
                x.LanguageCode, x.Status, x.StartedAt, x.LastActivityAt,
                x.ExpiresAt, x.SourceQrCodeId
            }).ToListAsync(cancellationToken);
        return Ok(sessions);
    }
}
