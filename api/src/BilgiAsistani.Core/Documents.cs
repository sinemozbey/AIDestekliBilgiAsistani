using System.Globalization;
using System.Text.RegularExpressions;

namespace BilgiAsistani.Core;

public sealed record Document(
    string DocId, string Title, string Family, string Version, DateOnly EffectiveDate, string Status,
    string? Supersedes, string FileName)
{
    public const string ActiveStatus = "yururlukte";
    public bool IsActive => Status == ActiveStatus;

    public DocVersionRef ToRef() => new(DocId, Title, Version, EffectiveDate, Status);
}

public sealed class Chunk(string chunkId, Document doc, string section, string text)
{
    public string ChunkId { get; } = chunkId;
    public Document Doc { get; } = doc;
    public string Section { get; } = section;
    public string Text { get; } = text;
    /// <summary>İndekslenen kökler (Bm25Index tarafından doldurulur).</summary>
    public IReadOnlyList<string> Tokens { get; internal set; } = [];
}

/// <summary>Markdown dokümanları okur ve '##' başlıklarına göre bölümlere (chunk) ayırır.</summary>
public sealed partial class Corpus
{
    public IReadOnlyList<Document> Documents { get; }
    public IReadOnlyList<Chunk> Chunks { get; }

    private Corpus(IReadOnlyList<Document> documents, IReadOnlyList<Chunk> chunks)
    {
        Documents = documents;
        Chunks = chunks;
    }

    [GeneratedRegex(@"\A---\n(.*?)\n---\n(.*)\z", RegexOptions.Singleline)]
    private static partial Regex FrontMatter();

    [GeneratedRegex("^## ", RegexOptions.Multiline)]
    private static partial Regex SectionHeading();

    public static Corpus Load(string directory)
    {
        var docs = new List<Document>();
        var chunks = new List<Chunk>();
        var files = Directory.GetFiles(directory, "*.md").OrderBy(Path.GetFileName, StringComparer.Ordinal);
        foreach (var path in files)
        {
            var (doc, sections) = LoadDocument(path);
            if (docs.Any(d => d.DocId == doc.DocId))
                throw new InvalidDataException($"Aynı doc_id iki kez kullanılmış: {doc.DocId}");
            docs.Add(doc);
            chunks.AddRange(sections.Select((s, i) => new Chunk($"{doc.DocId}#{i + 1}", doc, s.Heading, s.Content)));
        }
        if (docs.Count == 0)
            throw new InvalidDataException($"'{directory}' klasöründe doküman bulunamadı.");
        return new Corpus(docs, chunks);
    }

    private static (Document, List<(string Heading, string Content)>) LoadDocument(string path)
    {
        // Windows'ta CRLF ile çekilmiş dosyalar da aynı şekilde işlensin.
        var raw = File.ReadAllText(path).Replace("\r\n", "\n");
        var m = FrontMatter().Match(raw);
        if (!m.Success)
            throw new InvalidDataException($"{Path.GetFileName(path)}: başta '---' ile ayrılmış meta veri bloğu bulunamadı.");

        var meta = new Dictionary<string, string>();
        foreach (var line in m.Groups[1].Value.Split('\n'))
        {
            var idx = line.IndexOf(':');
            if (idx > 0) meta[line[..idx].Trim()] = line[(idx + 1)..].Trim();
        }
        string? Get(string key) => meta.TryGetValue(key, out var v) && v.Length > 0 ? v : null;

        var docId = Get("doc_id") ?? throw new InvalidDataException($"{Path.GetFileName(path)}: doc_id eksik.");
        var doc = new Document(
            DocId: docId,
            Title: Get("title") ?? throw new InvalidDataException($"{docId}: title eksik."),
            Family: Get("family") ?? docId,
            Version: Get("version") ?? "1.0",
            EffectiveDate: DateOnly.ParseExact(
                Get("effective_date") ?? throw new InvalidDataException($"{docId}: effective_date eksik."),
                "yyyy-MM-dd", CultureInfo.InvariantCulture),
            Status: Get("status") ?? Document.ActiveStatus,
            Supersedes: Get("supersedes"),
            FileName: Path.GetFileName(path));

        var sections = SectionHeading().Split(m.Groups[2].Value).Skip(1).Select(block =>
        {
            var nl = block.IndexOf('\n');
            var heading = (nl < 0 ? block : block[..nl]).Trim();
            var content = nl < 0 ? "" : block[(nl + 1)..];
            return (heading, string.Join(' ', content.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries)));
        }).ToList();
        return (doc, sections);
    }

    /// <summary>
    /// Göreli yolu önce çalışma dizininden, sonra uygulama dizininden başlayarak üst klasörlerde arar;
    /// böylece hem depo kökünden hem proje klasöründen "dotnet run" çalışır.
    /// </summary>
    public static string ResolveDirectory(string path)
    {
        if (Path.IsPathRooted(path)) return path;
        foreach (var start in new[] { Directory.GetCurrentDirectory(), AppContext.BaseDirectory })
        {
            for (var dir = new DirectoryInfo(start); dir is not null; dir = dir.Parent)
            {
                var candidate = Path.Combine(dir.FullName, path);
                if (Directory.Exists(candidate)) return candidate;
            }
        }
        throw new DirectoryNotFoundException($"Doküman klasörü bulunamadı: {path}");
    }
}
