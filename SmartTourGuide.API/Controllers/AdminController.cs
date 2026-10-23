using System.Net;
using System.Security.Cryptography;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTourGuide.Data.Context;
using SmartTourGuide.Data.Entities;
using SmartTourGuide.Shared.Constants;
using SmartTourGuide.Shared.Enums;

namespace SmartTourGuide.API.Controllers;

/// <summary>
/// No-login administration for a locally hosted demo. All data endpoints
/// require a loopback connection and the localhost host name.
/// </summary>
[ApiController]
[Route("api/admin")]
public sealed class AdminController : ControllerBase
{
    private readonly SmartTourGuideDbContext _db;

    public AdminController(SmartTourGuideDbContext db) => _db = db;

    [HttpGet("overview")]
    public async Task<IActionResult> GetOverview(CancellationToken cancellationToken)
    {
        if (!IsLocalRequest()) return Forbid();

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

    [HttpGet("tours")]
    public async Task<IActionResult> GetTours(CancellationToken cancellationToken)
    {
        if (!IsLocalRequest()) return Forbid();
        var tours = await _db.Tours.AsNoTracking()
            .OrderBy(x => x.DefaultName)
            .Select(x => new { x.Id, x.Code, x.DefaultName, x.IsActive, x.Address })
            .ToListAsync(cancellationToken);
        return Ok(tours);
    }

    [HttpGet("tours/{id:guid}")]
    public async Task<IActionResult> GetTour(Guid id, CancellationToken cancellationToken)
    {
        if (!IsLocalRequest()) return Forbid();
        var tour = await _db.Tours.AsNoTracking()
            .Where(x => x.Id == id)
            .Select(x => new
            {
                x.Id, x.Code, x.DefaultName, x.Address, x.CoverImageUrl,
                x.CenterLatitude, x.CenterLongitude, x.IsActive,
                Translations = x.Translations.Select(t => new
                {
                    t.LanguageCode, t.Name, t.Description, t.WelcomeMessage
                }),
                Locations = x.Locations.OrderBy(l => l.DisplayOrder).Select(l => new
                {
                    l.Id, l.Code, l.DefaultName, l.Latitude, l.Longitude,
                    l.TriggerRadiusMeters, l.DisplayOrder, l.ImageUrl, l.IsActive,
                    Translations = l.Translations.Select(t => new
                    {
                        t.LanguageCode, t.Title, t.Description, t.NarrationText,
                        t.AudioUrl, t.DurationSeconds, t.NarratorName
                    })
                })
            }).FirstOrDefaultAsync(cancellationToken);
        return tour is null ? NotFound() : Ok(tour);
    }

    [HttpPost("tours")]
    public async Task<IActionResult> CreateTour(
        [FromBody] SaveTourRequest request, CancellationToken cancellationToken)
    {
        if (!IsLocalRequest()) return Forbid();
        var validation = ValidateTourRequest(request);
        if (validation is not null) return BadRequest(new { message = validation });
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Tours.AnyAsync(x => x.Code == code, cancellationToken))
            return Conflict(new { message = "Mã tour đã tồn tại." });

        var now = DateTimeOffset.UtcNow;
        var tour = new Tour
        {
            Id = Guid.NewGuid(), Code = code,
            DefaultName = request.DefaultName.Trim(),
            Address = request.Address?.Trim(), CoverImageUrl = request.CoverImageUrl?.Trim(),
            IsActive = request.IsActive, CreatedAt = now
        };
        _db.Tours.Add(tour);
        _db.TourTranslations.Add(new TourTranslation
        {
            Id = Guid.NewGuid(), TourId = tour.Id,
            LanguageCode = SupportedLanguageCodes.Normalize(request.LanguageCode),
            Name = request.Name.Trim(), Description = request.Description.Trim(),
            WelcomeMessage = request.WelcomeMessage?.Trim(), CreatedAt = now
        });
        await _db.SaveChangesAsync(cancellationToken);
        return CreatedAtAction(nameof(GetTour), new { id = tour.Id }, new { tour.Id });
    }

