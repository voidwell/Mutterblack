using System.Net;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Options;
using Mutterblack.Client.Authentication;

namespace Mutterblack.Client.Test;

public class ServiceCollectionExtensionsTests
{
    private static readonly Dictionary<string, string?> _validSettings = new()
    {
        ["Voidwell:TokenServiceAddress"] = "https://auth.example.com/connect/token",
        ["Voidwell:ClientId"] = "my-client",
        ["Voidwell:ClientSecret"] = "my-secret"
    };

    [Fact]
    public void AddVoidwellClient_BindsOptionsFromVoidwellSection()
    {
        var settings = new Dictionary<string, string?>(_validSettings)
        {
            ["Voidwell:ClientScopes:0"] = "scope-a",
            ["Voidwell:ClientScopes:1"] = "scope-b"
        };
        using var provider = BuildProvider(settings);

        var options = provider.GetRequiredService<IOptions<VoidwellClientOptions>>().Value;

        Assert.Equal("https://auth.example.com/connect/token", options.TokenServiceAddress);
        Assert.Equal("my-client", options.ClientId);
        Assert.Equal("my-secret", options.ClientSecret);
        Assert.Contains("scope-a", options.ClientScopes);
        Assert.Contains("scope-b", options.ClientScopes);
    }

    [Fact]
    public void AddVoidwellClient_DefaultsToDaybreakGamesScope()
    {
        using var provider = BuildProvider(_validSettings);

        var options = provider.GetRequiredService<IOptions<VoidwellClientOptions>>().Value;

        Assert.Equal(["voidwell-daybreakgames"], options.ClientScopes);
    }

    [Theory]
    [InlineData("Voidwell:TokenServiceAddress")]
    [InlineData("Voidwell:ClientId")]
    [InlineData("Voidwell:ClientSecret")]
    public void AddVoidwellClient_RejectsMissingRequiredSetting(string missingKey)
    {
        var settings = new Dictionary<string, string?>(_validSettings);
        settings.Remove(missingKey);
        using var provider = BuildProvider(settings);

        var ex = Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<VoidwellClientOptions>>().Value);

        Assert.Contains(missingKey, Assert.Single(ex.Failures));
    }

    [Fact]
    public void AddVoidwellClient_RejectsEmptyScopes()
    {
        var settings = new Dictionary<string, string?>(_validSettings);
        using var provider = BuildProvider(settings, options => options.ClientScopes = []);

        Assert.Throws<OptionsValidationException>(() => provider.GetRequiredService<IOptions<VoidwellClientOptions>>().Value);
    }

    [Fact]
    public void AddVoidwellClient_SharesOneTokenServiceAcrossResolutions()
    {
        using var provider = BuildProvider(_validSettings);

        Assert.Same(
            provider.GetRequiredService<IVoidwellTokenService>(),
            provider.GetRequiredService<IVoidwellTokenService>());
    }

    [Fact]
    public async Task VoidwellClient_AuthenticatesRequestsAndReusesTheTokenAcrossCalls()
    {
        var tokenEndpoint = new StubHttpMessageHandler(HttpStatusCode.OK, """{"access_token":"token-1","expires_in":3600}""");
        var api = new StubHttpMessageHandler(HttpStatusCode.OK, """{"name":"MSW-R"}""");

        var services = CreateServices(_validSettings);
        services.AddHttpClient(VoidwellTokenService.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => tokenEndpoint);
        services.AddHttpClient<VoidwellClient>().ConfigurePrimaryHttpMessageHandler(() => api);
        await using var provider = services.BuildServiceProvider();

        var client = provider.GetRequiredService<VoidwellClient>();
        await client.GetPlanetsideWeaponStatsAsync("msw");
        await client.GetPlanetsideWeaponStatsAsync("msw");

        Assert.Single(tokenEndpoint.Requests);
        Assert.Equal(2, api.Requests.Count);
        Assert.All(api.Requests, r => Assert.Equal("token-1", r.Request.Headers.Authorization!.Parameter));
        Assert.Equal("https://api.voidwell.com/ps2/weaponinfo/byname/msw", api.Requests[0].Request.RequestUri!.ToString());
    }

    [Fact]
    public async Task VoidwellClient_GetsNewTokenWhenApiRejectsTheCachedOne()
    {
        var issued = 0;
        var tokenEndpoint = new StubHttpMessageHandler((_, _) =>
            StubHttpMessageHandler.Json(HttpStatusCode.OK, $$"""{"access_token":"token-{{++issued}}","expires_in":3600}"""));
        var api = new StubHttpMessageHandler((request, _) =>
            request.Headers.Authorization!.Parameter == "token-2"
                ? StubHttpMessageHandler.Json(HttpStatusCode.OK, """{"name":"MSW-R"}""")
                : StubHttpMessageHandler.Json(HttpStatusCode.Unauthorized, ""));

        var services = CreateServices(_validSettings);
        services.AddHttpClient(VoidwellTokenService.HttpClientName).ConfigurePrimaryHttpMessageHandler(() => tokenEndpoint);
        services.AddHttpClient<VoidwellClient>().ConfigurePrimaryHttpMessageHandler(() => api);
        await using var provider = services.BuildServiceProvider();

        var result = await provider.GetRequiredService<VoidwellClient>().GetPlanetsideWeaponStatsAsync("msw");

        Assert.Equal("MSW-R", result.Name);
        Assert.Equal(2, tokenEndpoint.Requests.Count);
    }

    private static ServiceProvider BuildProvider(Dictionary<string, string?> settings, Action<VoidwellClientOptions>? configure = null)
    {
        var services = CreateServices(settings);

        if (configure is not null)
        {
            services.PostConfigure(configure);
        }

        return services.BuildServiceProvider();
    }

    private static ServiceCollection CreateServices(Dictionary<string, string?> settings)
    {
        var configuration = new ConfigurationBuilder().AddInMemoryCollection(settings).Build();

        var services = new ServiceCollection();
        services.AddSingleton<IConfiguration>(configuration);
        services.AddVoidwellClient();

        return services;
    }
}
