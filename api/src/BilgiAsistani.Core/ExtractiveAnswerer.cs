namespace BilgiAsistani.Core;

public sealed record ExtractiveAnswer(bool Answerable, string Answer, IReadOnlyList<Hit> Used, double Coverage);

/// <summary>
/// LLM kullanmadan çalışan çıkarımsal yanıtlayıcı. En ilgili bölümden soru terimleriyle en çok
/// örtüşen cümleleri seçer. Kaynaklarda bilgi olup olmadığını, soru terimlerinin en iyi bölümde
/// geçme oranıyla (kapsama) ölçer.
/// </summary>
public static class ExtractiveAnswerer
{
    private static int Overlap(IEnumerable<string> terms, IReadOnlyCollection<string> tokens) =>
        terms.Count(t => tokens.Any(d => TurkishText.StemsMatch(t, d)));

    public static double Coverage(string question, Hit hit)
    {
        var terms = TurkishText.Tokenize(question).Distinct().ToList();
        return terms.Count == 0 ? 0 : (double)Overlap(terms, hit.Chunk.Tokens.ToHashSet()) / terms.Count;
    }

    public static ExtractiveAnswer Answer(string question, IReadOnlyList<Hit> hits, double minCoverage, int maxSentences = 2)
    {
        var top = hits[0];
        var coverage = Coverage(question, top);
        if (coverage < minCoverage) return new ExtractiveAnswer(false, "", [], coverage);

        var terms = TurkishText.Tokenize(question).Distinct().ToList();
        var chosen = TurkishText.SplitSentences(top.Chunk.Text)
            .Select((s, i) => (Sentence: s, Index: i, Score: Overlap(terms, TurkishText.Tokenize(s).ToHashSet())))
            .OrderByDescending(p => p.Score).ThenBy(p => p.Index)
            .Take(maxSentences)
            .OrderBy(p => p.Index) // doküman sırasını koru
            .Select(p => p.Sentence);
        return new ExtractiveAnswer(true, string.Join(' ', chosen), [top], coverage);
    }
}
