using System;
using System.Configuration;
using System.Data.Entity;
using System.Linq;
using CouponsScheduler.Models.Gifts;

namespace CouponsScheduler.Services
{
    /// <summary>發票解析：recordKey → MemberRecord → ExchangeInvoice；否則 MemberId + 時間窗。</summary>
    public class InvoiceResolver : IInvoiceResolver
    {
        private readonly int _windowMinutes;

        public InvoiceResolver()
        {
            _windowMinutes = int.Parse(ConfigurationManager.AppSettings["InvoiceResolver.TimeWindowMinutes"] ?? "10");
        }

        public string Resolve(string recordKey, string memberId, DateTime? createOn)
        {
            using (var db = new GiftsEntities())
            {
                if (!string.IsNullOrWhiteSpace(recordKey))
                {
                    var rk = recordKey.Trim();

                    var invMr = db.MemberRecords.AsNoTracking()
                        .Where(m => m.RecordKey == rk && m.InvoiceNo != null && m.InvoiceNo != string.Empty)
                        .OrderByDescending(m => m.Id)
                        .Select(m => m.InvoiceNo)
                        .FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(invMr))
                        return invMr.Trim();

                    var invEi = (
                        from ei in db.ExchangeInvoices.AsNoTracking()
                        join mr in db.MemberRecords.AsNoTracking() on ei.MRId equals mr.Id
                        where mr.RecordKey == rk && ei.InvoiceNo != null && ei.InvoiceNo != string.Empty
                        orderby ei.Id descending
                        select ei.InvoiceNo).FirstOrDefault();
                    if (!string.IsNullOrWhiteSpace(invEi))
                        return invEi.Trim();
                }

                if (!string.IsNullOrWhiteSpace(memberId) && createOn.HasValue)
                {
                    var mid = memberId.Trim();
                    var from = createOn.Value.AddMinutes(-_windowMinutes);
                    var to = createOn.Value.AddMinutes(_windowMinutes);
                    var anchor = createOn.Value;

                    var candidates = db.ExchangeInvoices.AsNoTracking()
                        .Where(e => e.MemberId == mid
                            && e.CreateOn >= from
                            && e.CreateOn <= to
                            && e.InvoiceNo != null && e.InvoiceNo != string.Empty)
                        .Select(e => new { e.InvoiceNo, e.CreateOn, e.Id })
                        .ToList();

                    var best = candidates
                        .OrderBy(e => Math.Abs((e.CreateOn ?? anchor).Subtract(anchor).TotalSeconds))
                        .ThenByDescending(e => e.Id)
                        .FirstOrDefault();

                    if (best != null && !string.IsNullOrWhiteSpace(best.InvoiceNo))
                        return best.InvoiceNo.Trim();
                }
            }

            return "查無";
        }
    }
}
