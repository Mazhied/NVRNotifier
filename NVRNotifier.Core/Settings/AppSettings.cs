using Microsoft.Extensions.Configuration;
using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NVRNotifier.Core.Settings
{
    internal class AppSettings: IAppSettings
    {
        public string ZoneMinderHost { get; set; }
        public string ZoneMinderPort { get; set; }
        public string ZoneMinderUser { get; set; }
        public string ZoneMinderPassword { get; set; }
        public string ZoneMinderVideoPath { get; set; }
        public string UseApiSsl { get; set; }
        public string TelegramBotToken { get; set; }
        public long ChatId { get; set; }

        public AppSettings(IConfiguration configuration)
        {
            this.ZoneMinderHost = configuration.GetValue<string>("ZoneMinder:Server") ?? "127.0.0.1";
            this.ZoneMinderPort = configuration.GetValue<string>("ZoneMinder:Port") ?? "9000";
            this.ZoneMinderUser = configuration.GetValue<string>("ZoneMinder:User") ?? "admin";
            this.ZoneMinderPassword = configuration.GetValue<string>("ZoneMinder:Password") ?? "admin";
            this.ZoneMinderVideoPath = configuration.GetValue<string>("ZoneMinder:VideoPath") ?? "/mnt/hdd1/camera_recordings";

            this.TelegramBotToken = configuration.GetValue<string>("Telegram:BotToken") ?? "";
            this.ChatId = configuration.GetValue<long>("Telegram:ChatId");
        }


    }
}
