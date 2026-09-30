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
        "extractive" => false,
        "llm" => true,
        _ => (await GetLlmHealthAsync(ct))?.LlmAvailable ?? false,
    };

    public async Task<AskResponse> AskAsync(string question, CancellationToken ct = default)
    {
        var useLlm = await UseLlmAsync(ct);
        var mode = useLlm ? "llm" : "extractive";

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
                return await AskLlmAsync(question, hits, decisions, candidates, ct);
            }
            catch (LlmServiceException e)
            {
                _log.LogWarning(e, "LLM kullanılamadı, çıkarımsal moda geçiliyor");
                warnings.Add($"LLM kullanılamadı, çıkarımsal moda geçildi: {e.Message}");
                mode = "extractive_fallback";
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

    private async Task<AskResponse> AskLlmAsync(
        string question, List<Hit> hits, List<VersionDecision> decisions, List<Candidate> candidates, CancellationToken ct)
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
            "llm", result.Model, candidates, []);
    }
}
