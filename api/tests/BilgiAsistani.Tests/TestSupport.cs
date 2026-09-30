using BilgiAsistani.Core;
using Microsoft.Extensions.Options;

namespace BilgiAsistani.Tests;

/// <summary>FastAPI LLM servisini taklit eder; gönderilen istekleri kaydeder.</summary>
public class FakeLlmClient : ILlmClient
{
    public LlmHealth? Health { get; set; } = new("ok", true, "fake-model");
    public Func<GenerateRequest, GenerateResponse> Respond { get; set; } =
        req => new GenerateResponse(true, "yanıt", [req.Sources[0].Id], [], "fake-model");
    public List<GenerateRequest> Requests { get; } = [];

    private int _healthChecks;
    public int HealthChecks => _healthChecks;

    public Task<LlmHealth?> GetHealthAsync(CancellationToken ct)
    {
        Interlocked.Increment(ref _healthChecks);
        return Task.FromResult(Health);
    }

    public Task<GenerateResponse> GenerateAsync(GenerateRequest request, CancellationToken ct)
    {
        Requests.Add(request);
        return Task.FromResult(Respond(request));
    }
}

/// <summary>Testlerde paylaşılan doküman kümesi ve indeks (bir kez yüklenir).</summary>
public static class TestData
{
    public static readonly Corpus Corpus = Corpus.Load(Corpus.ResolveDirectory("data/documents"));
    public static readonly Bm25Index Index = new(Corpus.Chunks);
}

public static class Pipelines
{
    public static QaPipeline Create(string mode = AnswerModes.Extractive, ILlmClient? llm = null) =>
        new(Options.Create(new AssistantOptions { AnswerMode = mode }), llm ?? new FakeLlmClient { Health = null });
}
