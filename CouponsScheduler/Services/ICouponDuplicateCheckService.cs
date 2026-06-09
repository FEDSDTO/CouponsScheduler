using System;
using CouponsScheduler.Models;

namespace CouponsScheduler.Services
{
    public interface ICouponDuplicateCheckService
    {
        DuplicateCheckResult Run(DateTime? checkDateLocal);
    }
}
