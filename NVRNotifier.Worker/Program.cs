using NVRNotifier.Worker;
using NVRNotifier.Core;
using NVRNotifier.Bot;
using System.Runtime.CompilerServices;

var builder = Host.CreateApplicationBuilder(args);
var services = builder.Services;
builder.Services.AddHostedService<Worker>();
builder.Services.AddCore();
builder.Services.AddTelegramNotifierBot(builder.Configuration);

var host = builder.Build();
await host.RunAsync();
