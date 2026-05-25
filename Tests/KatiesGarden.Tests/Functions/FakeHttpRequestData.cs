using Microsoft.Azure.Functions.Worker;
using Microsoft.Azure.Functions.Worker.Http;
using System.Net;
using System.Security.Claims;
using System.Text.Json;

namespace KatiesGarden.Tests.Functions;

internal sealed class FakeHttpRequestData : HttpRequestData
{
    private readonly Stream _body;

    public FakeHttpRequestData(FunctionContext context, object? body = null)
        : base(context)
    {
        _body = body is null
            ? Stream.Null
            : new MemoryStream(JsonSerializer.SerializeToUtf8Bytes(body));
    }

    public override Stream Body => _body;
    public override HttpHeadersCollection Headers => new();
    public override IReadOnlyCollection<IHttpCookie> Cookies => Array.Empty<IHttpCookie>();
    public override Uri Url => new("https://localhost/api/contact");
    public override IEnumerable<ClaimsIdentity> Identities => Array.Empty<ClaimsIdentity>();
    public override string Method => "POST";

    public override HttpResponseData CreateResponse() =>
        new FakeHttpResponseData(FunctionContext);
}

internal sealed class FakeHttpResponseData : HttpResponseData
{
    public FakeHttpResponseData(FunctionContext context) : base(context) { }

    public override HttpStatusCode StatusCode { get; set; }
    public override HttpHeadersCollection Headers { get; set; } = new();
    public override Stream Body { get; set; } = new MemoryStream();
    public override HttpCookies Cookies => null!;
}
