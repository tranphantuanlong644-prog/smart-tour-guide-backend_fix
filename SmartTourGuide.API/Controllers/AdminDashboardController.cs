using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTourGuide.Data.Context;
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
        var activeGuests = await _db.TourSessions
            .Where(x => x.IdentityType == IdentityType.Guest
                && x.Status == TourSessionStatus.Active
                && x.ExpiresAt > now
                && x.LastActivityAt >= activeSince
                && x.GuestIdentityId.HasValue)
            .Select(x => x.GuestIdentityId!.Value)
            .Distinct()
            .CountAsync(cancellationToken);
        return Ok(new
        {
            tours = await _db.Tours.CountAsync(x => x.IsActive, cancellationToken),
            activeQrCodes = await _db.TourQrCodes.CountAsync(
                x => x.IsActive && x.ExpiresAt > now, cancellationToken),
            scans = await _db.TourQrCodes.SumAsync(
                x => (int?)x.UsageCount, cancellationToken) ?? 0,
            guestSessions = await _db.TourSessions.CountAsync(
                x => x.IdentityType == IdentityType.Guest, cancellationToken),
            activeSessions = activeGuests
        });
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