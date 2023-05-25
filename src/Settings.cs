
using Microsoft.Extensions.Configuration;

namespace mstg
{
    class Settings
    {
        public static string Api_Mastodon_Instance { get; set; }
        public static string Api_Mastodon_Token { get; set; }
        public static string Api_Telegram_Channel { get; set; }
        public static string Api_Telegram_Token { get; set; }
        public static double Delay { get; set; }

        public static void Setup(string configPath = "config/")
        {
            if (!configPath.StartsWith("/"))
                configPath = Path.Join(Directory.GetCurrentDirectory(), configPath);

            if (!Directory.Exists(configPath))
                Directory.CreateDirectory(configPath);

            IConfigurationBuilder configBuilder = new ConfigurationBuilder()
                .SetBasePath(configPath)
                .AddJsonFile("settings.json", false);
            IConfiguration configRoot = configBuilder.Build();

            Settings.Api_Mastodon_Instance = configRoot["api:mastodon:instance"];
            Settings.Api_Mastodon_Token = configRoot["api:mastodon:token"];
            Settings.Api_Telegram_Channel = configRoot["api:telegram:channel"];
            Settings.Api_Telegram_Token = configRoot["api:telegram:token"];
            Settings.Delay = DoesSettingExist(configRoot, "delay") ? double.Parse(configRoot["delay"]) : 0;

            if (Settings.Api_Telegram_Channel.StartsWith("@") != true)
            {
                Settings.Api_Telegram_Channel = $"@{Settings.Api_Telegram_Channel}";
            }
        }

        static bool DoesSettingExist(IConfiguration configRoot, string settingString)
        {
            try
            {
                var setting = configRoot[settingString];

                if (setting != null)
                    return true;
                else
                    return false;
            }
            catch
            {
                return false;
            }
        }
    }
}