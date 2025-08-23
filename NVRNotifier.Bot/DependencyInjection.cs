using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Http;
using Microsoft.Extensions.DependencyInjection;
using NVRNotifier.Bot.Services;
using NVRNotifier.Core.Interfaces;
using Telegram.Bot;
using NVRNotifier.Bot.Abstract;

namespace NVRNotifier.Bot;

public static class DependencyInjection
{
    public static IServiceCollection AddTelegramNotifierBot(this IServiceCollection services, IConfiguration configuration)
    {
        services.AddHttpClient("telegram_bot_client").RemoveAllLoggers()
                .AddTypedClient<ITelegramBotClient>((httpClient, sp) =>
                {
                    TelegramBotClientOptions options = new(configuration.GetValue<string>("Telegram:BotToken") ?? "");
                    return new TelegramBotClient(options, httpClient);
                });

        services.AddScoped<UpdateHandler>();
        services.AddScoped<IReceiverService, ReceiverService>();
        return services;
    }
}
