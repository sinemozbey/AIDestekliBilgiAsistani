using System.Globalization;
using System.Text;

namespace BilgiAsistani.Eval;

/// <summary>Değerlendirme sonuçlarından beklenen/gerçek karşılaştırma tablosu içeren Markdown rapor üretir.</summary>
public static class Report
{
    private static readonly (string Key, string Label)[] Types =
        [("normal", "Normal"), ("cevapsiz", "Cevapsız"), ("celiskili", "Çelişkili")];

    private static string Cell(string s) => s.Replace("|", "\\|").Replace("\n", " ");

    public static string Render(List<EvalRow> rows, List<string> modes, string? model, string via)
    {
        var sb = new StringBuilder();
        var modelTxt = model is null ? "" : $" (`{model}`)";
        sb.AppendLine($"# Değerlendirme Sonuçları — mod: {string.Join(", ", modes.Select(m => $"`{m}`"))}{modelTxt}");
        sb.AppendLine();
        sb.AppendLine($"Çalıştırma zamanı: {DateTime.Now.ToString("yyyy-MM-dd HH:mm", CultureInfo.InvariantCulture)} · Çağrı yolu: {via}");
        sb.AppendLine();
        sb.AppendLine("## Özet");
        sb.AppendLine();
        sb.AppendLine("| Tür | Başarılı / Toplam |");
        sb.AppendLine("|---|---|");
        foreach (var (key, label) in Types)
        {
            var g = rows.Where(r => r.Case.Type == key).ToList();
            sb.AppendLine($"| {label} | {g.Count(r => r.Passed)} / {g.Count} |");
        }
        sb.AppendLine($"| **Toplam** | **{rows.Count(r => r.Passed)} / {rows.Count}** |");
        sb.AppendLine();
        sb.AppendLine("## Karşılaştırma");
        sb.AppendLine();
        sb.AppendLine("| # | Tür | Soru | Beklenen | Gerçek yanıt | Kaynak (doküman › bölüm) | Sürüm/çelişki kararı | Sonuç |");
        sb.AppendLine("|---|---|---|---|---|---|---|---|");
        foreach (var r in rows)
        {
            var sources = string.Join("<br>", r.Response.Sources.Select(s => $"{s.DocId} v{s.Version} › {s.Section}"));
            var decisions = r.Response.VersionDecisions
                .Select(d => $"{d.Selected.DocId} seçildi; {string.Join(", ", d.Rejected.Select(x => x.DocId))} reddedildi")
                .Concat(r.Response.ContentConflicts.Select(x => $"{x.Chosen.DocId} > {x.Rejected.DocId} ({x.Topic})"));
            var failed = r.Checks.Where(kv => !kv.Value).Select(kv => kv.Key).ToList();
            var verdict = r.Passed ? "✅" : "❌ " + string.Join(", ", failed);
            var type = Types.First(t => t.Key == r.Case.Type).Label;
            var dec = Cell(string.Join("<br>", decisions));
            sb.AppendLine($"| {r.Case.Id} | {type} | {Cell(r.Case.Question)} | {Cell(r.Case.ExpectedAnswer)} | " +
                          $"{Cell(r.Response.Answer)} | {(sources.Length > 0 ? sources : "—")} | " +
                          $"{(dec.Length > 0 ? dec : "—")} | {verdict} |");
        }
        sb.AppendLine();
        sb.AppendLine("## Kontroller");
        sb.AppendLine();
        sb.AppendLine("- **yanıtlanabilirlik**: sistemin yanıt verip vermeme kararı beklenenle aynı mı?");
        sb.AppendLine("- **doğru_kaynak**: beklenen (güncel) doküman kaynaklar arasında mı?");
        sb.AppendLine("- **eski_sürüm_kullanılmadı**: yanıtta yürürlükten kalkmış bir sürüm kaynak olarak gösterilmedi mi?");
        sb.AppendLine("- **anahtar_bilgi**: yanıtta beklenen kritik bilgi (ör. \"30 gün\") geçiyor mu?");
        sb.AppendLine("- **eski_bilgi_sızmadı**: yanıtta geçersiz kalmış bir bilgi (ör. eski kargo ücreti kuralı) yer almıyor mu?");
        sb.AppendLine("- **çelişki_gösterildi**: reddedilen eski kaynak(lar) yanıtın karar alanlarında açıkça listelendi mi?");
        sb.AppendLine("- **kaynak_uydurulmadı**: cevapsız sorularda kaynak gösterilmedi mi?");
        return sb.ToString();
    }
}
