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
        public string UseApiSsl { get; set; }
        public string TelegramBotToken { get; set; }

        public AppSettings(IConfiguration configuration)
        {
            this.ZoneMinderHost = configuration.GetValue<string>("ZoneMinder:Server") ?? "127.0.0.1";
            this.ZoneMinderPort = configuration.GetValue<string>("ZoneMinder:Port") ?? "9000";
            this.TelegramBotToken = configuration.GetValue<string>("Telegram:BotToken") ?? "";
        }


    }
}
