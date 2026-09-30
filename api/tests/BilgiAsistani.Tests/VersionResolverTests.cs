using BilgiAsistani.Core;

namespace BilgiAsistani.Tests;

public class VersionResolverTests
{
    private static readonly Corpus Corpus = TestData.Corpus;
    private static readonly Bm25Index Index = TestData.Index;

    private static (List<Hit> Hits, List<VersionDecision> Decisions) Resolve(string query) =>
        VersionResolver.Resolve(Index.Search(query, topK: 10), query, Index, Corpus.Documents);

    [Theory]
    [InlineData("iade-politikasi", "iade-politikasi-v2")]
    [InlineData("destek-kanallari", "destek-kanallari-v2")]
    [InlineData("garanti", "garanti-kosullari")]
    public void CurrentVersion_PrefersActiveThenNewest(string family, string expected) =>
        Assert.Equal(expected, VersionResolver.CurrentVersion(family, Corpus.Documents).DocId);

    [Fact]
    public void CurrentVersion_PrefersActiveOverNewerSuperseded()
    {
        var active = new Document("a", "A", "f", "1.0", new DateOnly(2024, 1, 1), "yururlukte", null);
        var newerButRetired = new Document("b", "A", "f", "2.0", new DateOnly(2025, 1, 1), "yururlukten_kalkti", null);

        Assert.Equal("a", VersionResolver.CurrentVersion("f", [active, newerButRetired]).DocId);
    }

    [Fact]
    public void Resolve_RemovesSupersededSectionsAndExplainsDecision()
    {
        var (hits, decisions) = Resolve("Ürünü kaç gün içinde iade edebilirim?");

        Assert.DoesNotContain(hits, h => !h.Chunk.Doc.IsActive);
        Assert.Equal("iade-politikasi-v2#2", hits[0].Chunk.ChunkId);

        var decision = Assert.Single(decisions);
        Assert.Equal("iade-politikasi-v2", decision.Selected.DocId);
        var rejected = Assert.Single(decision.Rejected);
        Assert.Equal("iade-politikasi-v1", rejected.DocId);
        Assert.Contains("14 gün", rejected.Excerpt);
        Assert.Contains("v2.0 (yürürlük: 2025-03-01, yürürlükte) seçildi", decision.Explanation);
        Assert.Equal(VersionResolver.Rule, decision.Rule);
    }

    [Fact]
    public void Resolve_OldVersionRankedFirst_IsReplacedByCurrentSection()
    {
        // v1 "canlı sohbet sunulmamaktadır" aramada v2'nin önüne geçiyor.
        var raw = Index.Search("Canlı sohbet desteğiniz var mı?", topK: 10);
        Assert.StartsWith("destek-kanallari-v1", raw[0].Chunk.ChunkId);

        var (hits, _) = Resolve("Canlı sohbet desteğiniz var mı?");

        Assert.Equal("destek-kanallari-v2#3", hits[0].Chunk.ChunkId);
        // Yerine geçen bölüm, eski sürümün skorunu devralır; sıralamada geri düşmez.
        Assert.Equal(raw[0].Score, hits[0].Score);
    }

    [Fact]
    public void Resolve_SingleVersionFamilies_AreUntouched()
    {
        var (hits, decisions) = Resolve("Cihazların garanti süresi ne kadar?");

        Assert.Equal("garanti-kosullari#1", hits[0].Chunk.ChunkId);
        Assert.DoesNotContain(decisions, d => d.Family == "garanti");
    }

    [Fact]
    public void Resolve_DoesNotDuplicateSections() =>
        Assert.Equal(Resolve("Para iadesi kaç iş gününde yapılır?").Hits.Count,
            Resolve("Para iadesi kaç iş gününde yapılır?").Hits.Select(h => h.Chunk.ChunkId).Distinct().Count());
}
