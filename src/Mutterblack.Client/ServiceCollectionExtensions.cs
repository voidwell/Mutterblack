using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Voidwell.Common.Authentication;

namespace Mutterblack.Client;

public static class ServiceCollectionExtensions
{
    public static IServiceCollection AddVoidwellClient(this IServiceCollection services)
    {
        services.AddOptionsWithValidateOnStart<VoidwellClientOptions>()
            .BindConfiguration("Voidwell")
            .ValidateDataAnnotations();

        services.AddHttpClient<VoidwellClient>(client =>
        {
            client.BaseAddress = new Uri("https://api.voidwell.com/");
        })
        .AddTokenHandler((serviceProvider, options) =>
        {
            var clientOptions = serviceProvider.GetRequiredService<IOptions<VoidwellClientOptions>>().Value;

            options.TokenServiceAddress = clientOptions.TokenServiceAddress;
            options.ClientId = clientOptions.ClientId;
            options.ClientSecret = clientOptions.ClientSecret;
            options.ClientScopes = clientOptions.ClientScopes;
        });

        return services;
    }
}
