using BilgiAsistani.Core;

namespace BilgiAsistani.Tests;

public class PipelineTests
{
    private static readonly QaPipeline Extractive = Pipelines.Create();

    [Fact]
    public void Corpus_LoadsAllDocumentsWithVersionMetadata()
    {
        Assert.Equal(10, Extractive.Corpus.Documents.Count);
        Assert.Equal(["destek-kanallari-v1", "iade-politikasi-v1"],
            Extractive.Corpus.Documents.Where(d => !d.IsActive).Select(d => d.DocId).Order());
    }

    [Fact]
    public void CurrentVersion_PrefersActiveThenNewest()
    {
        var docs = Extractive.Corpus.Documents;
        Assert.Equal("iade-politikasi-v2", VersionResolver.CurrentVersion("iade-politikasi", docs).DocId);
        Assert.Equal("destek-kanallari-v2", VersionResolver.CurrentVersion("destek-kanallari", docs).DocId);
    }

    [Fact]
    public async Task SupersededVersion_IsReplacedByCurrentAndExplained()
    {
        var r = await Extractive.AskAsync("Ürünü kaç gün içinde iade edebilirim?");

        Assert.True(r.Answerable);
        Assert.Equal(["iade-politikasi-v2"], r.Sources.Select(s => s.DocId));
        Assert.Contains("30 gün", r.Answer);
        var decision = Assert.Single(r.VersionDecisions);
        Assert.Equal("iade-politikasi-v2", decision.Selected.DocId);
        Assert.Equal("iade-politikasi-v1", Assert.Single(decision.Rejected).DocId);
    }

    [Fact]
    public async Task OldVersionRankedFirst_StillResolvesToCurrent()
    {
        // Aramada eski sürüm ("canlı sohbet sunulmamaktadır") öne çıksa da güncel sürüm kullanılmalı.
        var r = await Extractive.AskAsync("Canlı sohbet desteğiniz var mı?");

        Assert.StartsWith("destek-kanallari-v1", r.Candidates[0].ChunkId);
        Assert.Equal("destek-kanallari-v2", r.Sources[0].DocId);
    }

    [Theory]
    [InlineData("Nova Termo, Amazon Alexa ile uyumlu mu?")]
    [InlineData("Şirketin CEO'su kim?")]
    public async Task UnanswerableQuestion_ReturnsNoInfoWithoutSources(string question)
    {
        var r = await Extractive.AskAsync(question);

        Assert.False(r.Answerable);
        Assert.Equal(QaPipeline.NoInfoAnswer, r.Answer);
        Assert.Empty(r.Sources);
    }

    [Fact]
    public async Task UnrelatedVersionDecisions_AreNotShown()
    {
        // Adaylar arasında iade sürümleri olsa da yanıt yalnızca abonelik dokümanına dayanıyor.
        var r = await Extractive.AskAsync("Nova Plus aboneliğimi iptal edersem kalan süre için para iadesi alabilir miyim?");

        Assert.Equal("nova-plus-abonelik", r.Sources[0].DocId);
        Assert.Empty(r.VersionDecisions);
    }
}
