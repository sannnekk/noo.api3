using System.Net;
using System.Text;
using Moq;

namespace Noo.UnitTests.Auth.External;

/// <summary>
/// Replies with queued responses in order and records every request, so tests can assert
/// on the exact form body a provider sent.
/// </summary>
public class StubHttpMessageHandler : HttpMessageHandler
{
    private readonly Queue<HttpResponseMessage> _responses = new();

    public List<HttpRequestMessage> Requests { get; } = [];

    public List<string> Bodies { get; } = [];

    public StubHttpMessageHandler Enqueue(string json, HttpStatusCode status = HttpStatusCode.OK)
    {
        _responses.Enqueue(
            new HttpResponseMessage(status)
            {
                Content = new StringContent(json, Encoding.UTF8, "application/json"),
            }
        );

        return this;
    }

    public IHttpClientFactory AsFactory()
    {
        var client = new HttpClient(this);
        var factory = new Mock<IHttpClientFactory>();

        factory.Setup(f => f.CreateClient(It.IsAny<string>())).Returns(client);

        return factory.Object;
    }

    protected override async Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request,
        CancellationToken cancellationToken
    )
    {
        Requests.Add(request);
        Bodies.Add(
            request.Content is null ? string.Empty : await request.Content.ReadAsStringAsync(cancellationToken)
        );

        return _responses.Count > 0
            ? _responses.Dequeue()
            : new HttpResponseMessage(HttpStatusCode.NotImplemented);
    }
}
