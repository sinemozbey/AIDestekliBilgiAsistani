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

    public Task<LlmHealth?> GetHealthAsync(CancellationToken ct) => Task.FromResult(Health);

    public Task<GenerateResponse> GenerateAsync(GenerateRequest request, CancellationToken ct)
    {
        Requests.Add(request);
        return Task.FromResult(Respond(request));
    }
}

public static class Pipelines
{
    public static QaPipeline Create(string mode = "extractive", ILlmClient? llm = null) =>
        new(Options.Create(new AssistantOptions { AnswerMode = mode }), llm ?? new FakeLlmClient { Health = null });
}
