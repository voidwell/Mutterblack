using Microsoft.Extensions.DependencyInjection;
using Mutterblack.Client.Authentication;

namespace Mutterblack.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVoidwellClient(this IServiceCollection services)
    {
        services.AddOptionsWithValidateOnStart<VoidwellClientOptions>()
            .BindConfiguration("Voidwell")
            .ValidateDataAnnotations();

        services.AddSingleton(TimeProvider.System);

        services.AddHttpClient(VoidwellTokenService.HttpClientName);
        services.AddSingleton<IVoidwellTokenService, VoidwellTokenService>();
        services.AddTransient<VoidwellAuthHandler>();

        services.AddHttpClient<VoidwellClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.voidwell.com/");
        })
        .AddHttpMessageHandler<VoidwellAuthHandler>();

        return services;
    }
}
