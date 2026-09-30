using BilgiAsistani.Core;

namespace BilgiAsistani.Tests;

public class LlmPipelineTests
{
    private const string Question = "Ürün iadesinde kargo ücretini kim ödüyor?";

    [Fact]
    public async Task SupersededVersions_AreNeverSentToLlm()
    {
        var llm = new FakeLlmClient();
        await Pipelines.Create(AnswerModes.Llm, llm).AskAsync(Question);

        var sent = Assert.Single(llm.Requests).Sources;
        Assert.DoesNotContain(sent, s => s.Id.StartsWith("iade-politikasi-v1"));
        Assert.All(sent, s => Assert.Equal(Document.ActiveStatus, s.Status));
    }

    [Fact]
    public async Task LlmAnswer_KeepsOnlyKnownSourceIds_AndMapsConflictsFromMetadata()
    {
        var llm = new FakeLlmClient
        {
            Respond = req => new GenerateResponse(
                true, "HızlıKargo ile iade ücretsizdir.",
                ["iade-politikasi-v2#3", "uydurma#9"],
                [new LlmConflict("iade kargo ücreti", "iade-politikasi-v2#3", "kargo-teslimat#4", "daha yeni")],
                "fake-model"),
        };

        var r = await Pipelines.Create(AnswerModes.Llm, llm).AskAsync(Question);

        Assert.Equal(AnswerModes.Llm, r.Mode);
        Assert.Equal(["iade-politikasi-v2#3"], r.Sources.Select(s => s.ChunkId));
        var conflict = Assert.Single(r.ContentConflicts);
        Assert.Equal(new DateOnly(2025, 3, 1), conflict.Chosen.EffectiveDate);
        Assert.Equal("kargo-teslimat", conflict.Rejected.DocId);
        Assert.Equal("iade-politikasi-v1", Assert.Single(Assert.Single(r.VersionDecisions).Rejected).DocId);
    }

    [Fact]
    public async Task LlmSaysUnanswerable_ReturnsNoSources()
    {
        var llm = new FakeLlmClient { Respond = _ => new GenerateResponse(false, "Bilgi yok.", [], [], "fake-model") };

        var r = await Pipelines.Create(AnswerModes.Llm, llm).AskAsync("Nova Termo Alexa ile uyumlu mu?");

        Assert.False(r.Answerable);
        Assert.Empty(r.Sources);
        Assert.Empty(r.VersionDecisions);
    }

    [Fact]
    public async Task LowScore_SkipsLlmCall()
    {
        var llm = new FakeLlmClient();
        var r = await Pipelines.Create(AnswerModes.Llm, llm).AskAsync("Şirketin CEO'su kim?");

        Assert.False(r.Answerable);
        Assert.Empty(llm.Requests);
    }

    [Fact]
    public async Task LlmFailure_FallsBackToExtractiveWithWarning()
    {
        var llm = new FakeLlmClient { Respond = _ => throw new LlmServiceException("LLM servisine bağlanılamadı.") };

        var r = await Pipelines.Create(AnswerModes.Llm, llm).AskAsync("Garanti süresi ne kadar?");

        Assert.Equal(AnswerModes.ExtractiveFallback, r.Mode);
        Assert.True(r.Answerable);
        Assert.Contains("LLM servisine bağlanılamadı", Assert.Single(r.Warnings));
    }

    [Fact]
    public async Task LlmContext_IncludesWholeCurrentDocument_WhenSearchMissesRelevantSection()
    {
        // "Canlı Sohbet: 7 gün 24 saat" bölümü aramada ilk 5'e girmiyor; doküman genişletmesiyle LLM'e ulaşmalı.
        var llm = new FakeLlmClient();
        await Pipelines.Create(AnswerModes.Llm, llm).AskAsync("Pazar günü destek alabilir miyim?");

        var sent = Assert.Single(llm.Requests).Sources.Select(s => s.Id).ToList();
        Assert.Contains("destek-kanallari-v2#3", sent);
        Assert.DoesNotContain(sent, id => id.StartsWith("destek-kanallari-v1"));
        Assert.Equal(sent.Count, sent.Distinct().Count());
    }

    [Fact]
    public void LlmContext_ExpandsAtMostConfiguredNumberOfDocuments()
    {
        var pipeline = Pipelines.Create(AnswerModes.Llm);
        var question = "Ürün iadesinde kargo ücretini kim ödüyor?";
        var hits = VersionResolver.Resolve(pipeline.Index.Search(question, 10), question, pipeline.Index, pipeline.Corpus.Documents)
            .Hits.Take(5).ToList();

        var context = pipeline.BuildLlmContext(question, hits);

        var firstTwoDocs = hits.Select(h => h.Chunk.Doc.DocId).Distinct().Take(2).ToList();
        foreach (var docId in firstTwoDocs)
            Assert.Equal(
                pipeline.Corpus.Chunks.Where(c => c.Doc.DocId == docId).Select(c => c.ChunkId),
                context.Where(h => h.Chunk.Doc.DocId == docId).Select(h => h.Chunk.ChunkId));
        // İlk iki doküman dışındakilerden yalnızca aramada öne çıkan bölümler gelir.
        Assert.All(context.Where(h => !firstTwoDocs.Contains(h.Chunk.Doc.DocId)), h => Assert.Contains(h, hits));
        Assert.All(context, h => Assert.True(h.Chunk.Doc.IsActive));
        Assert.True(context.Count <= 20);
    }

    [Fact]
    public void LlmContext_ExpansionDisabled_SendsOnlyTopHits()
    {
        var pipeline = new QaPipeline(
            Microsoft.Extensions.Options.Options.Create(new AssistantOptions { AnswerMode = AnswerModes.Llm, ExpandedDocuments = 0 }),
            new FakeLlmClient());
        var hits = pipeline.Index.Search("garanti süresi", 3);

        Assert.Same(hits, pipeline.BuildLlmContext("garanti süresi", hits));
    }

    [Fact]
    public async Task AutoMode_CachesLlmServiceHealth_AcrossRequests()
    {
        var llm = new FakeLlmClient();
        var pipeline = Pipelines.Create(AnswerModes.Auto, llm);

        await Task.WhenAll(Enumerable.Range(0, 5).Select(_ => pipeline.GetLlmHealthAsync(CancellationToken.None)));
        await pipeline.AskAsync("Garanti süresi ne kadar?");
        await pipeline.AskAsync("Canlı sohbet desteğiniz var mı?");

        // İlk eşzamanlı çağrılar birden fazla kontrol yapabilir; önbellek dolduktan sonra servise tekrar gidilmez.
        var afterWarmup = llm.HealthChecks;
        await pipeline.AskAsync("Para iadesi kaç iş gününde yapılır?");
        Assert.InRange(afterWarmup, 1, 5);
        Assert.Equal(afterWarmup, llm.HealthChecks);
    }

    [Theory]
    [InlineData(true, AnswerModes.Llm)]
    [InlineData(false, AnswerModes.Extractive)]
    public async Task AutoMode_FollowsLlmServiceHealth(bool available, string expectedMode)
    {
        var llm = new FakeLlmClient { Health = new LlmHealth("ok", available, "fake-model") };

        var r = await Pipelines.Create(AnswerModes.Auto, llm).AskAsync("Garanti süresi ne kadar?");

        Assert.Equal(expectedMode, r.Mode);
        Assert.Empty(r.Warnings);
    }
}