    [HttpPut("tours/{id:guid}")]
    public async Task<IActionResult> UpdateTour(
        Guid id, [FromBody] SaveTourRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsLocalRequest()) return Forbid();
        var validation = ValidateTourRequest(request);
        if (validation is not null) return BadRequest(new { message = validation });
        var tour = await _db.Tours.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (tour is null) return NotFound();
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.Tours.AnyAsync(x => x.Id != id && x.Code == code, cancellationToken))
            return Conflict(new { message = "Mã tour đã tồn tại." });

        tour.Code = code;
        tour.DefaultName = request.DefaultName.Trim();
        tour.Address = request.Address?.Trim();
        tour.CoverImageUrl = request.CoverImageUrl?.Trim();
        tour.IsActive = request.IsActive;
        tour.UpdatedAt = DateTimeOffset.UtcNow;
        var language = SupportedLanguageCodes.Normalize(request.LanguageCode);
        var translation = await _db.TourTranslations.FirstOrDefaultAsync(
            x => x.TourId == id && x.LanguageCode == language, cancellationToken);
        if (translation is null)
        {
            translation = new TourTranslation
            {
                Id = Guid.NewGuid(), TourId = id, LanguageCode = language,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.TourTranslations.Add(translation);
        }
        translation.Name = request.Name.Trim();
        translation.Description = request.Description.Trim();
        translation.WelcomeMessage = request.WelcomeMessage?.Trim();
        translation.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPost("tours/{tourId:guid}/locations")]
    public async Task<IActionResult> CreateLocation(
        Guid tourId, [FromBody] SaveLocationRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsLocalRequest()) return Forbid();
        var validation = ValidateLocationRequest(request);
        if (validation is not null) return BadRequest(new { message = validation });
        if (!await _db.Tours.AnyAsync(x => x.Id == tourId, cancellationToken))
            return NotFound(new { message = "Không tìm thấy tour." });
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.TourLocations.AnyAsync(
                x => x.TourId == tourId && (x.Code == code || x.DisplayOrder == request.DisplayOrder),
                cancellationToken))
            return Conflict(new { message = "Mã địa điểm hoặc thứ tự đã được sử dụng trong tour." });

        var now = DateTimeOffset.UtcNow;
        var location = new TourLocation
        {
            Id = Guid.NewGuid(), TourId = tourId, Code = code,
            DefaultName = request.DefaultName.Trim(), Latitude = request.Latitude,
            Longitude = request.Longitude, TriggerRadiusMeters = request.TriggerRadiusMeters,
            DisplayOrder = request.DisplayOrder, ImageUrl = request.ImageUrl?.Trim(),
            IsActive = request.IsActive, CreatedAt = now
        };
        _db.TourLocations.Add(location);
        _db.TourLocationTranslations.Add(new TourLocationTranslation
        {
            Id = Guid.NewGuid(), TourLocationId = location.Id,
            LanguageCode = SupportedLanguageCodes.Normalize(request.LanguageCode),
            Title = request.Title.Trim(), Description = request.Description.Trim(),
            NarrationText = request.NarrationText.Trim(), AudioUrl = request.AudioUrl?.Trim() ?? string.Empty,
            DurationSeconds = request.DurationSeconds, NarratorName = request.NarratorName?.Trim(),
            CreatedAt = now
        });
        await _db.SaveChangesAsync(cancellationToken);
        return Ok(new { location.Id });
    }

    [HttpPut("locations/{id:guid}")]
    public async Task<IActionResult> UpdateLocation(
        Guid id, [FromBody] SaveLocationRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsLocalRequest()) return Forbid();
        var validation = ValidateLocationRequest(request);
        if (validation is not null) return BadRequest(new { message = validation });
        var location = await _db.TourLocations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (location is null) return NotFound();
        var code = request.Code.Trim().ToUpperInvariant();
        if (await _db.TourLocations.AnyAsync(x => x.Id != id && x.TourId == location.TourId
                && (x.Code == code || x.DisplayOrder == request.DisplayOrder), cancellationToken))
            return Conflict(new { message = "Mã địa điểm hoặc thứ tự đã được sử dụng trong tour." });

        location.Code = code;
        location.DefaultName = request.DefaultName.Trim();
        location.Latitude = request.Latitude;
        location.Longitude = request.Longitude;
        location.TriggerRadiusMeters = request.TriggerRadiusMeters;
        location.DisplayOrder = request.DisplayOrder;
        location.ImageUrl = request.ImageUrl?.Trim();
        location.IsActive = request.IsActive;
        location.UpdatedAt = DateTimeOffset.UtcNow;
        var language = SupportedLanguageCodes.Normalize(request.LanguageCode);
        var translation = await _db.TourLocationTranslations.FirstOrDefaultAsync(
            x => x.TourLocationId == id && x.LanguageCode == language, cancellationToken);
        if (translation is null)
        {
            translation = new TourLocationTranslation
            {
                Id = Guid.NewGuid(), TourLocationId = id, LanguageCode = language,
                CreatedAt = DateTimeOffset.UtcNow
            };
            _db.TourLocationTranslations.Add(translation);
        }
        translation.Title = request.Title.Trim();
        translation.Description = request.Description.Trim();
        translation.NarrationText = request.NarrationText.Trim();
        translation.AudioUrl = request.AudioUrl?.Trim() ?? string.Empty;
        translation.DurationSeconds = request.DurationSeconds;
        translation.NarratorName = request.NarratorName?.Trim();
        translation.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpPatch("locations/{id:guid}/active")]
    public async Task<IActionResult> SetLocationActive(
        Guid id, [FromBody] SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsLocalRequest()) return Forbid();
        var location = await _db.TourLocations.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (location is null) return NotFound();
        location.IsActive = request.IsActive;
        location.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    private static string? ValidateTourRequest(SaveTourRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Trim().Length > 50)
            return "Mã tour bắt buộc và tối đa 50 ký tự.";
        if (string.IsNullOrWhiteSpace(request.DefaultName) || request.DefaultName.Trim().Length > 200)
            return "Tên tour bắt buộc và tối đa 200 ký tự.";
        if (string.IsNullOrWhiteSpace(request.Name) || request.Name.Trim().Length > 200)
            return "Tên bản dịch bắt buộc và tối đa 200 ký tự.";
        if (request.Description?.Length > 4000 || request.WelcomeMessage?.Length > 2000)
            return "Mô tả/lời chào vượt quá độ dài cho phép.";
        if (!SupportedLanguageCodes.IsSupported(request.LanguageCode))
            return "Ngôn ngữ không được hỗ trợ.";
        return null;
    }

    private static string? ValidateLocationRequest(SaveLocationRequest request)
    {
        if (string.IsNullOrWhiteSpace(request.Code) || request.Code.Trim().Length > 50)
            return "Mã địa điểm bắt buộc và tối đa 50 ký tự.";
        if (string.IsNullOrWhiteSpace(request.DefaultName) || request.DefaultName.Trim().Length > 200)
            return "Tên địa điểm bắt buộc và tối đa 200 ký tự.";
        if (string.IsNullOrWhiteSpace(request.Title) || request.Title.Trim().Length > 200)
            return "Tiêu đề bản dịch bắt buộc và tối đa 200 ký tự.";
        if (request.Latitude is < -90 or > 90 || request.Longitude is < -180 or > 180)
            return "Vĩ độ/kinh độ không hợp lệ.";
        if (request.TriggerRadiusMeters is <= 0 or > 500 || request.DisplayOrder <= 0)
            return "Bán kính phải từ 0 đến 500m và thứ tự phải lớn hơn 0.";
        if (request.DurationSeconds < 0 || request.Description?.Length > 4000
            || request.NarrationText?.Length > 10000 || request.AudioUrl?.Length > 2000)
            return "Nội dung, audio URL hoặc thời lượng không hợp lệ.";
        if (!SupportedLanguageCodes.IsSupported(request.LanguageCode))
            return "Ngôn ngữ không được hỗ trợ.";
        return null;
    }

    [HttpPatch("tours/{id:guid}/active")]
    public async Task<IActionResult> SetTourActive(
        Guid id, [FromBody] SetActiveRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsLocalRequest()) return Forbid();
        var tour = await _db.Tours.FirstOrDefaultAsync(x => x.Id == id, cancellationToken);
        if (tour is null) return NotFound();
        tour.IsActive = request.IsActive;
        tour.UpdatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("qrcodes")]
    public async Task<IActionResult> GetQrCodes(CancellationToken cancellationToken)
    {
        if (!IsLocalRequest()) return Forbid();
        var codes = await _db.TourQrCodes.AsNoTracking()
            .OrderByDescending(x => x.CreatedAt)
            .Select(x => new
            {
                x.Id, x.AccessCode, x.TourId, TourName = x.Tour.DefaultName,
                x.IsActive, x.UsageCount, x.MaximumUsageCount,
                x.CreatedAt, x.ExpiresAt, x.LastUsedAt
            }).ToListAsync(cancellationToken);
        return Ok(codes);
    }

    [HttpPost("qrcodes")]
    public async Task<IActionResult> CreateQrCode(
        [FromBody] CreateQrCodeRequest request,
        CancellationToken cancellationToken)
    {
        if (!IsLocalRequest()) return Forbid();
        if (request.ExpiresInDays is < 1 or > 3650)
            return BadRequest(new { message = "Thời hạn QR phải từ 1 đến 3650 ngày." });
        if (request.MaximumUsageCount is <= 0)
            return BadRequest(new { message = "Số lượt dùng tối đa phải lớn hơn 0." });

        var tour = await _db.Tours.FirstOrDefaultAsync(
            x => x.Id == request.TourId && x.IsActive, cancellationToken);
        if (tour is null)
            return BadRequest(new { message = "Tour không tồn tại hoặc đang bị tắt." });

        var now = DateTimeOffset.UtcNow;
        var qr = new TourQrCode
        {
            Id = Guid.NewGuid(),
            TourId = tour.Id,
            AccessCode = Convert.ToHexString(RandomNumberGenerator.GetBytes(8)),
            Type = QrCodeType.Reusable,
            IsActive = true,
            UsageCount = 0,
            MaximumUsageCount = request.MaximumUsageCount,
            CreatedAt = now,
            ExpiresAt = now.AddDays(request.ExpiresInDays)
        };
        _db.TourQrCodes.Add(qr);
        await _db.SaveChangesAsync(cancellationToken);
        return Created($"/api/admin/qrcodes/{qr.Id}", new
        {
            qr.Id, qr.AccessCode, qr.TourId, TourName = tour.DefaultName,
            qr.ExpiresAt, qr.MaximumUsageCount
        });
    }

    [HttpPatch("qrcodes/{id:guid}/deactivate")]
    public async Task<IActionResult> DeactivateQrCode(
        Guid id, CancellationToken cancellationToken)
    {
        if (!IsLocalRequest()) return Forbid();
        var qr = await _db.TourQrCodes.FirstOrDefaultAsync(
            x => x.Id == id, cancellationToken);
        if (qr is null) return NotFound();
        qr.IsActive = false;
        qr.DeactivatedAt = DateTimeOffset.UtcNow;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    [HttpGet("sessions")]
    public async Task<IActionResult> GetGuestSessions(CancellationToken cancellationToken)
    {
        if (!IsLocalRequest()) return Forbid();
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

    private bool IsLocalRequest()
    {
        var remoteIp = HttpContext.Connection.RemoteIpAddress;
        var host = Request.Host.Host;
        return remoteIp is not null
            && IPAddress.IsLoopback(remoteIp)
            && (string.Equals(host, "localhost", StringComparison.OrdinalIgnoreCase)
                || IPAddress.TryParse(host, out var address) && IPAddress.IsLoopback(address));
    }

    public sealed record SetActiveRequest(bool IsActive);
    public sealed record SaveTourRequest(
        string Code, string DefaultName, string LanguageCode, string Name,
        string Description, string? WelcomeMessage, string? Address,
        string? CoverImageUrl, bool IsActive = true);
    public sealed record SaveLocationRequest(
        string Code, string DefaultName, string LanguageCode, string Title,
        string Description, string NarrationText, string? AudioUrl,
        double DurationSeconds, string? NarratorName,
        double Latitude, double Longitude, double TriggerRadiusMeters,
        int DisplayOrder, string? ImageUrl, bool IsActive = true);
    public sealed record CreateQrCodeRequest(
        Guid TourId,
        int ExpiresInDays = 30,
        int? MaximumUsageCount = null);
}
