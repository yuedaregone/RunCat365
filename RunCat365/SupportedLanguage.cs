// Copyright 2025 Takuto Nakamura
//
//    Licensed under the Apache License, Version 2.0 (the "License");
//    you may not use this file except in compliance with the License.
//    You may obtain a copy of the License at
//
//        http://www.apache.org/licenses/LICENSE-2.0
//
//    Unless required by applicable law or agreed to in writing, software
//    distributed under the License is distributed on an "AS IS" BASIS,
//    WITHOUT WARRANTIES OR CONDITIONS OF ANY KIND, either express or implied.
//    See the License for the specific language governing permissions and
//    limitations under the License.

using System.Globalization;

namespace RunCat365
{
    internal enum SupportedLanguage
    {
        ChineseSimplified,
        ChineseTraditional,
        English,
        French,
        German,
        Japanese,
        Spanish,
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
                "fr" => SupportedLanguage.French,
                "de" => SupportedLanguage.German,
                "ja" => SupportedLanguage.Japanese,
                "es" => SupportedLanguage.Spanish,
                _ => SupportedLanguage.English,
            };
        }

        internal static CultureInfo GetDefaultCultureInfo(this SupportedLanguage language)
        {
            return language switch
            {
                SupportedLanguage.ChineseSimplified => new CultureInfo("zh-CN"),
                SupportedLanguage.ChineseTraditional => new CultureInfo("zh-TW"),
                SupportedLanguage.French => new CultureInfo("fr-FR"),
                SupportedLanguage.German => new CultureInfo("de-DE"),
                SupportedLanguage.Japanese => new CultureInfo("ja-JP"),
                SupportedLanguage.Spanish => new CultureInfo("es-ES"),
                _ => new CultureInfo("en-US"),
            };
        }

        internal static string GetFontName(this SupportedLanguage language)
        {
            return language switch
            {
                SupportedLanguage.ChineseSimplified => "Microsoft YaHei",
                SupportedLanguage.ChineseTraditional => "Microsoft JhengHei",
                SupportedLanguage.French => "Consolas",
                SupportedLanguage.German => "Consolas",
                SupportedLanguage.Japanese => "Noto Sans JP",
                SupportedLanguage.Spanish => "Consolas",
                _ => "Consolas",
            };
        }
    }
}
