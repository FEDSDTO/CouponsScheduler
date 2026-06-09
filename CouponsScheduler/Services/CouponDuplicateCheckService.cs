using System;
using System.Linq;
using CouponsScheduler.Data;
using CouponsScheduler.Helpers;
using CouponsScheduler.Models;

namespace CouponsScheduler.Services
{
    public class CouponDuplicateCheckService : ICouponDuplicateCheckService
    {
        private readonly CouponDuplicateRepository _repo;
        private readonly IInvoiceResolver _invoiceResolver;

        public CouponDuplicateCheckService()
        {
            _repo = new CouponDuplicateRepository();
            _invoiceResolver = new InvoiceResolver();
        }

        public DuplicateCheckResult Run(DateTime? checkDateLocal)
        {
            TaipeiTimeHelper.GetDayRange(checkDateLocal, out var dayStart, out var dayEnd);

            var result = new DuplicateCheckResult
            {
                CheckDate = (checkDateLocal ?? TaipeiTimeHelper.ToTaipei(DateTime.UtcNow)).Date,
                DayStart = dayStart,
                DayEnd = dayEnd
            };

            var rows = _repo.QueryDuplicates(dayStart, dayEnd);
            if (rows == null || rows.Count == 0)
            {
                result.Result = DuplicateResultCodes.None;
                result.Message = "無";
                return result;
            }

            foreach (var row in rows)
                row.InvoiceNo = _invoiceResolver.Resolve(row.RecordKey, row.MemberId, row.CreateOn);

            var groups = rows
                .GroupBy(r => r.CouponNo, StringComparer.OrdinalIgnoreCase)
                .Select(g =>
                {
                    var members = g.Select(x => x.MemberId).Distinct(StringComparer.OrdinalIgnoreCase).ToList();
                    var issueType = members.Count > 1 ? IssueTypes.MultiMember : IssueTypes.MultiN;
                    var invoice = g.Select(x => x.InvoiceNo).FirstOrDefault(i => i != "查無") ?? "查無";

                    return new DuplicateCouponGroup
                    {
                        CouponNo = g.Key,
                        DuplicateCount = g.First().DuplicateCount,
                        InvoiceNo = invoice,
                        IssueType = issueType,
                        MemberIds = members,
                        Details = g.OrderByDescending(x => x.Id).ToList()
                    };
                })
                .OrderByDescending(g => g.DuplicateCount)
                .ThenBy(g => g.CouponNo)
                .ToList();

            result.Result = DuplicateResultCodes.DuplicateFound;
            result.Message = string.Format("發現 {0} 組重複券號", groups.Count);
            result.DuplicateCouponCount = groups.Count;
            result.Items = groups;
            return result;
        }
    }
}
