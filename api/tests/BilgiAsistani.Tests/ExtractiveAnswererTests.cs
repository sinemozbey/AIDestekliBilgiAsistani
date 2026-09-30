using BilgiAsistani.Core;

namespace BilgiAsistani.Tests;

public class ExtractiveAnswererTests
{
    private static readonly Corpus Corpus = Corpus.Load(Corpus.ResolveDirectory("data/documents"));
    private static readonly Bm25Index Index = new(Corpus.Chunks);

    private static Hit Top(string query) => Index.Search(query, topK: 1)[0];

    [Fact]
    public void Coverage_IsShareOfQuestionTermsFoundInSection()
    {
        // "garanti" ve "süre" bölümde geçiyor; tüm anlamlı terimler eşleşiyor.
        Assert.Equal(1.0, ExtractiveAnswerer.Coverage("Garanti süresi ne kadar?", Top("Garanti süresi ne kadar?")));
        // "Alexa" hiçbir dokümanda yok: "termo" eşleşse de oran düşük kalır.
        var hit = Top("Nova Termo Alexa ile uyumlu mu?");
        Assert.True(ExtractiveAnswerer.Coverage("Nova Termo Alexa ile uyumlu mu?", hit) < 0.6);
    }

    [Fact]
    public void Answer_SelectsMostRelevantSentencesInDocumentOrder()
    {
        var question = "Kaç TL üzeri siparişlerde kargo ücretsiz?";
        var result = ExtractiveAnswerer.Answer(question, [Top(question)], minCoverage: 0.6, maxSentences: 1);

        Assert.True(result.Answerable);
        Assert.Equal("500 TL ve üzeri siparişlerde kargo ücretsizdir.", result.Answer);
        Assert.Equal("kargo-teslimat#2", Assert.Single(result.Used).Chunk.ChunkId);
    }

    [Fact]
    public void Answer_BelowCoverageThreshold_IsNotAnswerable()
    {
        var question = "Nova Termo Alexa ile uyumlu mu?";
        var result = ExtractiveAnswerer.Answer(question, [Top(question)], minCoverage: 0.6);

        Assert.False(result.Answerable);
        Assert.Empty(result.Used);
        Assert.Equal("", result.Answer);
    }
}
