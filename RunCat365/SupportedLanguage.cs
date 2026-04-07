using System.Globalization;

namespace RunCat365
{
    internal enum SupportedLanguage
    {
        ChineseSimplified,
        ChineseTraditional,
        English,
    }

    internal static class SupportedLanguageExtension
    {
        private static SupportedLanguage DetectChineseVariant(CultureInfo culture)
        {
            return culture.Name.Contains("Hant") || culture.Name is "zh-TW" or "zh-HK" or "zh-MO"
                ? SupportedLanguage.ChineseTraditional
                : SupportedLanguage.ChineseSimplified;
        }

        internal static SupportedLanguage GetCurrentLanguage()
        {
            var culture = CultureInfo.CurrentUICulture;
            return culture.TwoLetterISOLanguageName switch
            {
                "zh" => DetectChineseVariant(culture),
                _ => SupportedLanguage.English,
            };
        }

        internal static CultureInfo GetDefaultCultureInfo(this SupportedLanguage language)
        {
            return language switch
            {
                SupportedLanguage.ChineseSimplified => new CultureInfo("zh-CN"),
                SupportedLanguage.ChineseTraditional => new CultureInfo("zh-TW"),
                _ => new CultureInfo("en-US"),
            };
        }

        internal static string GetFontName(this SupportedLanguage language)
        {
            return language switch
            {
                SupportedLanguage.ChineseSimplified => "Microsoft YaHei",
                SupportedLanguage.ChineseTraditional => "Microsoft JhengHei",
                _ => "Consolas",
            };
        }
    }
}
