namespace BilgiAsistani.Core;

// Dışarıya açılan API sözleşmesi. JSON alan adları snake_case'tir (JsonNamingPolicy.SnakeCaseLower).

public record DocVersionRef(string DocId, string Title, string Version, DateOnly EffectiveDate, string Status);

public record RejectedVersion(
    string DocId, string Title, string Version, DateOnly EffectiveDate, string Status,
    string? Section, string? Excerpt);

/// <summary>Aynı doküman ailesinin birden fazla sürümü bulunduğunda verilen karar.</summary>
public record VersionDecision(
    string Family, DocVersionRef Selected, IReadOnlyList<RejectedVersion> Rejected, string Rule, string Explanation);
