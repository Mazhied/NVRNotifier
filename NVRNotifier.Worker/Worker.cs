using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using NVRNotifier.Bot.Abstract;
using NVRNotifier.Core.Clients;

namespace NVRNotifier.Worker
{
    public class Worker : BackgroundService
    {
        private readonly IServiceProvider _serviceProvider;
        private readonly ILogger<Worker> _logger;

        public Worker(ILogger<Worker> logger, IServiceProvider serviceProvider)
        {
            _logger = logger;
            _serviceProvider = serviceProvider;
        }

        protected override async Task ExecuteAsync(CancellationToken stoppingToken)
        {
            while (!stoppingToken.IsCancellationRequested)
            {
                try
                {
                    //todo: добавить zmwsclient и обработку событий OnEventReceived и OnError

                    // Create new IServiceScope on each iteration. This way we can leverage benefits
                    // of Scoped TReceiverService and typed HttpClient - we'll grab "fresh" instance each time
                    using var scope = _serviceProvider.CreateScope();
                    var receiver = scope.ServiceProvider.GetRequiredService<IReceiverService>();

                    await receiver.ReceiveAsync(stoppingToken);
                }
                catch (Exception ex)
                {
                    _logger.LogError("Polling failed with exception: {Exception}", ex);
                    // Cooldown if something goes wrong
                    await Task.Delay(TimeSpan.FromSeconds(5), stoppingToken);
                }
                await Task.Delay(1000, stoppingToken);
            }
        }
    }
}
