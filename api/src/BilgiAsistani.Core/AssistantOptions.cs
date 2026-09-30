namespace BilgiAsistani.Core;

/// <summary>Yanıt modları: Assistant:AnswerMode ayarının değerleri ve yanıttaki "mode" alanı.</summary>
public static class AnswerModes
{
    /// <summary>LLM servisi kullanılabiliyorsa LLM, değilse çıkarımsal (yalnızca ayar değeri).</summary>
    public const string Auto = "auto";
    public const string Llm = "llm";
    public const string Extractive = "extractive";
    /// <summary>LLM seçilmişti ama ulaşılamadığı için çıkarımsal yanıt verildi (yalnızca yanıtta görülür).</summary>
    public const string ExtractiveFallback = "extractive_fallback";
}

public class AssistantOptions
{
    public const string Section = "Assistant";

    /// <summary>Göreli ise çalışma dizininden yukarı doğru aranır.</summary>
    public string DocumentsPath { get; set; } = "data/documents";

    /// <summary>auto: LLM servisi hazırsa llm, değilse extractive | llm | extractive</summary>
    public string AnswerMode { get; set; } = AnswerModes.Auto;

    /// <summary>LLM'e gönderilecek en fazla doküman bölümü sayısı.</summary>
    public int TopK { get; set; } = 5;

    /// <summary>En iyi BM25 skoru bunun altındaysa LLM çağrılmadan "bilgi yok" denir.</summary>
    public double MinScore { get; set; } = 3.0;

    /// <summary>Çıkarımsal modda soru terimlerinin en iyi bölümde bulunması gereken en düşük oran.</summary>
    public double MinCoverage { get; set; } = 0.6;

    /// <summary>
    /// LLM'e tüm bölümleriyle gönderilecek en fazla doküman sayısı (en iyi eşleşenlerden). Yanıtın bir kısmı
    /// dokümanın aramada öne çıkmayan bir bölümündeyse LLM onu da görür. 0: yalnızca en iyi TopK bölüm gönderilir.
    /// </summary>
    public int ExpandedDocuments { get; set; } = 2;
}

public class LlmServiceOptions
{
    public const string Section = "LlmService";
    public string BaseUrl { get; set; } = "http://localhost:8100";
    // LLM çağrıları uzun sürebileceği için zaman aşımı geniş tutulur.
    public int TimeoutSeconds { get; set; } = 120;
}
