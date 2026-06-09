using System;
using System.Configuration;
using System.IO;
using CouponsScheduler.Models;
using Newtonsoft.Json;

namespace CouponsScheduler.Helpers
{
    public class JobExecutionLogWriter
    {
        public void Write(DuplicateCheckResult result, int exitCode, string errorMessage = null)
        {
            var dirName = ConfigurationManager.AppSettings["JobExecutionLog.Directory"] ?? "logs";
            var baseDir = AppDomain.CurrentDomain.BaseDirectory;
            var dir = Path.Combine(baseDir, dirName);
            Directory.CreateDirectory(dir);

            var path = Path.Combine(dir, string.Format("job-execution-{0}.json", TaipeiTimeHelper.FormatDate(result.CheckDate)));
            var payload = new { executedAt = DateTime.Now, exitCode, errorMessage, result };
            File.WriteAllText(path, JsonConvert.SerializeObject(payload, Formatting.Indented));
        }
    }
}
