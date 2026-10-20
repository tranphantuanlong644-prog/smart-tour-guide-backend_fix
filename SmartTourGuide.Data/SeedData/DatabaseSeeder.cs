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

    private static readonly Guid
        VietnameseTourTranslationId =
            Guid.Parse(
                "31111111-1111-1111-1111-111111111111");

    private static readonly Guid
        EnglishTourTranslationId =
            Guid.Parse(
                "32222222-2222-2222-2222-222222222222");

    private static readonly Guid
        SimplifiedChineseTourTranslationId =
            Guid.Parse(
                "33333333-3333-3333-3333-333333333333");

    private static readonly Guid
        HongKongChineseTourTranslationId =
            Guid.Parse(
                "34444444-4444-4444-4444-444444444444");

    private static readonly Guid
        SpanishTourTranslationId =
            Guid.Parse(
                "35555555-5555-5555-5555-555555555555");

    public static async Task SeedAsync(
        SmartTourGuideDbContext dbContext,
        CancellationToken cancellationToken = default)
    {
        ArgumentNullException.ThrowIfNull(dbContext);

        await SeedTourAsync(
            dbContext,
            cancellationToken);

        // Lưu Tour trước vì Translation và Location
        // đều sử dụng TourId làm khóa ngoại.
        await dbContext.SaveChangesAsync(
            cancellationToken);

        await SeedDemoQrCodeAsync(
            dbContext,
            cancellationToken);

        await SeedTourTranslationsAsync(
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
                AccessCode = accessCode,
                Type = QrCodeType.Reusable,
                IsActive = true,
                CreatedAt = now,
                ExpiresAt = now.AddYears(10)
            },
            cancellationToken);
        await dbContext.SaveChangesAsync(cancellationToken);
    }

    private static async Task SeedTourAsync(
        SmartTourGuideDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var tourExists =
            await dbContext.Tours.AnyAsync(
                tour =>
                    tour.Id == VietNamQuocTuTourId,
                cancellationToken);

        if (tourExists)
        {
            return;
        }

        var now = DateTimeOffset.UtcNow;

        var tour =
            new Tour
            {
                Id = VietNamQuocTuTourId,
                Code = "VNTQ-MAIN-TOUR",
                DefaultName =
                    "Tham quan Việt Nam Quốc Tự",
                CoverImageUrl =
                    "/images/tours/vietnam-quoc-tu.jpg",
                Address =
                    "244 đường Ba Tháng Hai, " +
                    "Thành phố Hồ Chí Minh",
                CenterLatitude = null,
                CenterLongitude = null,
                IsActive = true,
                CreatedAt = now
            };

        await dbContext.Tours.AddAsync(
            tour,
            cancellationToken);
    }

    private static async Task SeedTourTranslationsAsync(
        SmartTourGuideDbContext dbContext,
        CancellationToken cancellationToken)
    {
        var existingLanguageCodes =
            await dbContext.TourTranslations
                .Where(
                    translation =>
                        translation.TourId ==
                            VietNamQuocTuTourId)
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
                CreateTourTranslation(
                    VietnameseTourTranslationId,
                    SupportedLanguageCodes.Vietnamese,
                    "Tham quan Việt Nam Quốc Tự",
                    "Hành trình tham quan giới thiệu " +
                    "các khu vực chính trong khuôn viên " +
                    "Việt Nam Quốc Tự.",
                    "Chào mừng bạn đến với hành trình " +
                    "tham quan Việt Nam Quốc Tự.",
                    now),

                CreateTourTranslation(
                    EnglishTourTranslationId,
                    SupportedLanguageCodes.English,
                    "Viet Nam Quoc Tu Guided Tour",
                    "A guided tour introducing the main " +
                    "areas within the Viet Nam Quoc Tu " +
                    "complex.",
                    "Welcome to the Viet Nam Quoc Tu " +
                    "guided tour.",
                    now),

                CreateTourTranslation(
                    SimplifiedChineseTourTranslationId,
                    SupportedLanguageCodes
                        .SimplifiedChinese,
                    "越南国寺导览",
                    "本导览介绍越南国寺范围内的主要参观区域。",
                    "欢迎参加越南国寺导览。",
                    now),

                CreateTourTranslation(
                    HongKongChineseTourTranslationId,
                    SupportedLanguageCodes
                        .TraditionalChineseHongKong,
                    "越南國寺導覽",
                    "本導覽介紹越南國寺範圍內的主要參觀區域。",
                    "歡迎參加越南國寺導覽。",
                    now),

                CreateTourTranslation(
                    SpanishTourTranslationId,
                    SupportedLanguageCodes.Spanish,
                    "Visita guiada a Viet Nam Quoc Tu",
                    "Un recorrido que presenta las " +
                    "principales áreas del complejo " +
                    "Viet Nam Quoc Tu.",
                    "Bienvenido a la visita guiada de " +
                    "Viet Nam Quoc Tu.",
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

        await dbContext.TourTranslations.AddRangeAsync(
            missingTranslations,
            cancellationToken);
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
                    displayOrder: 1,
                    createdAt: now),

                CreateLocation(
                    GeneralCampusLocationId,
                    "GENERAL-CAMPUS",
                    "Khuôn viên tổng quan",
                    displayOrder: 2,
                    createdAt: now),

                CreateLocation(
                    MainHallLocationId,
                    "MAIN-HALL",
                    "Chính điện",
                    displayOrder: 3,
                    createdAt: now),

                CreateLocation(
                    StupaLocationId,
                    "STUPA",
                    "Bảo tháp",
                    displayOrder: 4,
                    createdAt: now),

                CreateLocation(
                    HistoryAreaLocationId,
                    "HISTORY-AREA",
                    "Khu vực lịch sử",
                    displayOrder: 5,
                    createdAt: now)
            };

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

    private static TourTranslation CreateTourTranslation(
        Guid id,
        string languageCode,
        string name,
        string description,
        string welcomeMessage,
        DateTimeOffset createdAt)
    {
        return new TourTranslation
        {
            Id = id,
            TourId = VietNamQuocTuTourId,
            LanguageCode =
                SupportedLanguageCodes.Normalize(
                    languageCode),
            Name = name,
            Description = description,
            WelcomeMessage = welcomeMessage,
            CreatedAt = createdAt
        };
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
        int displayOrder,
        DateTimeOffset createdAt)
    {
        return new TourLocation
        {
            Id = id,
            TourId = VietNamQuocTuTourId,
            Code = code,
            DefaultName = defaultName,

            // Chỉ là tọa độ placeholder kỹ thuật.
            // Cần thay bằng tọa độ khảo sát thực tế.
            Latitude = 0,
            Longitude = 0,

            TriggerRadiusMeters =
                GpsDefaults.TriggerRadiusMeters,

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
