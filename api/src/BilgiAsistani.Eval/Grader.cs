using BilgiAsistani.Core;

namespace BilgiAsistani.Eval;

/// <summary>Bir yanıtı beklenen sonuçla karşılaştırır; her kontrol için geçti/kaldı döndürür.</summary>
public static class Grader
{
    public static Dictionary<string, bool> Grade(EvalCase c, AskResponse r)
    {
        var checks = new Dictionary<string, bool> { ["yanıtlanabilirlik"] = r.Answerable == c.ExpectedAnswerable };
        if (c.ExpectedAnswerable && r.Answerable)
        {
            checks["doğru_kaynak"] = r.Sources.Any(s => s.DocId == c.ExpectedDoc);
            checks["eski_sürüm_kullanılmadı"] = r.Sources.All(s => s.Status == Document.ActiveStatus);
            var answer = TurkishText.Normalize(r.Answer);
            checks["anahtar_bilgi"] = (c.ExpectedKeywords ?? []).Any(k => answer.Contains(TurkishText.Normalize(k)));
            if (c.ForbiddenKeywords is { Count: > 0 })
                checks["eski_bilgi_sızmadı"] = !c.ForbiddenKeywords.Any(k => answer.Contains(TurkishText.Normalize(k)));
            if (c.ExpectedRejected is { Count: > 0 })
            {
                var rejected = r.VersionDecisions.SelectMany(d => d.Rejected.Select(x => x.DocId))
                    .Concat(r.ContentConflicts.Select(x => x.Rejected.DocId)).ToHashSet();
                checks["çelişki_gösterildi"] = c.ExpectedRejected.All(rejected.Contains);
            }
        }
        else if (!c.ExpectedAnswerable)
        {
            checks["kaynak_uydurulmadı"] = r.Sources.Count == 0;
        }
        return checks;
    }
}
