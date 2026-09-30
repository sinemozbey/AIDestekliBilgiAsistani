using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BilgiAsistani.Core;

/// <summary>Soru -> arama -> sürüm çözümleme -> yanıt üretimi akışı.</summary>
public class QaPipeline
{
    public const string NoInfoAnswer = "Dokümanlarda bu soruyu yanıtlamak için yeterli bilgi bulunamadı.";
    private static readonly TimeSpan HealthCacheTtl = TimeSpan.FromSeconds(30);

    private readonly AssistantOptions _opts;
    private readonly ILlmClient _llm;
    private readonly ILogger<QaPipeline> _log;
    // Sağlık durumu tek bir değiştirilemez nesnede tutulur. Referans ataması atomik olduğu için eşzamanlı
    // istekler zamanı bir kayıttan, durumu başka bir kayıttan okuyamaz (iki alanlı bir struct'ta bu mümkündü).
    private sealed record HealthSnapshot(DateTime CheckedAt, LlmHealth? Health);
    private volatile HealthSnapshot? _healthCache;

    public Corpus Corpus { get; }
    public Bm25Index Index { get; }

    public QaPipeline(IOptions<AssistantOptions> options, ILlmClient llm, ILogger<QaPipeline>? log = null)
    {
        _opts = options.Value;
        _llm = llm;
        _log = log ?? NullLogger<QaPipeline>.Instance;
        Corpus = Corpus.Load(Corpus.ResolveDirectory(_opts.DocumentsPath));
        Index = new Bm25Index(Corpus.Chunks);
        _log.LogDebug("{Docs} doküman, {Chunks} bölüm indekslendi", Corpus.Documents.Count, Corpus.Chunks.Count);
    }

    public AssistantOptions Options => _opts;

    public IReadOnlyList<DocumentInfo> Documents() => Corpus.Documents.Select(d => new DocumentInfo(
        d.DocId, d.Title, d.Version, d.EffectiveDate, d.Status, d.Family, d.Supersedes,
        Corpus.Chunks.Where(c => c.Doc.DocId == d.DocId).Select(c => c.Section).ToList())).ToList();

    public IReadOnlyList<Hit> Search(string query, int? topK = null) => Index.Search(query, topK ?? _opts.TopK);

    /// <summary>LLM servisinin sağlık durumu; her istekte servise gitmemek için kısa süre önbelleğe alınır.</summary>
    public async Task<LlmHealth?> GetLlmHealthAsync(CancellationToken ct)
    {
        if (_healthCache is { } c && DateTime.UtcNow - c.CheckedAt < HealthCacheTtl) return c.Health;
        var health = await _llm.GetHealthAsync(ct);
        _healthCache = new HealthSnapshot(DateTime.UtcNow, health);
        return health;
    }

    private async Task<bool> UseLlmAsync(CancellationToken ct) => _opts.AnswerMode.ToLowerInvariant() switch
    {
        AnswerModes.Extractive => false,
        AnswerModes.Llm => true,
        _ => (await GetLlmHealthAsync(ct))?.LlmAvailable ?? false,
    };

    public async Task<AskResponse> AskAsync(string question, CancellationToken ct = default)
    {
        var useLlm = await UseLlmAsync(ct);
        var mode = useLlm ? AnswerModes.Llm : AnswerModes.Extractive;

        // Sürüm çözümlemesinde eski bölümler elenebileceği için fazladan aday alınır.
        var rawHits = Index.Search(question, _opts.TopK * 2);
        var candidates = rawHits.Select(h => new Candidate(h.Chunk.ChunkId, Math.Round(h.Score, 3))).ToList();

        AskResponse NoInfo(string m) => new(question, false, NoInfoAnswer, [], [], [], m, null, candidates, []);

        if (rawHits.Count == 0 || rawHits[0].Score < _opts.MinScore)
            return NoInfo(mode);

        var (resolved, decisions) = VersionResolver.Resolve(rawHits, question, Index, Corpus.Documents);
        var hits = resolved.Take(_opts.TopK).ToList();
        // Yalnızca eski sürümlerde eşleşme vardı; güncel sürüm bu konuya değinmiyor.
        if (hits.Count == 0)
            return NoInfo(mode);

        var warnings = new List<string>();
        if (useLlm)
        {
            try
            {
                return await AskLlmAsync(question, BuildLlmContext(question, hits), decisions, candidates, ct);
            }
            catch (LlmServiceException e)
            {
                _log.LogWarning(e, "LLM kullanılamadı, çıkarımsal moda geçiliyor");
                warnings.Add($"LLM kullanılamadı, çıkarımsal moda geçildi: {e.Message}");
                mode = AnswerModes.ExtractiveFallback;
            }
        }

        var ext = ExtractiveAnswerer.Answer(question, hits, _opts.MinCoverage);
        var usedFamilies = ext.Used.Select(h => h.Chunk.Doc.Family).ToHashSet();
        return new AskResponse(
            question, ext.Answerable, ext.Answerable ? ext.Answer : NoInfoAnswer,
            ext.Used.Select(SourceRef.From).ToList(),
            decisions.Where(d => usedFamilies.Contains(d.Family)).ToList(),
            [], mode, null, candidates, warnings);
    }

