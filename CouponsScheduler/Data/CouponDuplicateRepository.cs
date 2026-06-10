using System;
using System.Collections.Generic;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using CouponsScheduler.Models;
using CouponsScheduler.Models.Gifts;

namespace CouponsScheduler.Data
{
    /// <summary>抵用券重複檢查資料查詢（EF LINQ）。</summary>
    public class CouponDuplicateRepository
    {
        private static readonly DateTime MaxUsedEnd = new DateTime(9999, 12, 31);

        private sealed class TodayValidCoupon
        {
            public string CouponNo { get; set; }
            public DateTime UsedStart { get; set; }
            public DateTime UsedEnd { get; set; }
        }

        public IList<DuplicateLogRow> QueryDuplicates(DateTime dayStart, DateTime dayEnd)
        {
            var couponType = ConfigurationManager.AppSettings["DuplicateCheck.CouponType"] ?? "C";
            var logStatus = ConfigurationManager.AppSettings["DuplicateCheck.LogStatus"] ?? "N";
            var minDupStr = ConfigurationManager.AppSettings["DuplicateCheck.MaxDuplicateCount"];
            var requireModifyUser = bool.Parse(
                ConfigurationManager.AppSettings["DuplicateCheck.RequireModifyUser"] ?? "true");
            int? minDuplicateCount = null;
            if (!string.IsNullOrWhiteSpace(minDupStr) && int.TryParse(minDupStr, out var minCnt))
                minDuplicateCount = minCnt;

            using (var db = new GiftsEntities())
            {
                var todayValid = db.Coupons.AsNoTracking()
                    .Where(c => c.Type == couponType
                        && c.UsedStart != null
                        && c.UsedStart <= dayEnd
                        && (c.UsedEnd ?? MaxUsedEnd) >= dayStart)
                    .Select(c => new { c.CouponNo, c.UsedStart, c.UsedEnd })
                    .ToList()
                    .Where(c => !string.IsNullOrWhiteSpace(c.CouponNo))
                    .Select(c => new TodayValidCoupon
                    {
                        CouponNo = c.CouponNo.Trim(),
                        UsedStart = c.UsedStart.Value,
                        UsedEnd = c.UsedEnd ?? MaxUsedEnd
                    })
                    .GroupBy(x => new { x.CouponNo, x.UsedStart, x.UsedEnd })
                    .Select(g => g.First())
                    .ToList();

                if (todayValid.Count == 0)
                    return new List<DuplicateLogRow>();

                var minLogStart = todayValid.Min(x => x.UsedStart);
                var maxLogEnd = todayValid.Max(x => x.UsedEnd);

                var logsRaw = db.Coupon_Log.AsNoTracking()
                    .Where(l => l.Status == logStatus
                        && l.MemberId != null
                        && l.CouponNo != null
                        && (!requireModifyUser || (l.ModifyUser != null && l.ModifyUser != 0))
                        && l.CreateOn >= minLogStart
                        && l.CreateOn <= maxLogEnd)
                    .ToList();

                var cleanLog = logsRaw
                    .Select(l => new
                    {
                        l.Id,
                        MemberId = (l.MemberId ?? string.Empty).Trim(),
                        CouponNo = (l.CouponNo ?? string.Empty).Trim(),
                        l.CreateOn,
                        l.Status,
                        RecordKey = l.recordKey,
                        l.ModifyUser
                    })
                    .Where(l => l.CouponNo != string.Empty
                        && IsValidMemberId(l.MemberId)
                        && (!requireModifyUser || (l.ModifyUser.HasValue && l.ModifyUser.Value != 0)))
                    .ToList();

                var scopedLog = (
                    from cl in cleanLog
                    from tv in todayValid
                    where string.Equals(cl.CouponNo, tv.CouponNo, StringComparison.OrdinalIgnoreCase)
                       && cl.CreateOn >= tv.UsedStart
                       && cl.CreateOn <= tv.UsedEnd
                    select new DuplicateLogRow
                    {
                        Id = cl.Id,
                        MemberId = cl.MemberId,
                        CouponNo = cl.CouponNo,
                        CreateOn = cl.CreateOn,
                        Status = cl.Status,
                        RecordKey = cl.RecordKey,
                        ModifyUser = cl.ModifyUser
                    }).ToList();

                var dupCounts = scopedLog
                    .GroupBy(x => x.CouponNo, StringComparer.OrdinalIgnoreCase)
                    .Where(g => g.Count() > 1)
                    .ToDictionary(g => g.Key, g => g.Count(), StringComparer.OrdinalIgnoreCase);

                if (dupCounts.Count == 0)
                    return new List<DuplicateLogRow>();

                var result = scopedLog
                    .Where(x => dupCounts.ContainsKey(x.CouponNo))
                    .Select(x =>
                    {
                        x.DuplicateCount = dupCounts[x.CouponNo];
                        return x;
                    })
                    .OrderByDescending(x => x.DuplicateCount)
                    .ThenBy(x => x.CouponNo)
                    .ThenByDescending(x => x.Id)
                    .ToList();

                if (minDuplicateCount.HasValue)
                    result = result.Where(r => r.DuplicateCount >= minDuplicateCount.Value).ToList();

                return result;
            }
        }

        private static bool IsValidMemberId(string memberId)
        {
            if (string.IsNullOrWhiteSpace(memberId))
                return false;

            var m = memberId.Trim();
            if (m.Equals("NULL", StringComparison.OrdinalIgnoreCase))
                return false;

            var c = char.ToLowerInvariant(m[0]);
            return c == 'c' || c == 'b' || c == 'r';
        }
    }
}
