using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTourGuide.Data.Context;
using SmartTourGuide.Data.Entities;
using SmartTourGuide.Shared.Constants;
using SmartTourGuide.Shared.Enums;

namespace SmartTourGuide.API.Controllers;

[ApiController]
[AllowAnonymous]
[Route("api/public/tours")]
public sealed class PublicToursController : ControllerBase
{
    private readonly SmartTourGuideDbContext _db;
    private readonly IWebHostEnvironment _environment;

    public PublicToursController(
        SmartTourGuideDbContext db,
        IWebHostEnvironment environment)
    {
        _db = db;
        _environment = environment;
    }


    [HttpGet("qr/{accessCode}")]
    public async Task<IActionResult> GetByQrCode(
        string accessCode,
        [FromQuery] string language = "vi",
        [FromQuery] string? deviceId = null,
        CancellationToken cancellationToken = default)
    {
        if (!SupportedLanguageCodes.IsSupported(language))
        {
            return BadRequest(new
            {
                message = "Ngôn ngữ không được hỗ trợ."
            });
        }

        var normalizedLanguage = SupportedLanguageCodes.Normalize(language);

        if (string.IsNullOrWhiteSpace(deviceId) || deviceId.Length > 200)
        {
            deviceId = Guid.NewGuid().ToString("N");
        }

        var strategy = _db.Database.CreateExecutionStrategy();

        return await strategy.ExecuteAsync<IActionResult>(async () =>
        {
            var now = DateTimeOffset.UtcNow;

            var qr = await _db.TourQrCodes
                .AsNoTracking()
                .Where(x => x.AccessCode == accessCode
                    && x.IsActive
                    && x.ExpiresAt > now
                    && (!x.MaximumUsageCount.HasValue
                        || x.UsageCount < x.MaximumUsageCount.Value))
                .FirstOrDefaultAsync(cancellationToken);

            if (qr is null)
            {
                return NotFound(new
                {
                    message = "Mã QR không hợp lệ, đã hết hạn hoặc tour đã ngừng hoạt động."
                });
            }

            var tourEntity = await _db.Tours
                .AsNoTracking()
                .Include(x => x.Translations)
                .Include(x => x.Locations.Where(l => l.IsActive))
                    .ThenInclude(l => l.Translations)
                .AsSplitQuery()
                .FirstOrDefaultAsync(
                    x => x.Id == qr.TourId && x.IsActive,
                    cancellationToken);

            if (tourEntity is null)
            {
                return NotFound(new
                {
                    message = "Mã QR không hợp lệ, đã hết hạn hoặc tour đã ngừng hoạt động."
                });
            }

            var selectedTourTranslation = tourEntity.Translations
                .FirstOrDefault(t => t.LanguageCode == normalizedLanguage);

            var tour = new
            {
                tourEntity.Id,
                tourEntity.Code,
                Name = selectedTourTranslation?.Name ?? tourEntity.DefaultName,
                Description = selectedTourTranslation?.Description,
                WelcomeMessage = selectedTourTranslation?.WelcomeMessage,
                tourEntity.CoverImageUrl,
                tourEntity.Address,

                Translations = tourEntity.Translations.Select(t => new
                {
                    t.LanguageCode,
                    t.Name,
                    t.Description,
                    t.WelcomeMessage
                }).ToList(),

                Locations = tourEntity.Locations
                    .OrderBy(l => l.DisplayOrder)
                    .Select(l =>
                    {
                        var selectedTranslation = l.Translations
                            .FirstOrDefault(
                                t => t.LanguageCode == normalizedLanguage);

                        return new
                        {
                            l.Id,
                            l.Code,
                            Name = selectedTranslation?.Title ?? l.DefaultName,
                            Description = selectedTranslation?.Description,
                            NarrationText = selectedTranslation?.NarrationText,
                            AudioUrl = GetAvailableAudioUrl(
                                selectedTranslation?.AudioUrl),
                            l.ImageUrl,
                            l.DisplayOrder,
                            l.Latitude,
                            l.Longitude,
                            l.TriggerRadiusMeters,

                            Translations = l.Translations.Select(t => new
                            {
                                t.LanguageCode,
                                t.Title,
                                t.Description,
                                t.NarrationText,
                                AudioUrl = GetAvailableAudioUrl(t.AudioUrl)
                            }).ToList()
                        };
                    }).ToList()
            };

            await using var transaction =
                await _db.Database.BeginTransactionAsync(cancellationToken);

            try
            {
                var guest = await _db.GuestIdentities
                    .FirstOrDefaultAsync(
                        x => x.DeviceId == deviceId
                            && x.IsActive
                            && x.ExpiresAt > now,
                        cancellationToken);

                if (guest is null)
                {
                    guest = new GuestIdentity
                    {
                        Id = Guid.NewGuid(),
                        DisplayName = "Khách",
                        PreferredLanguageCode = normalizedLanguage,
                        DeviceId = deviceId!,
                        IsActive = true,
                        CreatedAt = now,
                        ExpiresAt = now.AddDays(30),
                        LastAccessedAt = now
                    };

                    _db.GuestIdentities.Add(guest);
                }
                else
                {
                    guest.PreferredLanguageCode = normalizedLanguage;
                    guest.LastAccessedAt = now;
                }

                var session = await _db.TourSessions
                    .FirstOrDefaultAsync(
                        x => x.GuestIdentityId == guest.Id
                            && x.TourId == tourEntity.Id
                            && x.Status == TourSessionStatus.Active
                            && x.ExpiresAt > now,
                        cancellationToken);

                if (session is null)
                {
                    session = new TourSession
                    {
                        Id = Guid.NewGuid(),
                        TourId = tourEntity.Id,
                        IdentityType = IdentityType.Guest,
                        GuestIdentityId = guest.Id,
                        SourceQrCodeId = qr.Id,
                        LanguageCode = normalizedLanguage,
                        Status = TourSessionStatus.Active,
                        StartedAt = now,
                        ExpiresAt = now.AddHours(4),
                        LastActivityAt = now
                    };

                    _db.TourSessions.Add(session);
                }
                else
                {
                    session.LanguageCode = normalizedLanguage;
                    session.LastActivityAt = now;
                    session.SourceQrCodeId = qr.Id;
                }

                // Kiểm tra và tăng lượt dùng QR bằng một câu lệnh SQL có điều kiện.
                var claimedQrCodes = await _db.TourQrCodes
                    .Where(x => x.Id == qr.Id
                        && x.IsActive
                        && x.ExpiresAt > now
                        && (!x.MaximumUsageCount.HasValue
                            || x.UsageCount < x.MaximumUsageCount.Value))
                    .ExecuteUpdateAsync(setters => setters
                        .SetProperty(
                            x => x.UsageCount,
                            x => x.UsageCount + 1)
                        .SetProperty(x => x.LastUsedAt, now)
                        .SetProperty(
                            x => x.IsActive,
                            x => x.Type != QrCodeType.SingleUse)
                        .SetProperty(
                            x => x.UsedAt,
                            x => x.Type == QrCodeType.SingleUse
                                ? now
                                : x.UsedAt)
                        .SetProperty(
                            x => x.DeactivatedAt,
                            x => x.Type == QrCodeType.SingleUse
                                ? now
                                : x.DeactivatedAt),
                        cancellationToken);

                if (claimedQrCodes == 0)
                {
                    await transaction.RollbackAsync(cancellationToken);

                    return NotFound(new
                    {
                        message = "Mã QR không hợp lệ, đã hết hạn hoặc tour đã ngừng hoạt động."
                    });
                }

                await _db.SaveChangesAsync(cancellationToken);
                await transaction.CommitAsync(cancellationToken);

                return Ok(new
                {
                    qrCodeId = qr.Id,
                    sessionId = session.Id,
                    language = normalizedLanguage,
                    tour
                });
            }
            catch
            {
                await transaction.RollbackAsync(CancellationToken.None);
                throw;
            }
        });
    }


