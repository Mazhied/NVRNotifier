using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NVRNotifier.Core.Settings
{
    public interface IAppSettings
    {
        string ZoneMinderHost { get; set; }
        string ZoneMinderPort { get; set; }
        string ZoneMinderUser {  get; set; }
        string ZoneMinderPassword { get; set; }
        string ZoneMinderVideoPath { get; set; }
        string UseApiSsl { get; set; }
        string TelegramBotToken { get; set; }
        long ChatId { get; set; }
    }
}
