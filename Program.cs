using Microsoft.AspNetCore;
using Microsoft.AspNetCore.Hosting;
using System;
using System.Data.SQLite;

namespace wow.tools.api
{
    public class Program
    {
        public static SQLiteConnection cnnOut = new SQLiteConnection("Data Source=:memory:;foreign keys=True;");

        public static void Main(string[] args)
        {
            SQLiteConnection cnnIn = new SQLiteConnection("Data Source=export.db3;foreign keys=True;Version=3;Read Only=True;");
            cnnIn.Open();
            cnnOut.Open();
            cnnIn.BackupDatabase(cnnOut, "main", "main", -1, BackupCallback, -1);
            cnnIn.Close();

            var query = new SQLiteCommand();
            query.Connection = cnnOut;
            query.CommandText = "PRAGMA query_only = TRUE";
            query.CommandTimeout = 10;
            query.ExecuteNonQuery();

            CreateWebHostBuilder(args).Build().Run();
        }

        private static bool BackupCallback(SQLiteConnection source, string sourceName, SQLiteConnection destination, string destinationName, int pages, int remainingPages, int totalPages, bool retry)
        {
            Console.WriteLine(remainingPages + "/" + totalPages);
            return true;
        }

        public static IWebHostBuilder CreateWebHostBuilder(string[] args) =>
            WebHost
                .CreateDefaultBuilder(args)
                .UseStartup<Startup>();
    }
}
