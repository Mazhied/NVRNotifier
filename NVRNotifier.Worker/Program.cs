using System.Runtime.CompilerServices;
using Serilog;


using NVRNotifier.Worker;
using NVRNotifier.Core;
using NVRNotifier.Bot;

var builder = Host.CreateApplicationBuilder(args);
var services = builder.Services;
builder.Services.AddHostedService<Worker>();

Log.Logger = new LoggerConfiguration().
    MinimumLevel.Debug().
    WriteTo.Console(restrictedToMinimumLevel: Serilog.Events.LogEventLevel.Information).
    WriteTo.File("logs/log.txt",
                 rollingInterval: RollingInterval.Day).
    CreateLogger();

builder.Logging.ClearProviders();
builder.Logging.AddSerilog(Log.Logger);

builder.Services.AddCore();
builder.Services.AddTelegramNotifierBot(builder.Configuration);

var host = builder.Build();
await host.RunAsync();
