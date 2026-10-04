using System.Net;
using Microsoft.Extensions.Options;
using Microsoft.Extensions.Time.Testing;
using Mutterblack.Client.Authentication;

namespace Mutterblack.Client.Test;

public class VoidwellTokenServiceTests
{
    private const string _tokenAddress = "https://auth.example.com/connect/token";

    private readonly FakeTimeProvider _time = new();

    [Fact]
    public async Task GetTokenAsync_RequestsClientCredentialsToken()
    {
        var handler = TokenHandler("token-1", 3600);
        var service = CreateService(handler);

        var token = await service.GetTokenAsync(TestContext.Current.CancellationToken);

        Assert.Equal("token-1", token);

        var (request, body) = Assert.Single(handler.Requests);
        Assert.Equal(HttpMethod.Post, request.Method);
        Assert.Equal(_tokenAddress, request.RequestUri!.ToString());

        var form = ParseForm(body);
        Assert.Equal("client_credentials", form["grant_type"]);
        Assert.Equal("my-client", form["client_id"]);
        Assert.Equal("my-secret", form["client_secret"]);
        Assert.Equal("scope-a scope-b", form["scope"]);
    }

    [Fact]
    public async Task GetTokenAsync_ReusesTokenUntilItExpires()
    {
        var handler = TokenHandler("token", 3600);
        var service = CreateService(handler);

        await service.GetTokenAsync(TestContext.Current.CancellationToken);
        _time.Advance(TimeSpan.FromMinutes(30));
        await service.GetTokenAsync(TestContext.Current.CancellationToken);

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetTokenAsync_RequestsNewTokenOnceExpired()
    {
        var count = 0;
        var handler = new StubHttpMessageHandler((_, _) =>
            StubHttpMessageHandler.Json(HttpStatusCode.OK, TokenJson($"token-{++count}", 3600)));
        var service = CreateService(handler);

        var first = await service.GetTokenAsync(TestContext.Current.CancellationToken);
        _time.Advance(TimeSpan.FromSeconds(3600));
        var second = await service.GetTokenAsync(TestContext.Current.CancellationToken);

        Assert.Equal("token-1", first);
        Assert.Equal("token-2", second);
    }

    [Fact]
    public async Task GetTokenAsync_RefreshesSlightlyBeforeExpiry()
    {
        var handler = TokenHandler("token", 3600);
        var service = CreateService(handler);

        await service.GetTokenAsync(TestContext.Current.CancellationToken);

        // Inside the 30 second buffer: the token is technically valid, but about to expire.
        _time.Advance(TimeSpan.FromSeconds(3580));
        await service.GetTokenAsync(TestContext.Current.CancellationToken);

        Assert.Equal(2, handler.Requests.Count);
    }

    [Fact]
    public async Task Invalidate_ForcesNewTokenOnNextCall()
    {
        var count = 0;
        var handler = new StubHttpMessageHandler((_, _) =>
            StubHttpMessageHandler.Json(HttpStatusCode.OK, TokenJson($"token-{++count}", 3600)));
        var service = CreateService(handler);

        var first = await service.GetTokenAsync(TestContext.Current.CancellationToken);
        service.Invalidate(first);
        var second = await service.GetTokenAsync(TestContext.Current.CancellationToken);

        Assert.Equal("token-1", first);
        Assert.Equal("token-2", second);
    }

    [Fact]
    public async Task Invalidate_IgnoresTokenThatWasAlreadyReplaced()
    {
        var handler = TokenHandler("token", 3600);
        var service = CreateService(handler);

        await service.GetTokenAsync(TestContext.Current.CancellationToken);
        service.Invalidate("some-older-token");
        await service.GetTokenAsync(TestContext.Current.CancellationToken);

        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetTokenAsync_ConcurrentCallsShareOneRequest()
    {
        var handler = TokenHandler("token", 3600);
        var service = CreateService(handler);

        var tokens = await Task.WhenAll(Enumerable.Range(0, 10).Select(_ => service.GetTokenAsync(TestContext.Current.CancellationToken)));

        Assert.All(tokens, t => Assert.Equal("token", t));
        Assert.Single(handler.Requests);
    }

    [Fact]
    public async Task GetTokenAsync_ThrowsWhenTokenServiceRejectsRequest()
    {
        var service = CreateService(new StubHttpMessageHandler(HttpStatusCode.BadRequest));

        var ex = await Assert.ThrowsAsync<VoidwellException>(() => service.GetTokenAsync(TestContext.Current.CancellationToken));

        Assert.Contains("400", ex.Message);
    }

    [Fact]
    public async Task GetTokenAsync_ThrowsWhenResponseHasNoAccessToken()
    {
        var service = CreateService(new StubHttpMessageHandler(HttpStatusCode.OK, "{}"));

        await Assert.ThrowsAsync<VoidwellException>(() => service.GetTokenAsync(TestContext.Current.CancellationToken));
    }

    [Fact]
    public async Task GetTokenAsync_RetriesAfterFailure()
    {
        var succeed = false;
        var handler = new StubHttpMessageHandler((_, _) => succeed
            ? StubHttpMessageHandler.Json(HttpStatusCode.OK, TokenJson("token", 3600))
            : StubHttpMessageHandler.Json(HttpStatusCode.InternalServerError, ""));
        var service = CreateService(handler);

        await Assert.ThrowsAsync<VoidwellException>(() => service.GetTokenAsync(TestContext.Current.CancellationToken));
        succeed = true;

        Assert.Equal("token", await service.GetTokenAsync(TestContext.Current.CancellationToken));
    }

    private VoidwellTokenService CreateService(StubHttpMessageHandler handler)
    {
        var options = Options.Create(new VoidwellClientOptions
        {
            TokenServiceAddress = _tokenAddress,
            ClientId = "my-client",
            ClientSecret = "my-secret",
            ClientScopes = ["scope-a", "scope-b"]
        });

        return new VoidwellTokenService(new StubHttpClientFactory(handler), options, _time);
    }

    private static StubHttpMessageHandler TokenHandler(string token, int expiresIn) =>
        new(HttpStatusCode.OK, TokenJson(token, expiresIn));

    private static string TokenJson(string token, int expiresIn) =>
        $$"""{"access_token":"{{token}}","token_type":"Bearer","expires_in":{{expiresIn}}}""";

    private static Dictionary<string, string> ParseForm(string? body) =>
        body!.Split('&')
            .Select(pair => pair.Split('='))
            .ToDictionary(parts => Uri.UnescapeDataString(parts[0]), parts => Uri.UnescapeDataString(parts[1].Replace('+', ' ')));

    private sealed class StubHttpClientFactory : IHttpClientFactory
    {
        private readonly HttpMessageHandler _handler;

        public StubHttpClientFactory(HttpMessageHandler handler)
        {
            _handler = handler;
        }

        public HttpClient CreateClient(string name) => new(_handler, disposeHandler: false);
    }
}
