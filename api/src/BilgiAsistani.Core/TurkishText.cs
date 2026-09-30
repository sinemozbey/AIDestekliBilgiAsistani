using System.Text.RegularExpressions;

namespace BilgiAsistani.Core;

/// <summary>
/// Türkçe metin normalizasyonu ve basit kök bulma.
/// Türkçe eklemeli bir dil olduğundan tam bir morfolojik çözümleyici yerine, bilgi erişiminde
/// iyi sonuç verdiği bilinen "ilk 5 karakter" (F5) kesmesi kullanılır:
/// "iadeler" -> "iade", "kargoya" -> "kargo".
/// </summary>
public static partial class TurkishText
{
    public const int StemLength = 5;
    public const int MinPrefix = 4;

    private static readonly HashSet<string> Stopwords =
    [
        "acaba", "ama", "ancak", "bana", "bazı", "ben", "benim", "bir", "biri", "birkaç", "bu", "bunu",
        "buna", "çok", "da", "de", "daha", "defa", "diye", "en", "gibi", "hangi", "hem", "hep", "her",
        "için", "ile", "ise", "kaç", "kadar", "ki", "mı", "mi", "mu", "mü", "mısın", "misin", "nasıl",
        "ne", "neden", "nedir", "nerede", "nereden", "niçin", "o", "olan", "olarak", "oluyor", "olur",
        "sonra", "şu", "var", "varsa", "ve", "veya", "ya", "yok", "zaman", "hangisi", "midir", "mıdır",
        "bizim", "sizin", "size", "siz", "biz", "istiyorum", "lazım", "gerekir", "gerekiyor",
        "yapmam", "yapabilirim", "yaparım", "ederim", "edebilir", "miyim", "mıyım", "muyum", "müyüm",
        "kaçta", "hâlâ", "artık", "şimdi", "kim", "kime", "kimin", "neler", "nelerdir", "the", "nova",
    ];

    [GeneratedRegex("[0-9a-zçğıöşüâîû]+")]
    private static partial Regex TokenRegex();

    [GeneratedRegex(@"(?<=[.!?])\s+(?=[A-ZÇĞİÖŞÜ0-9])")]
    private static partial Regex SentenceBoundary();

    /// <summary>I/İ harflerini Türkçe kurallarıyla küçültür ("İADE" -> "iade", "IŞIK" -> "ışık").</summary>
    public static string Normalize(string text) => text.Replace('I', 'ı').Replace('İ', 'i').ToLowerInvariant();

    /// <summary>
    /// F5 kökü; ünsüz yumuşaması (kitap -> kitabı, renk -> rengi) aynı köke eşlensin diye
    /// yumuşak ünsüzler sert karşılıklarına çevrilir.
    /// </summary>
    public static string Stem(string token)
    {
        var s = token.Length > StemLength ? token[..StemLength] : token;
        return string.Create(s.Length, s, static (span, src) =>
        {
            for (var i = 0; i < src.Length; i++)
                span[i] = src[i] switch { 'b' => 'p', 'c' => 'ç', 'd' => 't', 'ğ' => 'k', 'g' => 'k', var ch => ch };
        });
    }

    /// <summary>Kısa köklerde F5 yetersiz kalır ("iade" / "iades"): biri diğerinin önekiyse eşleştir.</summary>
    public static bool StemsMatch(string a, string b)
    {
        if (a == b) return true;
        var (shorter, longer) = a.Length <= b.Length ? (a, b) : (b, a);
        return shorter.Length >= MinPrefix && longer.StartsWith(shorter, StringComparison.Ordinal);
    }

    /// <summary>Metni normalize edip durak kelimeleri atar ve kök listesi döndürür.</summary>
    public static List<string> Tokenize(string text) =>
        TokenRegex().Matches(Normalize(text))
            .Select(m => m.Value)
            .Where(t => t.Length > 1 && !Stopwords.Contains(t))
            .Select(Stem)
            .ToList();

    public static List<string> SplitSentences(string text) =>
        SentenceBoundary().Split(text.Trim()).Select(p => p.Trim()).Where(p => p.Length > 0).ToList();
}
