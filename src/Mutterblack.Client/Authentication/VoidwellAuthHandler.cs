using System.Net;
using System.Net.Http.Headers;

namespace Mutterblack.Client.Authentication;

/// <summary>Adds the bearer token to outgoing requests and retries once with a new token if the API rejects it.</summary>
internal sealed class VoidwellAuthHandler : DelegatingHandler
{
    private readonly IVoidwellTokenService _tokenService;

    public VoidwellAuthHandler(IVoidwellTokenService tokenService)
    {
        _tokenService = tokenService;
    }

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        using var retryRequest = await CloneAsync(request, cancellationToken);

        var token = await _tokenService.GetTokenAsync(cancellationToken);
        request.Headers.Authorization = new AuthenticationHeaderValue("Bearer", token);

        var response = await base.SendAsync(request, cancellationToken);

        if (response.StatusCode != HttpStatusCode.Unauthorized)
        {
            return response;
        }

        response.Dispose();
        _tokenService.Invalidate(token);

        var newToken = await _tokenService.GetTokenAsync(cancellationToken);
        retryRequest.Headers.Authorization = new AuthenticationHeaderValue("Bearer", newToken);

        return await base.SendAsync(retryRequest, cancellationToken);
    }

    private static async Task<HttpRequestMessage> CloneAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var clone = new HttpRequestMessage(request.Method, request.RequestUri)
        {
            Version = request.Version,
            VersionPolicy = request.VersionPolicy
        };

        foreach (var header in request.Headers)
        {
            clone.Headers.TryAddWithoutValidation(header.Key, header.Value);
        }

        if (request.Content is not null)
        {
            var content = new ByteArrayContent(await request.Content.ReadAsByteArrayAsync(cancellationToken));

            foreach (var header in request.Content.Headers)
            {
                content.Headers.TryAddWithoutValidation(header.Key, header.Value);
            }

            clone.Content = content;
        }

        return clone;
    }
}
