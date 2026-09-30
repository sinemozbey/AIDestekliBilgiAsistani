using BilgiAsistani.Core;

namespace BilgiAsistani.Tests;

public class SearchTests
{
    private static readonly Corpus Corpus = TestData.Corpus;
    private static readonly Bm25Index Index = TestData.Index;

    [Fact]
    public void Corpus_ParsesFrontMatterAndSplitsSections()
    {
        Assert.Equal(10, Corpus.Documents.Count);
        var v2 = Corpus.Documents.Single(d => d.DocId == "iade-politikasi-v2");
        Assert.Equal("iade-politikasi", v2.Family);
        Assert.Equal(new DateOnly(2025, 3, 1), v2.EffectiveDate);
        Assert.Equal("iade-politikasi-v1", v2.Supersedes);
        Assert.True(v2.IsActive);
        Assert.Equal("İade Süresi", Corpus.Chunks.Single(c => c.ChunkId == "iade-politikasi-v2#2").Section);
    }

    [Theory]
    [InlineData("Cihazların garanti süresi ne kadar?", "garanti-kosullari#1")]
    [InlineData("Termostatı fabrika ayarlarına nasıl sıfırlarım?", "sorun-giderme#2")]
    [InlineData("Şifre sıfırlama bağlantısı ne kadar süre geçerli?", "hesap-uyelik#1")]
    public void Search_RanksRelevantSectionFirst(string query, string expectedChunk) =>
        Assert.Equal(expectedChunk, Index.Search(query, topK: 1)[0].Chunk.ChunkId);

    [Fact]
    public void Search_MatchesInflectedForms()
    {
        // "iadesinde" ile "iade", "ücretini" ile "ücreti" aynı köke eşlenmeli.
        var hit = Index.Search("iadesinde kargo ücretini", topK: 5)
            .First(h => h.Chunk.ChunkId == "iade-politikasi-v2#3");
        Assert.Equal(3, hit.MatchedTerms.Count);
    }

    [Fact]
    public void Search_CanBeRestrictedToOneDocument() =>
        Assert.All(Index.Search("iade süresi", topK: 10, docId: "iade-politikasi-v1"),
            h => Assert.Equal("iade-politikasi-v1", h.Chunk.Doc.DocId));

    [Fact]
    public void Search_UnrelatedQueryReturnsNothing() =>
        Assert.Empty(Index.Search("Amazon Alexa"));
}
