using System;
using UnityEngine;
#if ENABLED_UNITY_MATHEMATICS
using Unity.Mathematics;
#endif

namespace AES.Tools
{
    public static class NumberExtensions {
        public static float PercentageOf(this int part, int whole) {
            if (whole == 0) return 0; // Handling division by zero
            return (float) part / whole;
        }

        public static bool Approx(this float f1, float f2) => Mathf.Approximately(f1, f2);
        public static bool IsOdd(this int i) => i % 2 == 1;
        public static bool IsEven(this int i) => i % 2 == 0;

        public static int AtLeast(this int value, int min) => Mathf.Max(value, min);
        public static int AtMost(this int value, int max) => Mathf.Min(value, max);

#if ENABLED_UNITY_MATHEMATICS
        public static half AtLeast(this half value, half max) => MathfExtension.Max(value, max);
        public static half AtMost(this half value, half max) => MathfExtension.Min(value, max);
#endif

        public static float AtLeast(this float value, float min) => Mathf.Max(value, min);
        public static float AtMost(this float value, float max) => Mathf.Min(value, max);

        public static double AtLeast(this double value, double min) => MathfExtension.Max(value, min);
        public static double AtMost(this double value, double min) => MathfExtension.Min(value, min);
        
        
        /// <summary>
        /// 1234   -> "1.2k"
        /// 1234567-> "1.2m"
        /// 999    -> "999"
        /// </summary>
        public static string ToCompact(this long value, int decimalDigits = 1)
        {
            return ToCompactInternal(value, decimalDigits);
        }

        public static string ToCompact(this int value, int decimalDigits = 1)
        {
            return ToCompactInternal(value, decimalDigits);
        }

        public static string ToCompact(this double value, int decimalDigits = 1)
        {
            return ToCompactInternal(value, decimalDigits);
        }

        public static string ToCompact(this float value, int decimalDigits = 1)
        {
            return ToCompactInternal(value, decimalDigits);
        }

        static string ToCompactInternal(double value, int decimalDigits)
        {
            double abs = System.Math.Abs(value);
            string suffix = "";
            double shortNumber = value;

            if (abs >= 1_000_000_000d)
            {
                shortNumber = value / 1_000_000_000d;
                suffix = "b";
            }
            else if (abs >= 1_000_000d)
            {
                shortNumber = value / 1_000_000d;
                suffix = "m";
            }
            else if (abs >= 1_000d)
            {
                shortNumber = value / 1_000d;
                suffix = "k";
            }

            // 내림 (floor) 적용
            double factor = System.Math.Pow(10, decimalDigits);
            shortNumber = System.Math.Floor(shortNumber * factor) / factor;
            
            // 소수 자릿수만큼 포맷 (예: 1.2k)
            string format = decimalDigits > 0 ? $"0.{new string('0', decimalDigits)}" : "0";
            string number = shortNumber.ToString(format, System.Globalization.CultureInfo.InvariantCulture);

            // 소수점 뒤가 0이면 정리 (예: 1.0k -> 1k)
            if (number.Contains("."))
            {
                number = number.TrimEnd('0').TrimEnd('.');
            }

            return number + suffix;
        }
        
        /// <summary>
        /// TimeSpan → "y h m s" 형식
        /// 예)
        ///  59s        -> "59s"
        ///  61s        -> "1m 1s"
        ///  3661s      -> "1h 1m 1s"
        ///  31536001s  -> "1y 1s"
        /// </summary>
        public static string ToSMHY(this TimeSpan time, bool includeZeroUnits = false)
        {
            if (time <= TimeSpan.Zero)
                return "0s";

            const int DaysPerYear = 365;

            long totalSeconds = (long)Math.Floor(time.TotalSeconds);

            long secondsPerMinute = 60;
            long secondsPerHour   = 60 * secondsPerMinute;
            long secondsPerDay    = 24 * secondsPerHour;
            long secondsPerYear   = DaysPerYear * secondsPerDay;

            long years   = totalSeconds / secondsPerYear;
            totalSeconds %= secondsPerYear;

            long hours   = totalSeconds / secondsPerHour;
            totalSeconds %= secondsPerHour;

            long minutes = totalSeconds / secondsPerMinute;
            long seconds = totalSeconds % secondsPerMinute;

            System.Text.StringBuilder sb = new(32);

            void Append(long v, string unit)
            {
                if (!includeZeroUnits && v == 0) return;
                if (sb.Length > 0) sb.Append(' ');
                sb.Append(v).Append(unit);
            }

            Append(years, "y");
            Append(hours, "h");
            Append(minutes, "m");

            if (sb.Length == 0 || includeZeroUnits || seconds != 0)
                Append(seconds, "s");

            return sb.ToString();
        }

        // 초 기반 오버로드
        public static string ToSMHY(this long seconds, bool includeZeroUnits = false)
            => TimeSpan.FromSeconds(seconds).ToSMHY(includeZeroUnits);

        public static string ToSMHY(this int seconds, bool includeZeroUnits = false)
            => ((long)seconds).ToSMHY(includeZeroUnits);

        public static string ToSMHY(this float seconds, bool includeZeroUnits = false)
            => TimeSpan.FromSeconds(seconds).ToSMHY(includeZeroUnits);

        public static string ToSMHY(this double seconds, bool includeZeroUnits = false)
            => TimeSpan.FromSeconds(seconds).ToSMHY(includeZeroUnits);
        
        /// <summary>
        /// 분(minute) → "y h m s"
        /// 예)
        ///  1      -> "1m"
        ///  61     -> "1h 1m"
        ///  1500   -> "1d 1h"
        /// </summary>
        public static string MinutesToSMHY(this int minutes, bool includeZeroUnits = false)
            => TimeSpan.FromMinutes(minutes).ToSMHY(includeZeroUnits);

        public static string MinutesToSMHY(this long minutes, bool includeZeroUnits = false)
            => TimeSpan.FromMinutes(minutes).ToSMHY(includeZeroUnits);

        public static string MinutesToSMHY(this float minutes, bool includeZeroUnits = false)
            => TimeSpan.FromMinutes(minutes).ToSMHY(includeZeroUnits);

        public static string MinutesToSMHY(this double minutes, bool includeZeroUnits = false)
            => TimeSpan.FromMinutes(minutes).ToSMHY(includeZeroUnits);

    }
}