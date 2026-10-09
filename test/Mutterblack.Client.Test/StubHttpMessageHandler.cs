using System.Net;

namespace Mutterblack.Client.Test;

/// <summary>Answers every request from a delegate and records what was sent.</summary>
internal sealed class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, string?, HttpResponseMessage> _respond;

    public StubHttpMessageHandler(Func<HttpRequestMessage, string?, HttpResponseMessage> respond)
    {
        _respond = respond;
    }

    public StubHttpMessageHandler(HttpStatusCode statusCode, string body = "")
        : this((_, _) => Json(statusCode, body))
    {
    }

    /// <summary>The requests received so far, with the request body read before the request was disposed.</summary>
    public List<(HttpRequestMessage Request, string? Body)> Requests { get; } = [];

    public static HttpResponseMessage Json(HttpStatusCode statusCode, string body) =>
        new(statusCode)
        {
            Content = new StringContent(body, System.Text.Encoding.UTF8, "application/json")
        };

    protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken cancellationToken)
    {
        var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(cancellationToken);

        Requests.Add((request, body));

        var response = _respond(request, body);
        response.RequestMessage = request;

        return response;
    }
}
