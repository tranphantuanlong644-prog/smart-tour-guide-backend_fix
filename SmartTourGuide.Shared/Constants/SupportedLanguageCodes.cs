namespace SmartTourGuide.Shared.Constants;

public static class SupportedLanguageCodes
{
    public const string Vietnamese =
        "vi";

    public const string English =
        "en";

    public const string SimplifiedChinese =
        "zh-CN";

    public const string TraditionalChineseHongKong =
        "zh-HK";

    public const string Spanish =
        "es";

    public static readonly IReadOnlySet<string>
        All =
        new HashSet<string>(
            StringComparer.OrdinalIgnoreCase)
        {
            Vietnamese,
            English,
            SimplifiedChinese,
            TraditionalChineseHongKong,
            Spanish
        };

    public static bool IsSupported(
        string? languageCode)
    {
        return
            !string.IsNullOrWhiteSpace(
                languageCode) &&
            All.Contains(languageCode);
    }

    public static string Normalize(
        string languageCode)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(
            languageCode);

        var matchedLanguageCode =
            All.FirstOrDefault(
                supportedLanguageCode =>
                    string.Equals(
                        supportedLanguageCode,
                        languageCode.Trim(),
                        StringComparison
                            .OrdinalIgnoreCase));

        if (matchedLanguageCode is null)
        {
            throw new ArgumentException(
                "The language code is not supported.",
                nameof(languageCode));
        }

        return matchedLanguageCode;
    }
}