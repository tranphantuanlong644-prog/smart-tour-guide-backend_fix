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
                    new("Cổng chính", "Cổng chính dẫn vào sân chùa Việt Nam Quốc Tự, nơi du khách bắt đầu hành trình tham quan."),
                    new("Main Gate", "The main gate leads into the courtyard of Việt Nam Quốc Tự, where the visit begins."),
                    new("正门", "正门通往越南国寺的庭院，是参观的起点。"),
                    new("正門", "正門通往越南國寺嘅庭院，係參觀嘅起點。"),
                    new("Entrada principal", "La entrada principal da acceso al patio de Việt Nam Quốc Tự, donde comienza la visita.")),

                new LocationTranslationDefinition(
                    GeneralCampusLocationId,
                    "general-campus",
                    new("Khuôn viên tổng quan", "Từ sân chùa, du khách có thể nhìn bao quát khuôn viên trước khi đi tiếp đến chính điện hoặc bảo tháp."),
                    new("General Campus", "The courtyard offers a view of the temple grounds before visitors continue to the main hall or the stupa."),
                    new("园区概览", "从庭院可以环顾寺院，再前往主殿或佛塔。"),
                    new("園區概覽", "喺庭院可以望到寺院全貌，再行去主殿或者佛塔。"),
                    new("Vista general del recinto", "Desde el patio se aprecia el conjunto del templo antes de continuar hacia el salón principal o la estupa.")),

                new LocationTranslationDefinition(
                    MainHallLocationId,
                    "main-hall",
                    new("Chính điện", "Chính điện là nơi thờ Phật và cử hành các nghi lễ chính của chùa."),
                    new("Main Hall", "The main hall is where people pay homage to the Buddha and gather for the main ceremonies of the temple."),
                    new("主殿", "主殿是礼佛和举行寺院主要仪式的地方。"),
                    new("主殿", "主殿係善信禮佛同寺院舉行主要儀式嘅地方。"),
                    new("Salón principal", "En el salón principal se venera a Buda y se celebran las principales ceremonias del templo.")),

                new LocationTranslationDefinition(
                    StupaLocationId,
                    "stupa",
                    new("Bảo tháp", "Bảo tháp nổi bật giữa khuôn viên chùa với các tầng tháp thu nhỏ dần về phía đỉnh."),
                    new("Stupa", "The stupa stands out on the temple grounds, with tiers that narrow towards the top."),
                    new("佛塔", "佛塔在寺院中格外醒目，塔身层层向上收窄。"),
                    new("佛塔", "佛塔喺寺院入面十分搶眼，塔身一層層向塔頂收窄。"),
                    new("Estupa", "La estupa destaca en el recinto por sus niveles superpuestos, que se estrechan hacia la cima.")),

                new LocationTranslationDefinition(
                    HistoryAreaLocationId,
                    "history-area",
                    new("Khu vực lịch sử", "Khu vực lịch sử giới thiệu quá trình hình thành và phát triển của Việt Nam Quốc Tự."),
                    new("Historical Area", "This area introduces the origins of Việt Nam Quốc Tự and how the temple has developed over time."),
                    new("历史区域", "这里介绍越南国寺的由来和发展历程。"),
                    new("歷史區域", "呢度介紹越南國寺嘅由來同發展歷程。"),
                    new("Área histórica", "Esta zona presenta los orígenes de Việt Nam Quốc Tự y la evolución del templo a lo largo del tiempo."))
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
        var existingTranslations =
            await dbContext.TourLocationTranslations
                .Where(
                    translation =>
                        translation.TourLocationId ==
                            definition.LocationId)
                .ToListAsync(cancellationToken);

        var byLanguage = existingTranslations.ToDictionary(x => x.LanguageCode, StringComparer.OrdinalIgnoreCase);

        var now = DateTimeOffset.UtcNow;

        var translations =
            new[]
            {
                CreateLocationTranslation(
                    definition.LocationId,
                    definition.AudioFilePrefix,
                    SupportedLanguageCodes.Vietnamese,
                    definition.Vietnamese.Title,
                    definition.Vietnamese.Description,
                    "Đây là nội dung thuyết minh thử nghiệm " +
                    "cho khu vực " +
                    definition.Vietnamese.Title +
                    ". Nội dung chính thức sẽ được cập nhật " +
                    "sau khi được kiểm chứng.",
                    "Giọng đọc tiếng Việt",
                    now),

                CreateLocationTranslation(
                    definition.LocationId,
                    definition.AudioFilePrefix,
                    SupportedLanguageCodes.English,
                    definition.English.Title,
                    definition.English.Description,
                    "This is a demonstration narration " +
                    "for the " +
                    definition.English.Title +
                    ". The official content will be " +
                    "updated after verification.",
                    "English narrator",
                    now),

                CreateLocationTranslation(
                    definition.LocationId,
                    definition.AudioFilePrefix,
                    SupportedLanguageCodes
                        .SimplifiedChinese,
                    definition.SimplifiedChinese.Title,
                    definition.SimplifiedChinese.Description,
                    "这是" +
                    definition.SimplifiedChinese.Title +
                    "的演示讲解内容。" +
                    "正式内容将在核实后更新。",
                    "普通话讲解",
                    now),

                CreateLocationTranslation(
                    definition.LocationId,
                    definition.AudioFilePrefix,
                    SupportedLanguageCodes
                        .TraditionalChineseHongKong,
                    definition.TraditionalChinese.Title,
                    definition.TraditionalChinese.Description,
                    "這是" +
                    definition.TraditionalChinese.Title +
                    "的示範導覽內容。" +
                    "正式內容將在核實後更新。",
                    "粵語導覽",
                    now),

                CreateLocationTranslation(
                    definition.LocationId,
                    definition.AudioFilePrefix,
                    SupportedLanguageCodes.Spanish,
                    definition.Spanish.Title,
                    definition.Spanish.Description,
                    "Esta es una narración de " +
                    "demostración para " +
                    definition.Spanish.Title +
                    ". El contenido oficial se " +
                    "actualizará después de su " +
                    "verificación.",
                    "Narración en español",
                    now)
            };

        foreach (var translation in translations)
        {
            if (!byLanguage.TryGetValue(translation.LanguageCode, out var existing))
            {
                dbContext.TourLocationTranslations.Add(translation);
                continue;
            }

            // Only replace the original demo description on existing installations.
            var placeholder = translation.LanguageCode switch
            {
                SupportedLanguageCodes.Vietnamese => $"Nội dung giới thiệu ngắn về {translation.Title}.",
                SupportedLanguageCodes.English => $"A short introduction to the {translation.Title}.",
                SupportedLanguageCodes.SimplifiedChinese => $"关于{translation.Title}的简短介绍。",
                SupportedLanguageCodes.TraditionalChineseHongKong => $"關於{translation.Title}的簡短介紹。",
                SupportedLanguageCodes.Spanish => $"Una breve introducción a {translation.Title}.",
                _ => null
            };
            if (existing.Description == placeholder)
            {
                existing.Description = translation.Description;
                existing.UpdatedAt = now;
            }
        }
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

    private sealed record LocalizedIntroduction(string Title, string Description);

    private sealed record LocationTranslationDefinition(
        Guid LocationId,
        string AudioFilePrefix,
        LocalizedIntroduction Vietnamese,
        LocalizedIntroduction English,
        LocalizedIntroduction SimplifiedChinese,
        LocalizedIntroduction TraditionalChinese,
        LocalizedIntroduction Spanish);
}
