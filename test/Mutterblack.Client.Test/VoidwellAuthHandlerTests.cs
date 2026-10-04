using System.Net;
using Mutterblack.Client.Authentication;

namespace Mutterblack.Client.Test;

public class VoidwellAuthHandlerTests
{
    [Fact]
    public async Task SendAsync_AddsBearerToken()
    {
        var tokens = new FakeTokenService("token-1");
        var api = new StubHttpMessageHandler(HttpStatusCode.OK, "{}");
        using var client = CreateClient(tokens, api);

        using var response = await client.GetAsync("https://api.example.com/thing", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        var (request, _) = Assert.Single(api.Requests);
        Assert.Equal("Bearer", request.Headers.Authorization!.Scheme);
        Assert.Equal("token-1", request.Headers.Authorization.Parameter);
        Assert.Empty(tokens.Invalidated);
    }

    [Fact]
    public async Task SendAsync_OnUnauthorizedRetriesOnceWithNewToken()
    {
        var tokens = new FakeTokenService("old-token", "new-token");
        var api = new StubHttpMessageHandler((request, _) =>
            request.Headers.Authorization!.Parameter == "new-token"
                ? StubHttpMessageHandler.Json(HttpStatusCode.OK, "{}")
                : StubHttpMessageHandler.Json(HttpStatusCode.Unauthorized, ""));
        using var client = CreateClient(tokens, api);

        using var response = await client.GetAsync("https://api.example.com/thing", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, api.Requests.Count);
        Assert.Equal("new-token", api.Requests[1].Request.Headers.Authorization!.Parameter);
        Assert.Equal(["old-token"], tokens.Invalidated);
    }

    [Fact]
    public async Task SendAsync_DoesNotRetryMoreThanOnce()
    {
        var tokens = new FakeTokenService("token-1", "token-2", "token-3");
        var api = new StubHttpMessageHandler(HttpStatusCode.Unauthorized);
        using var client = CreateClient(tokens, api);

        using var response = await client.GetAsync("https://api.example.com/thing", TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.Unauthorized, response.StatusCode);
        Assert.Equal(2, api.Requests.Count);
    }

    [Theory]
    [InlineData(HttpStatusCode.Forbidden)]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.InternalServerError)]
    public async Task SendAsync_DoesNotRetryOtherFailures(HttpStatusCode status)
    {
        var tokens = new FakeTokenService("token-1", "token-2");
        var api = new StubHttpMessageHandler(status);
        using var client = CreateClient(tokens, api);

        using var response = await client.GetAsync("https://api.example.com/thing", TestContext.Current.CancellationToken);

        Assert.Equal(status, response.StatusCode);
        Assert.Single(api.Requests);
        Assert.Empty(tokens.Invalidated);
    }

    [Fact]
    public async Task SendAsync_RetryKeepsRequestBodyAndHeaders()
    {
        var tokens = new FakeTokenService("old-token", "new-token");
        var api = new StubHttpMessageHandler((request, _) =>
            request.Headers.Authorization!.Parameter == "new-token"
                ? StubHttpMessageHandler.Json(HttpStatusCode.OK, "{}")
                : StubHttpMessageHandler.Json(HttpStatusCode.Unauthorized, ""));
        using var client = CreateClient(tokens, api);

        using var request = new HttpRequestMessage(HttpMethod.Post, "https://api.example.com/thing")
        {
            Content = new StringContent("""{"name":"value"}""", System.Text.Encoding.UTF8, "application/json")
        };
        request.Headers.Add("X-Custom", "abc");

        using var response = await client.SendAsync(request, TestContext.Current.CancellationToken);

        Assert.Equal(HttpStatusCode.OK, response.StatusCode);
        Assert.Equal(2, api.Requests.Count);

        var retry = api.Requests[1];
        Assert.Equal(HttpMethod.Post, retry.Request.Method);
        Assert.Equal("""{"name":"value"}""", retry.Body);
        Assert.Equal("application/json", retry.Request.Content!.Headers.ContentType!.MediaType);
        Assert.Equal("abc", Assert.Single(retry.Request.Headers.GetValues("X-Custom")));
    }

    private static HttpClient CreateClient(FakeTokenService tokens, HttpMessageHandler inner) =>
        new(new VoidwellAuthHandler(tokens) { InnerHandler = inner });

    /// <summary>Hands out the given tokens in order, handing back the last one once it runs out.</summary>
    private sealed class FakeTokenService : IVoidwellTokenService
    {
        private readonly Queue<string> _tokens;
        private string _current = string.Empty;

        public FakeTokenService(params string[] tokens)
        {
            _tokens = new Queue<string>(tokens);
        }

        public List<string> Invalidated { get; } = [];

        public Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
        {
            if (_current.Length == 0 || (Invalidated.Contains(_current) && _tokens.Count > 0))
            {
                _current = _tokens.Dequeue();
            }

            return Task.FromResult(_current);
        }

        public void Invalidate(string rejectedToken)
        {
            Invalidated.Add(rejectedToken);
        }
    }
}
