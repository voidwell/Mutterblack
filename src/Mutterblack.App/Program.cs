using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Hosting;
using Mutterblack.Client;
using Mutterblack.Discord;
using Voidwell.Common.Logging;

var builder = Host.CreateApplicationBuilder(args);

builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true)
    .AddEnvironmentVariables();

builder.AddApplicationLogging();

builder.Services
    .AddDiscord()
    .AddVoidwellClient();

await builder.Build().RunAsync();