    /// <summary>
    /// LLM'e gönderilecek bağlam: en iyi eşleşen (en fazla ExpandedDocuments adet) dokümanın tüm bölümleri,
    /// doküman içindeki sırasıyla; ardından diğer dokümanlardan gelen en iyi bölümler. Arama doğru dokümanı
    /// bulup yanıtın bir kısmını içeren bölümü öne çıkaramadığında (ör. "pazar günü destek" sorusunda
    /// "Canlı Sohbet: 7 gün 24 saat" bölümü) LLM bu bölümü de görür. Hits yalnızca güncel sürümleri
    /// içerdiğinden genişletme de yalnızca güncel sürümlere uygulanır.
    /// </summary>
    public IReadOnlyList<Hit> BuildLlmContext(string question, IReadOnlyList<Hit> hits)
    {
        const int maxSources = 20; // LLM servisinin kabul ettiği en fazla kaynak sayısı
        if (_opts.ExpandedDocuments <= 0) return hits;

        var expanded = hits.Select(h => h.Chunk.Doc.DocId).Distinct().Take(_opts.ExpandedDocuments).ToHashSet();
        // Bölümün skoru: sürüm çözümlemesinden gelen skor, yoksa ham arama skoru, o da yoksa 0 (yalnızca genişletmeyle eklendi).
        var scores = Index.Search(question, Corpus.Chunks.Count).ToDictionary(h => h.Chunk.ChunkId);
        foreach (var h in hits) scores[h.Chunk.ChunkId] = h;

        return hits.Select(h => h.Chunk.Doc.DocId).Distinct()
            .SelectMany(docId => expanded.Contains(docId)
                ? Corpus.Chunks.Where(c => c.Doc.DocId == docId)
                    .Select(c => scores.GetValueOrDefault(c.ChunkId) ?? new Hit(c, 0, new HashSet<string>()))
                : hits.Where(h => h.Chunk.Doc.DocId == docId))
            .Take(maxSources)
            .ToList();
    }

    private async Task<AskResponse> AskLlmAsync(
        string question, IReadOnlyList<Hit> hits, List<VersionDecision> decisions, List<Candidate> candidates, CancellationToken ct)
    {
        var request = new GenerateRequest(question, hits.Select(h => new LlmSource(
            h.Chunk.ChunkId, h.Chunk.Doc.Title, h.Chunk.Doc.Version, h.Chunk.Doc.EffectiveDate, h.Chunk.Doc.Status,
            h.Chunk.Section, h.Chunk.Text)).ToList());
        var result = await _llm.GenerateAsync(request, ct);

        // LLM'in gösterdiği kaynak kimlikleri gerçek adaylarla doğrulanır; uydurulmuş kimlikler atılır.
        var byId = hits.ToDictionary(h => h.Chunk.ChunkId);
        var used = result.UsedSourceIds.Distinct().Where(byId.ContainsKey).Select(id => byId[id]).ToList();

        // Çelişki kayıtlarındaki tarih/sürüm bilgisi LLM çıktısından değil, doküman meta verisinden alınır.
        var conflicts = result.Conflicts
            .Where(c => byId.ContainsKey(c.ChosenSourceId) && byId.ContainsKey(c.RejectedSourceId))
            .Select(c => (c, Chosen: byId[c.ChosenSourceId].Chunk.Doc, Rejected: byId[c.RejectedSourceId].Chunk.Doc))
            .Where(x => x.Chosen.DocId != x.Rejected.DocId)
            .Select(x => new ContentConflict(x.c.Topic, x.Chosen.ToRef(), x.Rejected.ToRef(), x.c.Reason))
            .ToList();

        var answerable = result.Answerable && used.Count > 0;
        var usedFamilies = used.Select(h => h.Chunk.Doc.Family).ToHashSet();
        return new AskResponse(
            question, answerable,
            string.IsNullOrWhiteSpace(result.Answer) ? NoInfoAnswer : result.Answer,
            answerable ? used.Select(SourceRef.From).ToList() : [],
            // Yalnızca yanıtta kullanılan aileye ait sürüm kararları gösterilir.
            answerable ? decisions.Where(d => usedFamilies.Contains(d.Family)).ToList() : [],
            answerable ? conflicts : [],
            AnswerModes.Llm, result.Model, candidates, []);
    }
}
