using System.Globalization;

namespace BilgiAsistani.Core;

/// <summary>
/// Aynı doküman ailesindeki sürümler arasında güncel olanı seçer.
/// Kural (sırasıyla): 1) durumu "yururlukte" olan, 2) yürürlük tarihi en yeni olan,
/// 3) sürüm numarası en yüksek olan. Eski sürümün bir bölümü aramada öne çıkarsa aynı soru
/// güncel sürüm içinde tekrar aranır ve eski bölümün yerine güncel bölüm kullanılır.
/// </summary>
public static class VersionResolver
{
    public const string Rule = "1) yürürlükte olan sürüm, 2) en yeni yürürlük tarihi, 3) en yüksek sürüm numarası";

    public static string StatusLabel(string status) => status switch
    {
        "yururlukte" => "yürürlükte",
        "yururlukten_kalkti" => "yürürlükten kalktı",
        _ => status,
    };

    private sealed class VersionComparer : IComparer<Document>
    {
        public static readonly VersionComparer Instance = new();

        public int Compare(Document? x, Document? y)
        {
            var c = x!.IsActive.CompareTo(y!.IsActive);
            if (c != 0) return c;
            c = x.EffectiveDate.CompareTo(y.EffectiveDate);
            return c != 0 ? c : ParseVersion(x.Version).CompareTo(ParseVersion(y.Version));
        }

        private static Version ParseVersion(string v) =>
            System.Version.TryParse(v.Contains('.') ? v : v + ".0", out var parsed) ? parsed : new Version(0, 0);
    }

    public static Document CurrentVersion(string family, IEnumerable<Document> docs) =>
        docs.Where(d => d.Family == family).MaxBy(d => d, VersionComparer.Instance)!;

    /// <summary>
    /// Arama sonuçlarındaki eski sürüm bölümlerini eler. Her aile için güncel sürüm seçilir; güncel sürüme ait
    /// bölümler kalır, eski sürüme ait bölümün yerine aynı soru güncel sürümde aranarak bulunan bölüm konur.
    /// Sonunda tekrarlar temizlenir ve her aile için kararın açıklaması üretilir.
    /// </summary>
    public static (List<Hit> Hits, List<VersionDecision> Decisions) Resolve(
        IReadOnlyList<Hit> hits, string query, Bm25Index index, IReadOnlyList<Document> docs)
    {
        var winners = hits.Select(h => h.Chunk.Doc.Family).Distinct().ToDictionary(f => f, f => CurrentVersion(f, docs));

        var resolved = new List<Hit>();
        var rejectedByFamily = new Dictionary<string, Dictionary<string, RejectedVersion>>();
        var familyOrder = new List<string>();
        foreach (var hit in hits)
        {
            var doc = hit.Chunk.Doc;
            var winner = winners[doc.Family];
            if (doc.DocId == winner.DocId)
            {
                resolved.Add(hit);
                continue;
            }
            if (!rejectedByFamily.TryGetValue(doc.Family, out var rejected))
            {
                rejectedByFamily[doc.Family] = rejected = new Dictionary<string, RejectedVersion>();
                familyOrder.Add(doc.Family);
            }
            rejected.TryAdd(doc.DocId, new RejectedVersion(
                doc.DocId, doc.Title, doc.Version, doc.EffectiveDate, doc.Status, hit.Chunk.Section, hit.Chunk.Text));

            // Eski sürümün yerine güncel sürümdeki en ilgili bölümü getir. Yeni bölüm, sıralamada geri
            // düşmemesi için eski bölümün skorunu devralır (eski bölüm daha kısa olduğu için öne çıkmış olabilir).
            var replacement = index.Search(query, topK: 1, docId: winner.DocId);
            if (replacement.Count > 0)
                resolved.Add(replacement[0] with { Score = Math.Max(replacement[0].Score, hit.Score) });
        }

        // Aynı bölüm birden fazla kez eklendiyse tekilleştir, skora göre sırala.
        var unique = new Dictionary<string, Hit>();
        var order = new List<string>();
        foreach (var h in resolved)
        {
            if (!unique.TryGetValue(h.Chunk.ChunkId, out var existing)) order.Add(h.Chunk.ChunkId);
            if (existing is null || existing.Score < h.Score) unique[h.Chunk.ChunkId] = h;
        }
        var result = order.Select(id => unique[id]).OrderByDescending(h => h.Score).ToList();

        var decisions = familyOrder.Select(family =>
        {
            var winner = winners[family];
            var rejected = rejectedByFamily[family].Values.ToList();
            var old = string.Join(", ", rejected.Select(r => $"v{r.Version} ({Iso(r.EffectiveDate)}, {StatusLabel(r.Status)})"));
            return new VersionDecision(
                family, winner.ToRef(), rejected, Rule,
                $"'{winner.Title}' için birden fazla sürüm bulundu. v{winner.Version} (yürürlük: {Iso(winner.EffectiveDate)}, " +
                $"{StatusLabel(winner.Status)}) seçildi; {old} yanıtta kullanılmadı.");
        }).ToList();
        return (result, decisions);
    }

    private static string Iso(DateOnly d) => d.ToString("yyyy-MM-dd", CultureInfo.InvariantCulture);
}
