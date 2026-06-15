using System;
using System.Collections.Generic;

namespace CouponsScheduler.Models
{
    public static class DuplicateResultCodes
    {
        public const string None = "NONE";
        public const string DuplicateFound = "DUPLICATE_FOUND";
    }

    public static class IssueTypes
    {
        public const string MultiN = "MULTI_N";           // 同券被單一會員重複領取多次
        public const string MultiMember = "MULTI_MEMBER"; // 同券被多個不同會員領取
    }

    public class DuplicateLogRow
    {
        public long Id { get; set; }
        public string MemberId { get; set; }
        public int? GId { get; set; }
        public string CouponNo { get; set; }
        public DateTime? CreateOn { get; set; }
        public string Status { get; set; }
        public string RecordKey { get; set; }
        public int? ModifyUser { get; set; }
        public int DuplicateCount { get; set; }
    }

    public class DuplicateCouponGroup
    {
        public string CouponNo { get; set; }
        public int DuplicateCount { get; set; }
        public string IssueType { get; set; }
        public List<string> MemberIds { get; set; } = new List<string>();
        public List<DuplicateLogRow> Details { get; set; } = new List<DuplicateLogRow>();
    }

    public class DuplicateCheckResult
    {
        public string Result { get; set; }
        public string Message { get; set; }
        public DateTime CheckDate { get; set; }
        public DateTime DayStart { get; set; }
        public DateTime DayEnd { get; set; }
        public int DuplicateCouponCount { get; set; }
        public List<DuplicateCouponGroup> Items { get; set; } = new List<DuplicateCouponGroup>();
        public bool HasDuplicate => Result == DuplicateResultCodes.DuplicateFound;
    }
}
