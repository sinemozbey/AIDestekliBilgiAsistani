using System.ComponentModel;
using System.Diagnostics;
using System.Globalization;
using System.Text.Json;
using System.Text.Json.Nodes;
using System.Text.RegularExpressions;
using BilgiAsistani.Core;
using Microsoft.Extensions.Caching.Memory;
using Microsoft.OpenApi;
using Scalar.AspNetCore;

var builder = WebApplication.CreateBuilder(args);

builder.Services.ConfigureHttpJsonOptions(o => o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.SnakeCaseLower);
builder.Services.AddBilgiAsistani(builder.Configuration);
builder.Services.AddMemoryCache();
builder.Services.AddProblemDetails();
builder.Services.AddOpenApi(o =>
{
    o.AddDocumentTransformer((doc, _, _) =>
    {
        doc.Info.Title = "Bilgi Asistanı API";
        doc.Info.Description =
            "Nova Ev Teknolojileri destek ekibi için dokümanlara dayalı Türkçe soru-yanıt API'si. " +
            "Her yanıt kullanılan doküman bölümünü gösterir; bilgi yoksa bunu açıkça belirtir; " +
            "çelişen sürümlerde güncel olanı seçer ve bunu açıklar.";
        // Arayüzde asıl işlev (soru-yanıt) üstte görünsün.
        doc.Tags = new HashSet<OpenApiTag>
        {
            new() { Name = "Soru-yanıt", Description = "Soru sorma, doküman arama ve doküman listesi." },
            new() { Name = "Sistem", Description = "Servisin ve LLM bağlantısının durumu." },
        };
        return Task.CompletedTask;
    });
    // Arayüzde soru kutusu, denenmeye hazır bir örnekle açılsın.
    o.AddSchemaTransformer((schema, ctx, _) =>
    {
        if (ctx.JsonTypeInfo.Type == typeof(AskRequest))
            schema.Examples = [new JsonObject { ["question"] = "Ürünü kaç gün içinde iade edebilirim?" }];
        return Task.CompletedTask;
    });
});

var app = builder.Build();

app.UseExceptionHandler();
// Web arayüzü (wwwroot/index.html) ana adreste yayınlanır.
app.UseDefaultFiles();
app.UseStaticFiles();
app.MapOpenApi();
// Geliştiriciler için tarayıcıdan denenebilir API arayüzü: /scalar/
app.MapScalarApiReference(o => o.WithTitle("Bilgi Asistanı API"));

// İndeksi ilk istekte değil, açılışta kur; doküman hataları hemen görünsün.
var pipeline = app.Services.GetRequiredService<QaPipeline>();
app.Logger.LogInformation("{Docs} doküman, {Chunks} bölüm yüklendi. Yanıt modu: {Mode}",
    pipeline.Corpus.Documents.Count, pipeline.Corpus.Chunks.Count, pipeline.Options.AnswerMode);

var api = app.MapGroup("/api").WithTags("Soru-yanıt");
var tr = CultureInfo.GetCultureInfo("tr-TR");
var cacheTtl = TimeSpan.FromMinutes(app.Configuration.GetValue("AnswerCacheMinutes", 10));

api.MapPost("/ask", async (AskRequest req, QaPipeline qa, IMemoryCache cache, HttpContext ctx,
    ILogger<Program> log, CancellationToken ct,
    [Description("true ise alıntılar, reddedilen sürümler, içerik çelişkileri, yanıt modu ve arama adayları da döner.")]
    bool details = false) =>
{
    // Varsayılan yanıt sadedir (yanıt + kaynak + tek cümlelik açıklamalar); ?details=true alıntıları,
    // reddedilen sürüm metinlerini ve arama skorlarını da döndürür.
    IResult Respond(AskResponse r) => details ? Results.Ok(r) : Results.Ok(AskSummary.From(r));

    var question = req.Question?.Trim() ?? "";
    if (question.Length is < 3 or > 1000)
        return Results.ValidationProblem(new Dictionary<string, string[]>
        {
            ["question"] = ["Soru 3 ile 1000 karakter arasında olmalıdır."],
        });

    // Aynı soru (büyük/küçük harf ve boşluk farkı gözetmeksizin) tekrar sorulduğunda LLM maliyeti tekrarlanmaz.
    var key = "ask:" + Regex.Replace(question.ToLower(tr), @"\s+", " ");
    if (cache.TryGetValue(key, out AskResponse? cached))
    {
        ctx.Response.Headers["X-Cache"] = "HIT";
        return Respond(cached!);
    }

    var sw = Stopwatch.StartNew();
    var answer = await qa.AskAsync(question, ct);
    log.LogInformation("Soru yanıtlandı: answerable={Answerable} mode={Mode} sources={Sources} süre={Elapsed}ms",
        answer.Answerable, answer.Mode, string.Join(",", answer.Sources.Select(s => s.ChunkId)), sw.ElapsedMilliseconds);

    // LLM hatası nedeniyle yedek moda düşülmüş yanıtlar önbelleğe alınmaz.
    if (answer.Warnings.Count == 0)
        cache.Set(key, answer, cacheTtl);
    ctx.Response.Headers["X-Cache"] = "MISS";
    return Respond(answer);
})
.WithName("Ask")
.Produces<AskSummary>()
.ProducesValidationProblem()
.WithSummary("Soru sor")
.WithDescription("Dokümanlara dayanarak Türkçe bir soruyu yanıtlar. Varsayılan yanıt: answerable, answer, " +
                 "sources (doküman, sürüm, bölüm, bölüm metni) ve notes (sürüm/çelişki kararları). " +
                 "details=true: reddedilen sürümler, uygulanan kural, içerik çelişkileri, yanıt modu ve arama adayları.");

api.MapGet("/search", (string q, QaPipeline qa, int topK = 5) =>
    q.Trim().Length < 2 || topK is < 1 or > 20
        ? Results.ValidationProblem(new Dictionary<string, string[]>
            { ["q"] = ["Sorgu en az 2 karakter, top_k 1-20 arasında olmalıdır."] })
        : Results.Ok(new SearchResponse(q.Trim(), qa.Search(q.Trim(), topK).Select(SourceRef.From).ToList())))
.WithName("Search")
.Produces<SearchResponse>()
.WithSummary("Doküman ara")
.WithDescription("Doküman bölümlerinde anahtar kelime (BM25) araması yapar; yanıt üretmez, skorlarıyla bölümleri döndürür.");

api.MapGet("/documents", (QaPipeline qa) => Results.Ok(qa.Documents()))
.WithName("Documents")
.Produces<List<DocumentInfo>>()
.WithSummary("Dokümanları listele")
.WithDescription("Bilgi tabanındaki dokümanları; sürüm, yürürlük tarihi, durum ve bölüm başlıklarıyla listeler.");

app.MapGet("/health", async (QaPipeline qa, CancellationToken ct) =>
{
    var llm = await qa.GetLlmHealthAsync(ct);
    return Results.Ok(new
    {
        status = "ok",
        answer_mode = qa.Options.AnswerMode,
        documents = qa.Corpus.Documents.Count,
        chunks = qa.Corpus.Chunks.Count,
        llm_service = llm is null ? (object)"ulaşılamıyor" : llm,
    });
})
.WithTags("Sistem")
.WithSummary("Sistem durumu")
.WithDescription("Yüklenen doküman ve bölüm sayısını, yanıt modunu ve LLM servisinin durumunu gösterir.");

app.Run();

public partial class Program;
