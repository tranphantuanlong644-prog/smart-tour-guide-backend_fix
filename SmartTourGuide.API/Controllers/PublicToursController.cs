using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Hosting;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using SmartTourGuide.Data.Context;
using SmartTourGuide.Shared.Constants;

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
        CancellationToken cancellationToken = default)
    {
        if (!SupportedLanguageCodes.IsSupported(language))
            return BadRequest(new { message = "Ngôn ngữ không được hỗ trợ." });

        var now = DateTimeOffset.UtcNow;
        var qr = await _db.TourQrCodes
            .Where(x => x.AccessCode == accessCode && x.IsActive && x.ExpiresAt > now
                && (!x.MaximumUsageCount.HasValue || x.UsageCount < x.MaximumUsageCount.Value))
            .FirstOrDefaultAsync(cancellationToken);

        if (qr is null)
            return NotFound(new { message = "Mã QR không hợp lệ, đã hết hạn hoặc tour đã ngừng hoạt động." });

        if (!await _db.Tours.AnyAsync(x => x.Id == qr.TourId && x.IsActive, cancellationToken))
            return NotFound(new { message = "Mã QR không hợp lệ, đã hết hạn hoặc tour đã ngừng hoạt động." });

        var normalizedLanguage = SupportedLanguageCodes.Normalize(language);
        var tourEntity = await _db.Tours
            .AsNoTracking()
            .Include(x => x.Translations)
            .Include(x => x.Locations.Where(l => l.IsActive))
                .ThenInclude(l => l.Translations)
            .AsSplitQuery()
            .FirstOrDefaultAsync(x => x.Id == qr.TourId && x.IsActive, cancellationToken);

        if (tourEntity is null)
            return NotFound(new { message = "Không tìm thấy nội dung thuyết minh." });

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
                        .FirstOrDefault(t => t.LanguageCode == normalizedLanguage);

                    return new
                    {
                        l.Id,
                        l.Code,
                        Name = selectedTranslation?.Title ?? l.DefaultName,
                        Description = selectedTranslation?.Description,
                        NarrationText = selectedTranslation?.NarrationText,
                        AudioUrl = GetAvailableAudioUrl(selectedTranslation?.AudioUrl),
                        l.ImageUrl,
                        l.DisplayOrder,
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

        // Consume the QR only after we have loaded a valid tour response. The response
        // contains all public translations, so changing language on this page is local
        // and does not request the already-consumed SingleUse code again.
        qr.UsageCount++;
        qr.LastUsedAt = now;
        if (qr.Type == SmartTourGuide.Shared.Enums.QrCodeType.SingleUse)
        {
            qr.IsActive = false;
            qr.UsedAt = now;
            qr.DeactivatedAt = now;
        }
        await _db.SaveChangesAsync(cancellationToken);

        return Ok(new { qrCodeId = qr.Id, language = normalizedLanguage, tour });
    }
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
