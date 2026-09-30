using Microsoft.Extensions.Logging;
using Microsoft.Extensions.Logging.Abstractions;
using Microsoft.Extensions.Options;

namespace BilgiAsistani.Core;

/// <summary>Soru -> arama -> sürüm çözümleme -> yanıt üretimi akışı.</summary>
public class QaPipeline
{
    public const string NoInfoAnswer = "Dokümanlarda bu soruyu yanıtlamak için yeterli bilgi bulunamadı.";

    private readonly AssistantOptions _opts;
    private readonly ILogger<QaPipeline> _log;

    public Corpus Corpus { get; }
    public Bm25Index Index { get; }

    public QaPipeline(IOptions<AssistantOptions> options, ILogger<QaPipeline>? log = null)
    {
        _opts = options.Value;
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

    public Task<AskResponse> AskAsync(string question, CancellationToken ct = default)
    {
        const string mode = "extractive";

        // Sürüm çözümlemesinde eski bölümler elenebileceği için fazladan aday alınır.
        var rawHits = Index.Search(question, _opts.TopK * 2);
        var candidates = rawHits.Select(h => new Candidate(h.Chunk.ChunkId, Math.Round(h.Score, 3))).ToList();

        AskResponse NoInfo() => new(question, false, NoInfoAnswer, [], [], [], mode, null, candidates, []);

        if (rawHits.Count == 0 || rawHits[0].Score < _opts.MinScore)
            return Task.FromResult(NoInfo());

        var (resolved, decisions) = VersionResolver.Resolve(rawHits, question, Index, Corpus.Documents);
        var hits = resolved.Take(_opts.TopK).ToList();
        // Yalnızca eski sürümlerde eşleşme vardı; güncel sürüm bu konuya değinmiyor.
        if (hits.Count == 0)
            return Task.FromResult(NoInfo());

        var ext = ExtractiveAnswerer.Answer(question, hits, _opts.MinCoverage);
        var usedFamilies = ext.Used.Select(h => h.Chunk.Doc.Family).ToHashSet();
        return Task.FromResult(new AskResponse(
            question, ext.Answerable, ext.Answerable ? ext.Answer : NoInfoAnswer,
            ext.Used.Select(SourceRef.From).ToList(),
            decisions.Where(d => usedFamilies.Contains(d.Family)).ToList(),
            [], mode, null, candidates, []));
    }
}
