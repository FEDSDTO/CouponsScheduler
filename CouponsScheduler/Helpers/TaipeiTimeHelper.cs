using System;

namespace CouponsScheduler.Helpers
{
    /// <summary>台北時區日期區間計算。</summary>
    public static class TaipeiTimeHelper
    {
        private static readonly TimeZoneInfo Taipei =
            TimeZoneInfo.FindSystemTimeZoneById("Taipei Standard Time");

        public static DateTime ToTaipei(DateTime utcOrUnspecified)
        {
            var dt = utcOrUnspecified.Kind == DateTimeKind.Unspecified
                ? DateTime.SpecifyKind(utcOrUnspecified, DateTimeKind.Utc)
                : utcOrUnspecified;
            return TimeZoneInfo.ConvertTimeFromUtc(dt.ToUniversalTime(), Taipei);
        }

        /// <summary>取得指定日（台北）00:00:00 與 23:59:59.997。</summary>
        public static void GetDayRange(DateTime? checkDateLocal, out DateTime dayStart, out DateTime dayEnd)
        {
            var baseDate = checkDateLocal ?? ToTaipei(DateTime.UtcNow);
            var d = baseDate.Date;
            dayStart = d;
            dayEnd = d.AddDays(1).AddMilliseconds(-3);
        }

        public static string FormatDate(DateTime d) => d.ToString("yyyy-MM-dd");
    }
}
