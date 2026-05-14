using System.Net;
using System.Text;

namespace OrderFlow.Tests.Helpers;

/// <summary>
/// Kontrolowalny HttpMessageHandler do użycia w testach zamiast prawdziwego HTTP.
/// </summary>
public class TestHttpMessageHandler : HttpMessageHandler
{
    private readonly Func<HttpRequestMessage, HttpResponseMessage> _responder;

    public List<HttpRequestMessage> SentRequests { get; } = new();

    public TestHttpMessageHandler(Func<HttpRequestMessage, HttpResponseMessage> responder)
    {
        _responder = responder;
    }

    public static TestHttpMessageHandler RespondWithJson(string json,
        HttpStatusCode status = HttpStatusCode.OK)
        => new(_ => new HttpResponseMessage(status)
        {
            Content = new StringContent(json, Encoding.UTF8, "application/json")
        });

    public static TestHttpMessageHandler RespondWith(HttpStatusCode status)
        => new(_ => new HttpResponseMessage(status));

    protected override Task<HttpResponseMessage> SendAsync(
        HttpRequestMessage request, CancellationToken cancellationToken)
    {
        SentRequests.Add(request);
        return Task.FromResult(_responder(request));
    }

    public static string BuildNbpJson(string code, decimal mid) =>
        $$"""{"table":"A","currency":"test","code":"{{code}}","rates":[{"no":"001/A/NBP/2026","effectiveDate":"2026-05-13","mid":{{mid.ToString(System.Globalization.CultureInfo.InvariantCulture)}}}]}""";
}
