using System.Net.Http.Json;
using System.Text.Json;

namespace BilgiAsistani.Core;

public interface ILlmClient
{
    /// <summary>Servise ulaşılamazsa null döner.</summary>
    Task<LlmHealth?> GetHealthAsync(CancellationToken ct);

    Task<GenerateResponse> GenerateAsync(GenerateRequest request, CancellationToken ct);
}

public class LlmServiceException(string message, Exception? inner = null) : Exception(message, inner);

/// <summary>FastAPI LLM servisine (llm-service/) HTTP istemcisi.</summary>
public class LlmServiceClient(IHttpClientFactory factory) : ILlmClient
{
    public const string HttpClientName = "llm-service";
    // Sağlık kontrolü hızlı olmalı: servis yanıt vermiyorsa istek beklemeden çıkarımsal moda geçilir.
    private static readonly TimeSpan HealthTimeout = TimeSpan.FromSeconds(3);

    public static readonly JsonSerializerOptions Json = new(JsonSerializerDefaults.Web)
    {
        PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower,
    };

    public async Task<LlmHealth?> GetHealthAsync(CancellationToken ct)
    {
        try
        {
            using var cts = CancellationTokenSource.CreateLinkedTokenSource(ct);
            cts.CancelAfter(HealthTimeout);
            return await factory.CreateClient(HttpClientName).GetFromJsonAsync<LlmHealth>("health", Json, cts.Token);
        }
        catch (Exception e) when ((e is HttpRequestException or TaskCanceledException or JsonException) && !ct.IsCancellationRequested)
        {
            return null;
        }
    }

    public async Task<GenerateResponse> GenerateAsync(GenerateRequest request, CancellationToken ct)
    {
        HttpResponseMessage response;
        try
        {
            response = await factory.CreateClient(HttpClientName).PostAsJsonAsync("generate", request, Json, ct);
        }
        catch (HttpRequestException e)
        {
            throw new LlmServiceException("LLM servisine bağlanılamadı.", e);
        }
        catch (TaskCanceledException e) when (!ct.IsCancellationRequested)
        {
            throw new LlmServiceException("LLM servisi zaman aşımına uğradı.", e);
        }

        using (response)
        {
            if (!response.IsSuccessStatusCode)
            {
                var body = await response.Content.ReadAsStringAsync(ct);
                throw new LlmServiceException(
                    $"LLM servisi {(int)response.StatusCode} döndürdü: {(body.Length > 300 ? body[..300] + "…" : body)}");
            }
            try
            {
                return await response.Content.ReadFromJsonAsync<GenerateResponse>(Json, ct)
                       ?? throw new LlmServiceException("LLM servisi boş yanıt döndürdü.");
            }
            catch (JsonException e)
            {
                throw new LlmServiceException("LLM servisinin yanıtı beklenen biçimde değil.", e);
            }
        }
    }
}
