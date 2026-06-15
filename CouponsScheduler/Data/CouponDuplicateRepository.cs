using System;
using System.Collections.Generic;
using System.Configuration;
using System.Linq;
using CouponsScheduler.Helpers;
using CouponsScheduler.Models;
using Dapper;

namespace CouponsScheduler.Data
{
    /// <summary>抵用券重複檢查資料查詢（Dapper SQL）。</summary>
    public class CouponDuplicateRepository
    {
        private const string QuerySql = @"
WITH TodayValidCoupons AS (
    SELECT DISTINCT
        LTRIM(RTRIM(c.CouponNo)) AS CouponNo,
        c.UsedStart,
        ISNULL(c.UsedEnd, CAST('9999-12-31' AS DATETIME)) AS UsedEnd
    FROM Coupon c
    WHERE c.Type = @CouponType
      AND c.UsedStart IS NOT NULL
      AND LTRIM(RTRIM(c.CouponNo)) <> ''
      AND c.UsedStart <= @DayEnd
      AND ISNULL(c.UsedEnd, CAST('9999-12-31' AS DATETIME)) >= @DayStart
),
CleanLog AS (
    SELECT
        l.Id,
        LTRIM(RTRIM(l.MemberId)) AS MemberId,
        l.GId,
        LTRIM(RTRIM(l.CouponNo)) AS CouponNo,
        l.CreateOn,
        l.Status,
        l.recordKey AS RecordKey,
        l.ModifyUser
    FROM Coupon_Log l
    WHERE l.Status = @LogStatus
      AND l.MemberId IS NOT NULL
      AND l.CouponNo IS NOT NULL
      AND LTRIM(RTRIM(l.MemberId)) <> ''
      AND UPPER(LTRIM(RTRIM(l.MemberId))) <> 'NULL'
      AND LTRIM(RTRIM(l.MemberId)) LIKE @MemberIdPattern
      AND LTRIM(RTRIM(l.CouponNo)) <> ''
      AND (@RequireModifyUser = 0 OR (l.ModifyUser IS NOT NULL AND l.ModifyUser <> 0))
),
ScopedLog AS (
    SELECT cl.*
    FROM CleanLog cl
    INNER JOIN TodayValidCoupons tv
        ON cl.CouponNo = tv.CouponNo
       AND cl.CreateOn >= tv.UsedStart
       AND cl.CreateOn <= tv.UsedEnd
),
DupCoupons AS (
    SELECT CouponNo, COUNT(*) AS DuplicateCount
    FROM ScopedLog
    GROUP BY CouponNo
    HAVING COUNT(*) > 1
)
SELECT
    sl.Id,
    sl.MemberId,
    sl.GId,
    sl.CouponNo,
    sl.CreateOn,
    sl.Status,
    sl.RecordKey,
    sl.ModifyUser,
    dc.DuplicateCount
FROM ScopedLog sl
INNER JOIN DupCoupons dc ON sl.CouponNo = dc.CouponNo
WHERE (@MinDuplicateCount IS NULL OR dc.DuplicateCount >= @MinDuplicateCount)
ORDER BY dc.DuplicateCount DESC, sl.CouponNo, sl.Id DESC;";

        public IList<DuplicateLogRow> QueryDuplicates(DateTime dayStart, DateTime dayEnd)
        {
            var couponType = ConfigurationManager.AppSettings["DuplicateCheck.CouponType"] ?? "C";
            var logStatus = ConfigurationManager.AppSettings["DuplicateCheck.LogStatus"] ?? "N";
            var memberIdPattern = ConfigurationManager.AppSettings["DuplicateCheck.MemberIdPrefixPattern"] ?? "[cbr]%";
            var requireModifyUser = bool.Parse(
                ConfigurationManager.AppSettings["DuplicateCheck.RequireModifyUser"] ?? "true");

            int? minDuplicateCount = null;
            var minDupStr = ConfigurationManager.AppSettings["DuplicateCheck.MaxDuplicateCount"];
            if (!string.IsNullOrWhiteSpace(minDupStr) && int.TryParse(minDupStr, out var minCnt))
                minDuplicateCount = minCnt;

            using (var conn = GiftsDbConnection.Open())
            {
                return conn.Query<DuplicateLogRow>(QuerySql, new
                {
                    DayStart = dayStart,
                    DayEnd = dayEnd,
                    CouponType = couponType,
                    LogStatus = logStatus,
                    MemberIdPattern = memberIdPattern,
                    RequireModifyUser = requireModifyUser ? 1 : 0,
                    MinDuplicateCount = minDuplicateCount
                }, commandTimeout: 120).ToList();
            }
        }
    }
}
