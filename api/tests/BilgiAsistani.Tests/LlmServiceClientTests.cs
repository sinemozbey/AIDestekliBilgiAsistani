using System.Net;
using System.Text;
using System.Text.Json;
using BilgiAsistani.Core;

namespace BilgiAsistani.Tests;

/// <summary>FastAPI LLM servisiyle HTTP sözleşmesi: istek biçimi, yanıtın okunması ve hata durumları.</summary>
public class LlmServiceClientTests
{
    private sealed class StubHandler(Func<HttpRequestMessage, HttpResponseMessage> respond) : HttpMessageHandler
    {
        public List<(string Method, string Path, string? Body)> Requests { get; } = [];

        protected override async Task<HttpResponseMessage> SendAsync(HttpRequestMessage request, CancellationToken ct)
        {
            var body = request.Content is null ? null : await request.Content.ReadAsStringAsync(ct);
            Requests.Add((request.Method.Method, request.RequestUri!.AbsolutePath, body));
            return respond(request);
        }
    }

    private sealed class StubFactory(HttpMessageHandler handler) : IHttpClientFactory
    {
        public HttpClient CreateClient(string name) =>
            new(handler, disposeHandler: false) { BaseAddress = new Uri("http://llm-service.test/") };
    }

    private static (LlmServiceClient Client, StubHandler Handler) Create(Func<HttpRequestMessage, HttpResponseMessage> respond)
    {
        var handler = new StubHandler(respond);
        return (new LlmServiceClient(new StubFactory(handler)), handler);
    }

    private static HttpResponseMessage Json(string json, HttpStatusCode status = HttpStatusCode.OK) =>
        new(status) { Content = new StringContent(json, Encoding.UTF8, "application/json") };

    private static readonly GenerateRequest Request = new("İade süresi nedir?",
    [
        new LlmSource("iade-politikasi-v2#2", "İade ve Değişim Politikası", "2.0", new DateOnly(2025, 3, 1),
            "yururlukte", "İade Süresi", "Müşteriler ürünü 30 gün içinde iade edebilir."),
    ]);

    [Fact]
    public async Task Generate_PostsSnakeCaseRequest_AndParsesResponse()
    {
        var (client, handler) = Create(_ => Json("""
            {"answerable": true, "answer": "30 gün.", "used_source_ids": ["iade-politikasi-v2#2"],
             "conflicts": [{"topic": "t", "chosen_source_id": "a", "rejected_source_id": "b", "reason": "r"}],
             "model": "claude-opus-5-5"}
            """));

        var response = await client.GenerateAsync(Request, CancellationToken.None);

        var (method, path, body) = Assert.Single(handler.Requests);
        Assert.Equal(("POST", "/generate"), (method, path));
        var source = JsonDocument.Parse(body!).RootElement.GetProperty("sources")[0];
        Assert.Equal("2025-03-01", source.GetProperty("effective_date").GetString());
        Assert.Equal("İade Süresi", source.GetProperty("section").GetString());

        Assert.True(response.Answerable);
        Assert.Equal(["iade-politikasi-v2#2"], response.UsedSourceIds);
        Assert.Equal("b", Assert.Single(response.Conflicts).RejectedSourceId);
        Assert.Equal("claude-opus-5-5", response.Model);
    }

    [Theory]
    [InlineData(HttpStatusCode.ServiceUnavailable, "503")]
    [InlineData(HttpStatusCode.BadGateway, "502")]
    public async Task Generate_ErrorStatus_ThrowsLlmServiceException(HttpStatusCode status, string expectedInMessage)
    {
        var (client, _) = Create(_ => Json("""{"detail": "ANTHROPIC_API_KEY tanımlı değil"}""", status));

        var ex = await Assert.ThrowsAsync<LlmServiceException>(() => client.GenerateAsync(Request, CancellationToken.None));

        Assert.Contains(expectedInMessage, ex.Message);
    }

    [Fact]
    public async Task Generate_ConnectionFailure_ThrowsLlmServiceException()
    {
        var (client, _) = Create(_ => throw new HttpRequestException("connection refused"));

        var ex = await Assert.ThrowsAsync<LlmServiceException>(() => client.GenerateAsync(Request, CancellationToken.None));

        Assert.Contains("bağlanılamadı", ex.Message);
    }

    [Fact]
    public async Task Generate_MalformedJson_ThrowsLlmServiceException()
    {
        var (client, _) = Create(_ => Json("<html>proxy error</html>"));

        await Assert.ThrowsAsync<LlmServiceException>(() => client.GenerateAsync(Request, CancellationToken.None));
    }

    [Fact]
    public async Task Health_ParsesUnavailabilityReason()
    {
        var (client, _) = Create(_ => Json(
            """{"status": "ok", "llm_available": false, "model": "claude-opus-5-5", "reason": "ANTHROPIC_API_KEY geçersiz"}"""));

        var health = await client.GetHealthAsync(CancellationToken.None);

        Assert.False(health!.LlmAvailable);
        Assert.Equal("ANTHROPIC_API_KEY geçersiz", health.Reason);
    }

    [Fact]
    public async Task Health_ParsesAvailability_AndReturnsNullWhenUnreachable()
    {
        var (up, _) = Create(_ => Json("""{"status": "ok", "llm_available": true, "model": "claude-opus-5-5"}"""));
        var (down, _) = Create(_ => throw new HttpRequestException("connection refused"));

        var health = await up.GetHealthAsync(CancellationToken.None);

        Assert.True(health!.LlmAvailable);
        Assert.Null(await down.GetHealthAsync(CancellationToken.None));
    }
}
