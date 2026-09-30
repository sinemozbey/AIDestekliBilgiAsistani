namespace BilgiAsistani.Core;

public sealed record Hit(Chunk Chunk, double Score, IReadOnlySet<string> MatchedTerms);

/// <summary>Bağımlılıksız BM25 arama indeksi.</summary>
public sealed class Bm25Index
{
    // Başlık terimleri gövdeye göre daha belirleyici olduğundan tekrar edilerek ağırlıklandırılır.
    private const int TitleWeight = 2;
    private const int SectionWeight = 3;
    private const double K1 = 1.5, B = 0.75;

    private readonly IReadOnlyList<Chunk> _chunks;
    private readonly List<Dictionary<string, int>> _tf;
    private readonly double _avgdl;
    private readonly HashSet<string> _vocabulary;
    private readonly Dictionary<string, double> _idfCache = new();
    private readonly Lock _cacheLock = new();

    public Bm25Index(IReadOnlyList<Chunk> chunks)
    {
        _chunks = chunks;
        foreach (var c in chunks)
        {
            var title = TurkishText.Tokenize(c.Doc.Title);
            var section = TurkishText.Tokenize(c.Section);
            c.Tokens = [
                .. Enumerable.Repeat(title, TitleWeight).SelectMany(t => t),
                .. Enumerable.Repeat(section, SectionWeight).SelectMany(t => t),
                .. TurkishText.Tokenize(c.Text),
            ];
        }
        _tf = chunks.Select(c => c.Tokens.GroupBy(t => t).ToDictionary(g => g.Key, g => g.Count())).ToList();
        _avgdl = chunks.Count == 0 ? 0 : chunks.Average(c => c.Tokens.Count);
        _vocabulary = _tf.SelectMany(tf => tf.Keys).ToHashSet();
    }

    /// <summary>Sorgu kökünü sözlükte eşleşen köklere genişletir ("iade" -> {"iade", "iates", "iatel"}).</summary>
    public IReadOnlyList<string> Expand(string term) =>
        _vocabulary.Where(v => TurkishText.StemsMatch(term, v)).Order(StringComparer.Ordinal).ToList();

    // Varyantlar tek bir terim gibi ele alınır: df, herhangi bir varyantı içeren bölüm sayısıdır.
    private double Idf(IReadOnlyList<string> variants)
    {
        var key = string.Join('|', variants);
        lock (_cacheLock)
        {
            if (_idfCache.TryGetValue(key, out var cached)) return cached;
            var n = _chunks.Count;
            var df = _tf.Count(tf => variants.Any(tf.ContainsKey));
            return _idfCache[key] = Math.Log(1 + (n - df + 0.5) / (df + 0.5));
        }
    }

    private Hit ScoreChunk(int idx, Dictionary<string, IReadOnlyList<string>> expanded)
    {
        var tf = _tf[idx];
        var dl = _chunks[idx].Tokens.Count;
        double score = 0;
        var matched = new HashSet<string>();
        foreach (var (term, variants) in expanded)
        {
            var f = variants.Sum(v => tf.GetValueOrDefault(v));
            if (f == 0) continue;
            matched.Add(term);
            score += Idf(variants) * f * (K1 + 1) / (f + K1 * (1 - B + B * dl / _avgdl));
        }
        return new Hit(_chunks[idx], score, matched);
    }

    public IReadOnlyList<Hit> Search(string query, int topK = 5, string? docId = null)
    {
        var expanded = TurkishText.Tokenize(query).Distinct().ToDictionary(t => t, Expand);
        return Enumerable.Range(0, _chunks.Count)
            .Where(i => docId is null || _chunks[i].Doc.DocId == docId)
            .Select(i => ScoreChunk(i, expanded))
            .Where(h => h.Score > 0)
            .OrderByDescending(h => h.Score)
            .Take(topK)
            .ToList();
    }
}
