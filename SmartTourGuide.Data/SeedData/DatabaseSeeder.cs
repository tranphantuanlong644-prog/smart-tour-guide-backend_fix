using Microsoft.EntityFrameworkCore;
using SmartTourGuide.Data.Context;
using SmartTourGuide.Data.Entities;
using SmartTourGuide.Shared.Constants;
using SmartTourGuide.Shared.Enums;

namespace SmartTourGuide.Data.SeedData;

public static class DatabaseSeeder
{
    public static readonly Guid VietNamQuocTuTourId =
        Guid.Parse(
            "11111111-1111-1111-1111-111111111111");

    public static readonly Guid MainGateLocationId =
        Guid.Parse(
            "21111111-1111-1111-1111-111111111111");

    public static readonly Guid GeneralCampusLocationId =
        Guid.Parse(
            "22222222-2222-2222-2222-222222222222");

    public static readonly Guid MainHallLocationId =
        Guid.Parse(
            "23333333-3333-3333-3333-333333333333");

    public static readonly Guid StupaLocationId =
        Guid.Parse(
            "24444444-4444-4444-4444-444444444444");

    public static readonly Guid HistoryAreaLocationId =
        Guid.Parse(
            "25555555-5555-5555-5555-555555555555");

    public static async Task SeedAsync(
        SmartTourGuideDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        await SeedDemoQrCodeAsync(
            dbContext,
            cancellationToken);

        await SeedLocationsAsync(
            dbContext,
            cancellationToken);

        // Lưu các địa điểm trước khi tạo
        // TourLocationTranslation.
        await dbContext.SaveChangesAsync(
            cancellationToken);

        await SeedLocationTranslationsAsync(
            dbContext,
            cancellationToken);

        await dbContext.SaveChangesAsync(
            cancellationToken);
    }

