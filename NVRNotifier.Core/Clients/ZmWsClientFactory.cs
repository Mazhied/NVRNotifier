using Microsoft.Extensions.Logging;
using NVRNotifier.Core.Settings;
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace NVRNotifier.Core.Clients
{
    public class ZmWsClientFactory
    {
        private readonly ILogger<ZmWsClient> _logger;
        private readonly IAppSettings _appSettings;

        public ZmWsClientFactory(ILogger<ZmWsClient> logger, IAppSettings appSettings)
        {
            _logger = logger;
            _appSettings = appSettings;
        }

        public ZmWsClient Create()
        {
            return new ZmWsClient(
                _appSettings.ZoneMinderHost,
                _appSettings.ZoneMinderPort,
                _appSettings.ZoneMinderUser,
                _appSettings.ZoneMinderPassword,
                _logger);
        }
    }
}
