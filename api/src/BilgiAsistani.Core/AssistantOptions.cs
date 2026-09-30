namespace BilgiAsistani.Core;

public class AssistantOptions
{
    public const string Section = "Assistant";

    /// <summary>Göreli ise çalışma dizininden yukarı doğru aranır.</summary>
    public string DocumentsPath { get; set; } = "data/documents";

    /// <summary>Yanıt üretimine aktarılacak en fazla doküman bölümü sayısı.</summary>
    public int TopK { get; set; } = 5;

    /// <summary>En iyi BM25 skoru bunun altındaysa yanıt üretilmeden "bilgi yok" denir.</summary>
    public double MinScore { get; set; } = 3.0;

    /// <summary>Çıkarımsal modda soru terimlerinin en iyi bölümde bulunması gereken en düşük oran.</summary>
    public double MinCoverage { get; set; } = 0.6;
}
