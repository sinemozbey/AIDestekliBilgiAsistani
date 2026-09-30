// Değerlendirme setini çalıştırır; beklenen ve gerçek sonuçları karşılaştırır.
//
// Kullanım (api/ klasöründen):
//   dotnet run --project src/BilgiAsistani.Eval -- --mode extractive
//   dotnet run --project src/BilgiAsistani.Eval -- --mode llm            (LLM servisi çalışıyor olmalı)
//   dotnet run --project src/BilgiAsistani.Eval -- --api-url http://localhost:5080   (çalışan API üzerinden uçtan uca)
using System.Net.Http.Json;
using System.Text.Encodings.Web;
using System.Text.Json;
using BilgiAsistani.Core;
using BilgiAsistani.Eval;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;

var opts = ParseArgs(args);
var questionsPath = opts.GetValueOrDefault("questions") ?? Corpus.ResolveDirectory("eval") + "/questions.json";
var outDir = opts.GetValueOrDefault("out") ?? Path.GetDirectoryName(Path.GetFullPath(questionsPath))!;
var apiUrl = opts.GetValueOrDefault("api-url");

var json = new JsonSerializerOptions(LlmServiceClient.Json)
{
    WriteIndented = true,
    Encoder = JavaScriptEncoder.UnsafeRelaxedJsonEscaping, // Türkçe karakterler okunabilir kalsın
};
var cases = JsonSerializer.Deserialize<List<EvalCase>>(File.ReadAllText(questionsPath), json)!;

Func<string, Task<AskResponse>> ask;
if (apiUrl is not null)
{
    var http = new HttpClient { BaseAddress = new Uri(apiUrl), Timeout = TimeSpan.FromMinutes(3) };
    ask = async q =>
    {
        // Değerlendirme kontrolleri ayrıntılı yanıttaki kaynak ve karar alanlarını kullanır.
        var r = await http.PostAsJsonAsync("/api/ask?details=true", new AskRequest(q), json);
        r.EnsureSuccessStatusCode();
        return (await r.Content.ReadFromJsonAsync<AskResponse>(json))!;
    };
}
else
{
    // Ortam değişkenleri (Assistant__*, LlmService__*) ve komut satırı ayarları uygulanır.
    var config = new ConfigurationBuilder()
        .AddEnvironmentVariables()
        .AddInMemoryCollection(opts.TryGetValue("mode", out var mode)
            ? new Dictionary<string, string?> { ["Assistant:AnswerMode"] = mode }
            : [])
        .Build();
    var qa = new ServiceCollection().AddLogging().AddBilgiAsistani(config).BuildServiceProvider()
        .GetRequiredService<QaPipeline>();
    ask = q => qa.AskAsync(q);
}

var rows = new List<EvalRow>();
foreach (var c in cases)
{
    var resp = await ask(c.Question);
    var checks = Grader.Grade(c, resp);
    rows.Add(new EvalRow(c, resp, checks, checks.Values.All(v => v)));
    Console.WriteLine($"[{(rows[^1].Passed ? "OK  " : "HATA")}] {c.Id} {c.Question}");
}

var modes = rows.Select(r => r.Response.Mode).Distinct().Order().ToList();
var suffix = modes.Contains(AnswerModes.Llm) ? AnswerModes.Llm : AnswerModes.Extractive;
var model = rows.Select(r => r.Response.Model).FirstOrDefault(m => m is not null);
var via = apiUrl is not null ? $"{apiUrl} (.NET API → FastAPI LLM servisi)" : "süreç içi .NET QaPipeline";
File.WriteAllText(Path.Combine(outDir, $"results_{suffix}.json"), JsonSerializer.Serialize(rows, json));
File.WriteAllText(Path.Combine(outDir, $"results_{suffix}.md"), Report.Render(rows, modes, model, via));
Console.WriteLine($"\n{rows.Count(r => r.Passed)}/{rows.Count} başarılı -> {Path.Combine(outDir, $"results_{suffix}.md")}");

static Dictionary<string, string> ParseArgs(string[] args)
{
    var d = new Dictionary<string, string>();
    for (var i = 0; i + 1 < args.Length; i += 2)
    {
        if (!args[i].StartsWith("--")) throw new ArgumentException($"Beklenmeyen argüman: {args[i]}");
        d[args[i][2..]] = args[i + 1];
    }
    return d;
}
