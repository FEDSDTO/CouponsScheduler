using System.Configuration;
using System.Data.SqlClient;

namespace CouponsScheduler.Helpers
{
    public static class GiftsDbConnection
    {
        public static SqlConnection Open()
        {
            var connStr = ConfigurationManager.ConnectionStrings["GiftsDb"]?.ConnectionString;
            if (string.IsNullOrWhiteSpace(connStr))
                throw new ConfigurationErrorsException("connectionStrings/GiftsDb 未設定。");

            var conn = new SqlConnection(connStr);
            conn.Open();
            return conn;
        }
    }
}
