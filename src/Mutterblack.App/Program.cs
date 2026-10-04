using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Mutterblack.App;
using Mutterblack.Client;
using Mutterblack.Discord;

var builder = Host.CreateApplicationBuilder(new HostApplicationBuilderSettings
{
    ApplicationName = "Mutterblack",
});

builder.Configuration.Sources.Clear();
builder.Configuration
    .AddJsonFile("appsettings.json")
    .AddJsonFile($"appsettings.{builder.Environment.EnvironmentName}.json", true)
    .AddEnvironmentVariables();

builder.Logging.AddLogging(builder.Environment, builder.Configuration);

builder.Services
    .AddDiscord()
    .AddVoidwellClient();

await builder.Build().RunAsync();
