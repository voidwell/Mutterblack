using System.Net.Http.Json;
using System.Text.Json.Serialization;
using Microsoft.Extensions.Options;

namespace Mutterblack.Client.Authentication;

/// <summary>Obtains client-credentials access tokens from the OIDC provider and reuses them until they expire.</summary>
internal sealed class VoidwellTokenService : IVoidwellTokenService
{
    internal const string HttpClientName = nameof(VoidwellTokenService);

    private static readonly TimeSpan _expiryBuffer = TimeSpan.FromSeconds(30);

    private readonly IHttpClientFactory _httpClientFactory;
    private readonly VoidwellClientOptions _options;
    private readonly TimeProvider _timeProvider;
    private readonly SemaphoreSlim _lock = new(1, 1);

    private string? _token;
    private DateTimeOffset _expiresAt;

    public VoidwellTokenService(IHttpClientFactory httpClientFactory, IOptions<VoidwellClientOptions> options, TimeProvider timeProvider)
    {
        _httpClientFactory = httpClientFactory;
        _options = options.Value;
        _timeProvider = timeProvider;
    }

    public async Task<string> GetTokenAsync(CancellationToken cancellationToken = default)
    {
        if (TryGetCachedToken(out var cached))
        {
            return cached;
        }

        await _lock.WaitAsync(cancellationToken);
        try
        {
            // Another caller may have refreshed the token while we waited.
            if (TryGetCachedToken(out cached))
            {
                return cached;
            }

            var response = await RequestTokenAsync(cancellationToken);

            _expiresAt = _timeProvider.GetUtcNow().AddSeconds(response.ExpiresIn);
            _token = response.AccessToken;

            return response.AccessToken;
        }
        finally
        {
            _lock.Release();
        }
    }

    public void Invalidate(string rejectedToken)
    {
        _lock.Wait();
        try
        {
            // Only clear it if nobody has already replaced the rejected token.
            if (_token == rejectedToken)
            {
                _token = null;
            }
        }
        finally
        {
            _lock.Release();
        }
    }

    private bool TryGetCachedToken(out string token)
    {
        var current = _token;
        if (current is not null && _timeProvider.GetUtcNow() < _expiresAt - _expiryBuffer)
        {
            token = current;
            return true;
        }

        token = string.Empty;
        return false;
    }

    private async Task<TokenResponse> RequestTokenAsync(CancellationToken cancellationToken)
    {
        using var request = new HttpRequestMessage(HttpMethod.Post, _options.TokenServiceAddress)
        {
            Content = new FormUrlEncodedContent(new Dictionary<string, string>
            {
                ["grant_type"] = "client_credentials",
                ["client_id"] = _options.ClientId,
                ["client_secret"] = _options.ClientSecret,
                ["scope"] = string.Join(' ', _options.ClientScopes)
            })
        };

        var httpClient = _httpClientFactory.CreateClient(HttpClientName);
        using var response = await httpClient.SendAsync(request, cancellationToken);

        if (!response.IsSuccessStatusCode)
        {
            throw new VoidwellException($"Failed to get an access token: {(int)response.StatusCode} {response.ReasonPhrase}");
        }

        var token = await response.Content.ReadFromJsonAsync<TokenResponse>(cancellationToken);

        if (token is null || string.IsNullOrEmpty(token.AccessToken))
        {
            throw new VoidwellException("The token service returned no access token.");
        }

        return token;
    }

    private sealed class TokenResponse
    {
        [JsonPropertyName("access_token")]
        public string AccessToken { get; set; } = string.Empty;

        [JsonPropertyName("expires_in")]
        public int ExpiresIn { get; set; }
    }
}
