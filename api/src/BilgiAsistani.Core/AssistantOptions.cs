namespace BilgiAsistani.Core;

public class AssistantOptions
{
    public const string Section = "Assistant";

    /// <summary>Göreli ise çalışma dizininden yukarı doğru aranır.</summary>
    public string DocumentsPath { get; set; } = "data/documents";

    /// <summary>auto: LLM servisi hazırsa llm, değilse extractive | llm | extractive</summary>
    public string AnswerMode { get; set; } = "auto";

    /// <summary>LLM'e gönderilecek en fazla doküman bölümü sayısı.</summary>
    public int TopK { get; set; } = 5;

    /// <summary>En iyi BM25 skoru bunun altındaysa LLM çağrılmadan "bilgi yok" denir.</summary>
    public double MinScore { get; set; } = 3.0;

    /// <summary>Çıkarımsal modda soru terimlerinin en iyi bölümde bulunması gereken en düşük oran.</summary>
    public double MinCoverage { get; set; } = 0.6;
}

public class LlmServiceOptions
{
    public const string Section = "LlmService";
    public string BaseUrl { get; set; } = "http://localhost:8100";
    // LLM çağrıları uzun sürebileceği için zaman aşımı geniş tutulur.
    public int TimeoutSeconds { get; set; } = 120;
}
