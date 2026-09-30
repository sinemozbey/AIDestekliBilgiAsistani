namespace BilgiAsistani.Core;

// Dışarıya açılan API sözleşmesi. JSON alan adları snake_case'tir (JsonNamingPolicy.SnakeCaseLower).

public record AskRequest(string? Question);

public record DocVersionRef(string DocId, string Title, string Version, DateOnly EffectiveDate, string Status);

public record SourceRef(
    string DocId, string Title, string Version, DateOnly EffectiveDate, string Status,
    string ChunkId, string Section, string Excerpt, double Score)
{
    public static SourceRef From(Hit h) => new(
        h.Chunk.Doc.DocId, h.Chunk.Doc.Title, h.Chunk.Doc.Version, h.Chunk.Doc.EffectiveDate, h.Chunk.Doc.Status,
        h.Chunk.ChunkId, h.Chunk.Section, h.Chunk.Text, Math.Round(h.Score, 3));
}

public record RejectedVersion(
    string DocId, string Title, string Version, DateOnly EffectiveDate, string Status,
    string? Section, string? Excerpt);

/// <summary>Aynı doküman ailesinin birden fazla sürümü bulunduğunda verilen karar.</summary>
public record VersionDecision(
    string Family, DocVersionRef Selected, IReadOnlyList<RejectedVersion> Rejected, string Rule, string Explanation);

/// <summary>Farklı dokümanlar arasında LLM tarafından tespit edilen içerik çelişkisi.</summary>
public record ContentConflict(string Topic, DocVersionRef Chosen, DocVersionRef Rejected, string Reason);

public record Candidate(string ChunkId, double Score);

public record AskResponse(
    string Question,
    bool Answerable,
    string Answer,
    IReadOnlyList<SourceRef> Sources,
    IReadOnlyList<VersionDecision> VersionDecisions,
    IReadOnlyList<ContentConflict> ContentConflicts,
    string Mode,
    string? Model,
    IReadOnlyList<Candidate> Candidates,
    IReadOnlyList<string> Warnings);

/// <summary>Varsayılan (sade) yanıtta kaynak: hangi dokümanın hangi bölümü kullanıldı ve bölümün metni.</summary>
public record SummarySource(string Document, string Version, string Section, string Excerpt);

/// <summary>
/// /api/ask'in varsayılan yanıtı: tek bir yanıt, kullanılan bölümler ve sürüm/çelişki kararlarının tek cümlelik
/// açıklamaları. Reddedilen sürüm metinleri, uygulanan kural ve arama skorları için ?details=true kullanılır.
/// </summary>
public record AskSummary(bool Answerable, string Answer, IReadOnlyList<SummarySource> Sources, IReadOnlyList<string> Notes)
{
    public static AskSummary From(AskResponse r) => new(
        r.Answerable,
        r.Answer,
        r.Sources.Select(s => new SummarySource(s.Title, s.Version, s.Section, s.Excerpt)).ToList(),
        [
            .. r.VersionDecisions.Select(d => d.Explanation),
            .. r.ContentConflicts.Select(c =>
                $"'{c.Chosen.Title}' ({c.Chosen.EffectiveDate:yyyy-MM-dd}) ile '{c.Rejected.Title}' " +
                $"({c.Rejected.EffectiveDate:yyyy-MM-dd}) çelişiyor ({c.Topic}); daha güncel olan '{c.Chosen.Title}' esas alındı."),
            .. r.Warnings,
        ]);
}

public record SearchResponse(string Query, IReadOnlyList<SourceRef> Results);

public record DocumentInfo(
    string DocId, string Title, string Version, DateOnly EffectiveDate, string Status,
    string Family, string? Supersedes, IReadOnlyList<string> Sections);
