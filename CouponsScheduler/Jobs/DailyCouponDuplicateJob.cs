using System;
using CouponsScheduler.Helpers;
using CouponsScheduler.Models;
using CouponsScheduler.Notifications;
using CouponsScheduler.Services;
using NLog;

namespace CouponsScheduler.Jobs
{
    public class DailyCouponDuplicateJob
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();
        private readonly ICouponDuplicateCheckService _checkService = new CouponDuplicateCheckService();
        private readonly EmailNotificationSender _notifier = new EmailNotificationSender();
        private readonly JobExecutionLogWriter _logWriter = new JobExecutionLogWriter();

        /// <summary>0=無重複；1=有重複；2=失敗。</summary>
        public int Run(DateTime? checkDateLocal)
        {
            DuplicateCheckResult result = null;
            try
            {
                Log.Info("開始 Coupon 重複檢查，檢查日={0}",
                    checkDateLocal.HasValue ? checkDateLocal.Value.ToString("yyyy-MM-dd") : "(今天台北)");

                result = _checkService.Run(checkDateLocal);
                Log.Info("結果={0}, {1}", result.Result, result.Message);

                _notifier.Send(result);
                _logWriter.Write(result, result.HasDuplicate ? 1 : 0);
                return result.HasDuplicate ? 1 : 0;
            }
            catch (Exception ex)
            {
                Log.Error(ex, "執行失敗");
                if (result == null)
                {
                    TaipeiTimeHelper.GetDayRange(checkDateLocal, out var ds, out var de);
                    result = new DuplicateCheckResult
                    {
                        CheckDate = (checkDateLocal ?? TaipeiTimeHelper.ToTaipei(DateTime.UtcNow)).Date,
                        DayStart = ds,
                        DayEnd = de,
                        Result = "FAILED",
                        Message = ex.Message
                    };
                }
                _logWriter.Write(result, 2, ex.ToString());
                return 2;
            }
        }
    }
}
