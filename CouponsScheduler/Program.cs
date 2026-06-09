using System;
using System.Globalization;
using CouponsScheduler.Jobs;
using NLog;

namespace CouponsScheduler
{
    internal static class Program
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        private static int Main(string[] args)
        {
            try
            {
                DateTime? checkDate = null;
                var runNow = false;
                var showHelp = false;

                for (var i = 0; i < args.Length; i++)
                {
                    var a = args[i].Trim();
                    if (a.Equals("--help", StringComparison.OrdinalIgnoreCase) || a == "-h" || a == "/?")
                        showHelp = true;
                    else if (a.Equals("--run-now", StringComparison.OrdinalIgnoreCase))
                        runNow = true;
                    else if (a.Equals("--date", StringComparison.OrdinalIgnoreCase) && i + 1 < args.Length)
                    {
                        if (!DateTime.TryParseExact(args[++i], "yyyy-MM-dd", CultureInfo.InvariantCulture,
                            DateTimeStyles.None, out var d))
                        {
                            Console.WriteLine("無效日期格式，請使用 yyyy-MM-dd");
                            return 2;
                        }
                        checkDate = d;
                        runNow = true;
                    }
                }

                if (showHelp)
                {
                    PrintHelp();
                    return 0;
                }

                if (!runNow && args.Length > 0)
                {
                    Console.WriteLine("未知參數。使用 --help 查看說明。");
                    return 2;
                }

                return new DailyCouponDuplicateJob().Run(checkDate);
            }
            catch (Exception ex)
            {
                Log.Fatal(ex, "程式未處理例外");
                Console.WriteLine(ex);
                return 2;
            }
            finally
            {
                LogManager.Shutdown();
            }
        }

        private static void PrintHelp()
        {
            Console.WriteLine(@"CouponsScheduler - Coupon 重複券號每日檢查 (.NET Framework 4.8)

用法：
  CouponsScheduler.exe                    （工作排程器每日執行）
  CouponsScheduler.exe --run-now          （手動，檢查今天台北）
  CouponsScheduler.exe --run-now --date 2026-05-20

結束碼：
  0 = 成功，無重複
  1 = 成功，發現重複
  2 = 執行失敗");
        }
    }
}
