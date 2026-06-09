using System;

namespace CouponsScheduler.Services
{
    public interface IInvoiceResolver
    {
        string Resolve(string recordKey, string memberId, DateTime? createOn);
    }
}
