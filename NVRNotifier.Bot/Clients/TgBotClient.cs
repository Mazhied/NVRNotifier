using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Telegram.Bot;

using NVRNotifier.Core.Settings;


namespace NVRNotifier.Bot.Clients
{
    internal class TgBotClient
    {
        private readonly IAppSettings _appSettings;
        private TelegramBotClient _botClient;

        public TgBotClient(IAppSettings appSettings)
        {
            this._appSettings = appSettings;

            Initialize();
        }

        private void Initialize()
        {
            _botClient = new TelegramBotClient(_appSettings.TelegramBotToken);
            
        }
    }
}
