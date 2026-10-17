using System;
using System.Globalization;

namespace TinCan.Json
{
    internal static class Timestamp
    {
        const string FormatString = "yyyy-MM-dd'T'HH:mm:ss.fff'Z'";

        public static string Format(DateTime value)
        {
            DateTime utc;
            if (value.Kind == DateTimeKind.Local)
            {
                utc = value.ToUniversalTime();
            }
            else
            {
                utc = DateTime.SpecifyKind(value, DateTimeKind.Utc);
            }

            return utc.ToString(FormatString, CultureInfo.InvariantCulture);
        }

        public static DateTime Parse(string value)
        {
            var parsed = DateTimeOffset.Parse(value, CultureInfo.InvariantCulture, DateTimeStyles.RoundtripKind);
            return parsed.UtcDateTime;
        }
    }
}
