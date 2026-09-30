using BilgiAsistani.Core;

namespace BilgiAsistani.Eval;

/// <summary>Değerlendirme sorusu ve beklenen sonuç (eval/questions.json).</summary>
public record EvalCase(
    string Id, string Type, string Question, bool ExpectedAnswerable, string ExpectedAnswer,
    string? ExpectedDoc = null, List<string>? ExpectedKeywords = null, List<string>? ExpectedRejected = null,
    List<string>? ForbiddenKeywords = null);

/// <summary>Bir sorunun gerçek yanıtı, kontrol sonuçları ve başarılı olup olmadığı.</summary>
public record EvalRow(EvalCase Case, AskResponse Response, Dictionary<string, bool> Checks, bool Passed);
