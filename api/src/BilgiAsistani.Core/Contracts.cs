namespace BilgiAsistani.Core;

// Dışarıya açılan API sözleşmesi. JSON alan adları snake_case'tir (JsonNamingPolicy.SnakeCaseLower).

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

public record DocumentInfo(
    string DocId, string Title, string Version, DateOnly EffectiveDate, string Status,
    string Family, string? Supersedes, IReadOnlyList<string> Sections);
