using Microsoft.Extensions.Configuration;
using System.IO;

namespace wow.tools.api
{
    public static class SettingsManager
    {
        public static string cacheDir;
        public static string connectionString;
        public static string apiKey;
        public static string cascToolHost;

        static SettingsManager()
        {
            LoadSettings();
        }

        public static void LoadSettings()
        {
            var config = new ConfigurationBuilder().SetBasePath(Directory.GetCurrentDirectory()).AddJsonFile("config.json", optional: false, reloadOnChange: false).Build();
            cacheDir = config.GetSection("config")["cacheDir"];
            connectionString = config.GetSection("config")["connectionString"];
            apiKey = config.GetSection("config")["apiKey"];
            cascToolHost = config.GetSection("config")["cascToolHost"];
        }
    }
}