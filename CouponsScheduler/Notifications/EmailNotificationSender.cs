using System;
using System.Configuration;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Mail;
using System.Text;
using CouponsScheduler.Helpers;
using CouponsScheduler.Models;
using NLog;

namespace CouponsScheduler.Notifications
{
    public class EmailNotificationSender
    {
        private static readonly Logger Log = LogManager.GetCurrentClassLogger();

        public void Send(DuplicateCheckResult result)
        {
            if (!bool.Parse(ConfigurationManager.AppSettings["Notification.Enabled"] ?? "false"))
            {
                Log.Info("Notification.Enabled=false，略過郵件。");
                return;
            }

            var skipWhenNone = bool.Parse(ConfigurationManager.AppSettings["Notification.SkipDuplicateNotification"] ?? "true");
            if (skipWhenNone && !result.HasDuplicate)
            {
                Log.Info("無重複且 SkipDuplicateNotification=true，略過郵件。");
                return;
            }

            try
            {
                var host = ConfigurationManager.AppSettings["Notification.SmtpHost"];
                var from = ConfigurationManager.AppSettings["Notification.EmailFrom"] ?? "coupon-job@localhost";
                var displayName = ConfigurationManager.AppSettings["Notification.EmailDisplayName"] ?? "抵用券排程";
                var port = int.Parse(ConfigurationManager.AppSettings["Notification.SmtpPort"] ?? "587");
                var ssl = bool.Parse(ConfigurationManager.AppSettings["Notification.SmtpEnableSsl"] ?? "true");
                var user = ConfigurationManager.AppSettings["Notification.SmtpUser"];
                var pwd = ConfigurationManager.AppSettings["Notification.SmtpPassword"];

                var dateStr = TaipeiTimeHelper.FormatDate(result.CheckDate);
                var subject = string.Format("[Coupon重複檢查] {0}", dateStr);
                var body = BuildBody(result, dateStr);

                using (var msg = new MailMessage())
                {
                    msg.From = new MailAddress(from, displayName);
                    msg.Subject = subject;
                    msg.Body = body;
                    msg.BodyEncoding = Encoding.UTF8;
                    msg.IsBodyHtml = false;

                    AddAddressesFromFile(msg.To, "toEmail.txt");
                    if (msg.To.Count == 0)
                        AddAddressesFromConfig(msg.To, ConfigurationManager.AppSettings["Notification.EmailTo"]);

                    AddAddressesFromFile(msg.CC, "ccEmail.txt");

                    if (msg.To.Count == 0)
                    {
                        Log.Warn("無收件人，略過寄信。");
                        return;
                    }

                    using (var client = new SmtpClient(host, port))
                    {
                        client.EnableSsl = ssl;
                        if (!string.IsNullOrWhiteSpace(user))
                            client.Credentials = new NetworkCredential(user, pwd);
                        client.Send(msg);
                    }

                    Log.Info("已發送通知，To={0}, CC={1}",
                        string.Join(",", msg.To.Select(x => x.Address)),
                        msg.CC.Count > 0 ? string.Join(",", msg.CC.Select(x => x.Address)) : "(無)");
                }
            }
            catch (Exception ex)
            {
                Log.Error(ex, "寄信失敗");
            }
        }

        private static string BuildBody(DuplicateCheckResult result, string dateStr)
        {
            var sb = new StringBuilder();
            if (!result.HasDuplicate)
            {
                sb.AppendLine(string.Format("[Coupon重複檢查] {0}", dateStr));
                sb.AppendLine("結果：無");
                sb.AppendLine("範圍：今天有效抵用券 + 各券 UsedStart~UsedEnd 區間內 Coupon_Log（c/b/r, N）");
                return sb.ToString();
            }

            sb.AppendLine(string.Format("[Coupon重複檢查] {0}", dateStr));
            sb.AppendLine(string.Format("發現 {0} 組重複券號", result.DuplicateCouponCount));
            sb.AppendLine("券號 | 重複次數 | 發票 | IssueType | Members");
            foreach (var g in result.Items)
            {
                sb.AppendLine(string.Format("{0} | {1} | {2} | {3} | {4}",
                    g.CouponNo, g.DuplicateCount, g.InvoiceNo, g.IssueType, string.Join(", ", g.MemberIds)));
                foreach (var d in g.Details)
                {
                    sb.AppendLine(string.Format("  - LogId={0}, MemberId={1}, CreateOn={2:yyyy-MM-dd HH:mm:ss}, ModifyUser={3}, Invoice={4}",
                        d.Id, d.MemberId, d.CreateOn, d.ModifyUser, d.InvoiceNo));
                }
            }
            return sb.ToString();
        }

        private static void AddAddressesFromFile(MailAddressCollection collection, string fileName)
        {
            var path = Path.Combine(AppDomain.CurrentDomain.BaseDirectory, fileName);
            if (!File.Exists(path))
                return;

            foreach (var line in File.ReadAllLines(path))
            {
                var email = (line ?? string.Empty).Trim();
                if (!string.IsNullOrWhiteSpace(email))
                    collection.Add(email);
            }
        }

        private static void AddAddressesFromConfig(MailAddressCollection collection, string configValue)
        {
            if (string.IsNullOrWhiteSpace(configValue))
                return;

            foreach (var part in configValue.Split(new[] { ';', ',' }, StringSplitOptions.RemoveEmptyEntries))
            {
                var email = part.Trim();
                if (!string.IsNullOrEmpty(email))
                    collection.Add(email);
            }
        }
    }
}