    [HttpPost("sessions/{sessionId:guid}/end")]
    public async Task<IActionResult> EndGuestSession(
        Guid sessionId, [FromBody] GuestSessionRequest request,
        CancellationToken cancellationToken)
    {
        if (string.IsNullOrWhiteSpace(request.DeviceId))
            return BadRequest(new { message = "Thiếu mã thiết bị." });

        var session = await _db.TourSessions
            .Include(x => x.GuestIdentity)
            .FirstOrDefaultAsync(x => x.Id == sessionId
                && x.IdentityType == IdentityType.Guest
                && x.Status == TourSessionStatus.Active
                && x.GuestIdentity != null
                && x.GuestIdentity.DeviceId == request.DeviceId,
                cancellationToken);
        if (session is null) return NoContent();

        var now = DateTimeOffset.UtcNow;
        session.Status = TourSessionStatus.Completed;
        session.EndedAt = now;
        session.LastActivityAt = now;
        await _db.SaveChangesAsync(cancellationToken);
        return NoContent();
    }

    public sealed record GuestSessionRequest(string DeviceId);

    private string? GetAvailableAudioUrl(string? audioUrl)
    {
        if (string.IsNullOrWhiteSpace(audioUrl))
            return null;

        var normalizedUrl = audioUrl.Trim().Replace('\\', '/');

        // Only serve local files under wwwroot/audio. This prevents returning
        // stale seed URLs or arbitrary paths as usable media links.
        if (!normalizedUrl.StartsWith("/audio/", StringComparison.OrdinalIgnoreCase))
            return null;

        var fileName = Path.GetFileName(normalizedUrl);
        if (string.IsNullOrWhiteSpace(fileName))
            return null;

        var webRootPath = _environment.WebRootPath;
        if (string.IsNullOrWhiteSpace(webRootPath))
            return null;

        var physicalPath = Path.Combine(webRootPath, "audio", fileName);
        return System.IO.File.Exists(physicalPath) ? normalizedUrl : null;
    }

}
