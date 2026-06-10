using System;
using System.Collections.Generic;

namespace CouponsScheduler.Models
{
    // 重複檢查結果的狀態碼常數
    public static class DuplicateResultCodes
    {
        public const string None = "NONE";
        public const string DuplicateFound = "DUPLICATE_FOUND";
    }

    // 優惠券異常發放或領取的類型常數
    public static class IssueTypes
    {
        public const string MultiN = "MULTI_N";           // 同券/同發票被單一會員重複領取多次
        public const string MultiMember = "MULTI_MEMBER"; // 同券/同發票被多個不同會員領取
    }

    // 從資料庫或日誌中撈出的單筆優惠券發放/領取原始紀錄
    public class DuplicateLogRow
    {
        public long Id { get; set; }
        public string MemberId { get; set; }
        public string CouponNo { get; set; }
        public DateTime? CreateOn { get; set; }
        public string Status { get; set; }
        public string RecordKey { get; set; } // 用於比對重複的鍵值
        public int? ModifyUser { get; set; }
        public int DuplicateCount { get; set; }
        public string InvoiceNo { get; set; }
        public string IssueType { get; set; } // 對應 IssueTypes
    }

    // 依特定優惠券編號彙整後的重複群組資料
    public class DuplicateCouponGroup
    {
        public string CouponNo { get; set; }
        public int DuplicateCount { get; set; }
        public string InvoiceNo { get; set; }
        public string IssueType { get; set; } // 對應 IssueTypes
        public List<string> MemberIds { get; set; } = new List<string>(); // 涉及此事件的所有會員
        public List<DuplicateLogRow> Details { get; set; } = new List<DuplicateLogRow>(); // 原始明細紀錄
    }

    // 每日優惠券重複檢查作業的最終產出結果
    public class DuplicateCheckResult
    {
        public string Result { get; set; } // 對應 DuplicateResultCodes
        public string Message { get; set; }
        public DateTime CheckDate { get; set; }
        public DateTime DayStart { get; set; } // 檢查區間起點 (00:00:00)
        public DateTime DayEnd { get; set; }   // 檢查區間終點 (23:59:59.997)
        public int DuplicateCouponCount { get; set; }
        public List<DuplicateCouponGroup> Items { get; set; } = new List<DuplicateCouponGroup>();
        public bool HasDuplicate => Result == DuplicateResultCodes.DuplicateFound; // 捷徑屬性：是否有重複異常
    }
}