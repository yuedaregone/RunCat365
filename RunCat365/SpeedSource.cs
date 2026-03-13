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

using RunCat365.Properties;
using System.Diagnostics.CodeAnalysis;

namespace RunCat365
{
    enum SpeedSource
    {
        TomatoClock,
    }

    internal static class SpeedSourceExtension
    {
        internal static string GetLocalizedString(this SpeedSource speedSource)
        {
            return speedSource switch
            {
                SpeedSource.TomatoClock => Strings.SystemInfo_TomatoClock,
                _ => "",
            };
        }

        internal static bool TryParse([NotNullWhen(true)] string? value, out SpeedSource result)
        {
            SpeedSource? nullableResult = value switch
            {
                "TomatoClock" => SpeedSource.TomatoClock,
                _ => null,
            };

            if (nullableResult is SpeedSource nonNullableResult)
            {
                result = nonNullableResult;
                return true;
            }
            else
            {
                result = SpeedSource.TomatoClock;
                return false;
            }
        }
    }
}