    private static async Task SeedDemoQrCodeAsync(
        SmartTourGuideDbContext dbContext,
        CancellationToken cancellationToken)
    {
        const string accessCode = "VNTQ-DEMO-2026";
        if (await dbContext.TourQrCodes.AnyAsync(
                qr => qr.AccessCode == accessCode,
                cancellationToken))
            return;

        var now = DateTimeOffset.UtcNow;
        await dbContext.TourQrCodes.AddAsync(
            new TourQrCode
            {
                Id = Guid.Parse("41111111-1111-1111-1111-111111111111"),
                TourId = VietNamQuocTuTourId,
                TourCode = "VNTQ-MAIN-TOUR",
                TourName = "Tham quan Việt Nam Quốc Tự",
                TourDescription = "Hành trình tham quan giới thiệu các khu vực chính trong khuôn viên Việt Nam Quốc Tự.",
                WelcomeMessage = "Chào mừng bạn đến với hành trình tham quan Việt Nam Quốc Tự.",
                CoverImageUrl = "/images/tours/vietnam-quoc-tu.jpg",
                Address = "244 đường Ba Tháng Hai, Thành phố Hồ Chí Minh",
                AccessCode = accessCode,
                Type = QrCodeType.Reusable,
                IsActive = true,
                CreatedAt = now,
                ExpiresAt = now.AddYears(10)
            },
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedLocationsAsync(
        SmartTourGuideDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var existingLocationIds =
            await dbContext.TourLocations
                .Where(
                    location =>
                        location.TourId ==
                            VietNamQuocTuTourId)
                .Select(location => location.Id)
                .ToListAsync(cancellationToken);

        var existingLocationIdSet =
            existingLocationIds.ToHashSet();

        var now = DateTimeOffset.UtcNow;

        var locations =
            new[]
            {
                CreateLocation(
                    MainGateLocationId,
                    "MAIN-GATE",
                    "Cổng chính",
                    latitude: 10.77160,
                    longitude: 106.67352,
                    displayOrder: 1,
                    createdAt: now),

                CreateLocation(
                    GeneralCampusLocationId,
                    "GENERAL-CAMPUS",
                    "Khuôn viên tổng quan",
                    latitude: 10.77182,
                    longitude: 106.67343,
                    displayOrder: 2,
                    createdAt: now),

                CreateLocation(
                    MainHallLocationId,
                    "MAIN-HALL",
                    "Chính điện",
                    latitude: 10.77195,
                    longitude: 106.67343,
                    displayOrder: 3,
                    createdAt: now),

                CreateLocation(
                    StupaLocationId,
                    "STUPA",
                    "Bảo tháp",
                    latitude: 10.77182,
                    longitude: 106.67360,
                    displayOrder: 4,
                    createdAt: now),

                CreateLocation(
                    HistoryAreaLocationId,
                    "HISTORY-AREA",
                    "Khu vực lịch sử",
                    latitude: 10.77169,
                    longitude: 106.67331,
                    displayOrder: 5,
                    createdAt: now)
            };

        // Older demo databases contain (0, 0) placeholder coordinates.
        // Replace those only; preserve any coordinates an admin has already set.
        var seededIds = locations.Select(x => x.Id).ToArray();
        var existingSeededLocations = await dbContext.TourLocations
            .Where(x => seededIds.Contains(x.Id))
            .ToListAsync(cancellationToken);
        foreach (var existing in existingSeededLocations.Where(x => x.Latitude == 0 && x.Longitude == 0))
        {
            var definition = locations.First(x => x.Id == existing.Id);
            existing.Latitude = definition.Latitude;
            existing.Longitude = definition.Longitude;
            existing.TriggerRadiusMeters = 12;
            existing.UpdatedAt = now;
        }

        var missingLocations =
            locations
                .Where(
                    location =>
                        !existingLocationIdSet.Contains(
                            location.Id))
                .ToArray();

        if (missingLocations.Length == 0)
        {
            return;
        }

        await dbContext.TourLocations.AddRangeAsync(
            missingLocations,
            cancellationToken);
    }

    private static async Task
        SeedLocationTranslationsAsync(
            SmartTourGuideDbContext dbContext,
            CancellationToken cancellationToken)
    {
        var definitions =
            new[]
            {
                new LocationTranslationDefinition(
                    MainGateLocationId,
                    "main-gate",
                    "Cổng chính",
                    "Main Gate",
                    "正门",
                    "正門",
                    "Entrada principal"),

                new LocationTranslationDefinition(
                    GeneralCampusLocationId,
                    "general-campus",
                    "Khuôn viên tổng quan",
                    "General Campus",
                    "园区概览",
                    "園區概覽",
                    "Vista general del recinto"),

                new LocationTranslationDefinition(
                    MainHallLocationId,
                    "main-hall",
                    "Chính điện",
                    "Main Hall",
                    "主殿",
                    "主殿",
                    "Salón principal"),

                new LocationTranslationDefinition(
                    StupaLocationId,
                    "stupa",
                    "Bảo tháp",
                    "Stupa",
                    "佛塔",
                    "佛塔",
                    "Estupa"),

                new LocationTranslationDefinition(
                    HistoryAreaLocationId,
                    "history-area",
                    "Khu vực lịch sử",
                    "Historical Area",
                    "历史区域",
                    "歷史區域",
                    "Área histórica")
            };

        foreach (var definition in definitions)
        {
            await SeedLocationTranslationsForLocationAsync(
                dbContext,
                definition,
                cancellationToken);
        }
    }

    private static async Task
        SeedLocationTranslationsForLocationAsync(
            SmartTourGuideDbContext dbContext,
            LocationTranslationDefinition definition,
            CancellationToken cancellationToken)
    {
        var existingLanguageCodes =
            await dbContext.TourLocationTranslations
                .Where(
                    translation =>
                        translation.TourLocationId ==
                            definition.LocationId)
                .Select(
                    translation =>
                        translation.LanguageCode)
                .ToListAsync(cancellationToken);

        var existingLanguageCodeSet =
            existingLanguageCodes.ToHashSet(
                StringComparer.OrdinalIgnoreCase);

        var now = DateTimeOffset.UtcNow;

        var translations =
            new[]
            {
                CreateLocationTranslation(
                    definition.LocationId,
                    definition.AudioFilePrefix,
                    SupportedLanguageCodes.Vietnamese,
                    definition.VietnameseTitle,
                    "Nội dung giới thiệu ngắn về " +
                    definition.VietnameseTitle + ".",
                    "Đây là nội dung thuyết minh thử nghiệm " +
                    "cho khu vực " +
                    definition.VietnameseTitle +
                    ". Nội dung chính thức sẽ được cập nhật " +
                    "sau khi được kiểm chứng.",
                    "Giọng đọc tiếng Việt",
                    now),

                CreateLocationTranslation(
                    definition.LocationId,
                    definition.AudioFilePrefix,
                    SupportedLanguageCodes.English,
                    definition.EnglishTitle,
                    "A short introduction to the " +
                    definition.EnglishTitle + ".",
                    "This is a demonstration narration " +
                    "for the " +
                    definition.EnglishTitle +
                    ". The official content will be " +
                    "updated after verification.",
                    "English narrator",
                    now),

                CreateLocationTranslation(
                    definition.LocationId,
                    definition.AudioFilePrefix,
                    SupportedLanguageCodes
                        .SimplifiedChinese,
                    definition.SimplifiedChineseTitle,
                    "关于" +
                    definition.SimplifiedChineseTitle +
                    "的简短介绍。",
                    "这是" +
                    definition.SimplifiedChineseTitle +
                    "的演示讲解内容。" +
                    "正式内容将在核实后更新。",
                    "普通话讲解",
                    now),

                CreateLocationTranslation(
                    definition.LocationId,
                    definition.AudioFilePrefix,
                    SupportedLanguageCodes
                        .TraditionalChineseHongKong,
                    definition.TraditionalChineseTitle,
                    "關於" +
                    definition.TraditionalChineseTitle +
                    "的簡短介紹。",
                    "這是" +
                    definition.TraditionalChineseTitle +
                    "的示範導覽內容。" +
                    "正式內容將在核實後更新。",
                    "粵語導覽",
                    now),

                CreateLocationTranslation(
                    definition.LocationId,
                    definition.AudioFilePrefix,
                    SupportedLanguageCodes.Spanish,
                    definition.SpanishTitle,
                    "Una breve introducción a " +
                    definition.SpanishTitle + ".",
                    "Esta es una narración de " +
                    "demostración para " +
                    definition.SpanishTitle +
                    ". El contenido oficial se " +
                    "actualizará después de su " +
                    "verificación.",
                    "Narración en español",
                    now)
            };

        var missingTranslations =
            translations
                .Where(
                    translation =>
                        !existingLanguageCodeSet.Contains(
                            translation.LanguageCode))
                .ToArray();

        if (missingTranslations.Length == 0)
        {
            return;
        }

        await dbContext.TourLocationTranslations
            .AddRangeAsync(
                missingTranslations,
                cancellationToken);
    }

    private static TourLocationTranslation
        CreateLocationTranslation(
            Guid tourLocationId,
            string audioFilePrefix,
            string languageCode,
            string title,
            string description,
            string narrationText,
            string narratorName,
            DateTimeOffset createdAt)
    {
        var normalizedLanguageCode =
            SupportedLanguageCodes.Normalize(
                languageCode);

        var audioLanguageCode =
            normalizedLanguageCode
                .ToLowerInvariant();

        return new TourLocationTranslation
        {
            Id = Guid.NewGuid(),
            TourLocationId = tourLocationId,
            LanguageCode = normalizedLanguageCode,
            Title = title,
            Description = description,
            NarrationText = narrationText,
            AudioUrl =
                $"/audio/{audioFilePrefix}-" +
                $"{audioLanguageCode}.mp3",

            // Chỉ là thời lượng demo.
            // Cần thay bằng thời lượng MP3 thật.
            DurationSeconds = 60,

            NarratorName = narratorName,
            CreatedAt = createdAt
        };
    }

    private static TourLocation CreateLocation(
        Guid id,
        string code,
        string defaultName,
        double latitude,
        double longitude,
        int displayOrder,
        DateTimeOffset createdAt)
    {
        return new TourLocation
        {
            Id = id,
            TourId = VietNamQuocTuTourId,
            Code = code,
            DefaultName = defaultName,

            Latitude = latitude,
            Longitude = longitude,

            TriggerRadiusMeters = 12,

            DisplayOrder = displayOrder,
            ImageUrl =
                "/images/locations/" +
                $"{code.ToLowerInvariant()}.jpg",

            IsActive = true,
            CreatedAt = createdAt
        };
    }

    private sealed record LocationTranslationDefinition(
        Guid LocationId,
        string AudioFilePrefix,
        string VietnameseTitle,
        string EnglishTitle,
        string SimplifiedChineseTitle,
        string TraditionalChineseTitle,
        string SpanishTitle);
}
