using System.Net;
using System.Net.Http.Json;
using System.Text.Json;
using BilgiAsistani.Core;
using Microsoft.AspNetCore.Mvc.Testing;
using Microsoft.AspNetCore.TestHost;
using Microsoft.Extensions.DependencyInjection;

namespace BilgiAsistani.Tests;

public class ApiTests
{
    private static (HttpClient Client, FakeLlmClient Llm) Create()
    {
        var llm = new FakeLlmClient
        {
            // LLM'e bölümler doküman sırasıyla gider; sahte yanıt, iade süresi sorusunun dayandığı bölümü seçer.
            Respond = req => new GenerateResponse(true, "30 gün içinde iade edebilirsiniz.",
                [(req.Sources.FirstOrDefault(s => s.Section == "İade Süresi") ?? req.Sources[0]).Id], [], "fake-model"),
        };
        var factory = new WebApplicationFactory<Program>().WithWebHostBuilder(b =>
        {
            b.UseSetting("Assistant:AnswerMode", "llm");
            b.ConfigureTestServices(s => s.AddSingleton<ILlmClient>(llm));
        });
        return (factory.CreateClient(), llm);
    }

    [Fact]
    public async Task Ask_ByDefault_ReturnsSingleAnswerWithSourcesAndNotes()
    {
        var (client, _) = Create();

        var response = await client.PostAsJsonAsync("/api/ask", new { question = "Ürünü kaç gün içinde iade edebilirim?" });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.Equal(["answerable", "answer", "sources", "notes"], body.EnumerateObject().Select(p => p.Name));
        var source = body.GetProperty("sources")[0];
        Assert.Equal("İade ve Değişim Politikası", source.GetProperty("document").GetString());
        Assert.Equal("2.0", source.GetProperty("version").GetString());
        Assert.Equal("İade Süresi", source.GetProperty("section").GetString());
        Assert.Contains("30 gün içinde iade", source.GetProperty("excerpt").GetString());
        // Sürüm seçimi tek cümleyle açıklanır; eski sürümün metni varsayılan yanıtta yer almaz.
        var note = Assert.Single(body.GetProperty("notes").EnumerateArray()).GetString();
        Assert.Contains("v2.0 (yürürlük: 2025-03-01, yürürlükte) seçildi", note);
        Assert.DoesNotContain("14 gün", body.ToString());
    }

    [Fact]
    public void Summary_ExplainsContentConflictsAndWarningsInNotes()
    {
        var v2 = new DocVersionRef("iade-politikasi-v2", "İade ve Değişim Politikası", "2.0", new DateOnly(2025, 3, 1), "yururlukte");
        var kargo = new DocVersionRef("kargo-teslimat", "Kargo ve Teslimat Bilgileri", "1.0", new DateOnly(2024, 2, 12), "yururlukte");
        var response = new AskResponse("soru", true, "yanıt", [], [],
            [new ContentConflict("iade kargo ücreti", v2, kargo, "daha yeni")], "extractive_fallback", null, [],
            ["LLM kullanılamadı, çıkarımsal moda geçildi."]);

        var notes = AskSummary.From(response).Notes;

        Assert.Equal(2, notes.Count);
        Assert.Equal("'İade ve Değişim Politikası' (2025-03-01) ile 'Kargo ve Teslimat Bilgileri' (2024-02-12) çelişiyor " +
                     "(iade kargo ücreti); daha güncel olan 'İade ve Değişim Politikası' esas alındı.", notes[0]);
        Assert.Contains("LLM kullanılamadı", notes[1]);
    }

    [Fact]
    public async Task Ask_WithDetails_ReturnsSnakeCaseResponseWithSourcesAndVersionDecisions()
    {
        var (client, _) = Create();

        var response = await client.PostAsJsonAsync("/api/ask?details=true", new { question = "Ürünü kaç gün içinde iade edebilirim?" });

        response.EnsureSuccessStatusCode();
        var body = await response.Content.ReadFromJsonAsync<JsonElement>();
        Assert.True(body.GetProperty("answerable").GetBoolean());
        Assert.Equal("iade-politikasi-v2", body.GetProperty("sources")[0].GetProperty("doc_id").GetString());
        Assert.Equal("2025-03-01", body.GetProperty("sources")[0].GetProperty("effective_date").GetString());
        Assert.Equal("iade-politikasi-v1",
            body.GetProperty("version_decisions")[0].GetProperty("rejected")[0].GetProperty("doc_id").GetString());
    }

    [Fact]
    public async Task Ask_SameQuestionTwice_SecondIsServedFromCache()
    {
        var (client, llm) = Create();

        var first = await client.PostAsJsonAsync("/api/ask", new { question = "İade süresi nedir?" });
        var second = await client.PostAsJsonAsync("/api/ask", new { question = "  iade   SÜRESİ nedir? " });

        Assert.Equal("MISS", first.Headers.GetValues("X-Cache").Single());
        Assert.Equal("HIT", second.Headers.GetValues("X-Cache").Single());
        Assert.Single(llm.Requests);
    }

    [Theory]
    [InlineData("")]
    [InlineData("  a ")]
    public async Task Ask_InvalidQuestion_Returns400(string question)
    {
        var (client, llm) = Create();

        var response = await client.PostAsJsonAsync("/api/ask", new { question });

        Assert.Equal(HttpStatusCode.BadRequest, response.StatusCode);
        Assert.Empty(llm.Requests);
    }

    [Fact]
    public async Task Search_And_Documents_Work()
    {
        var (client, _) = Create();

        var search = await client.GetFromJsonAsync<JsonElement>("/api/search?q=garanti%20s%C3%BCresi&topK=2");
        var docs = await client.GetFromJsonAsync<JsonElement>("/api/documents");

        Assert.Equal("garanti-kosullari#1", search.GetProperty("results")[0].GetProperty("chunk_id").GetString());
        Assert.Equal(2, search.GetProperty("results").GetArrayLength());
        Assert.Equal(10, docs.GetArrayLength());
    }

    [Fact]
    public async Task Root_ServesWebUi_AndScalarServesApiReference()
    {
        var client = new WebApplicationFactory<Program>().CreateClient();

        var root = await client.GetAsync("/");
        var script = await client.GetAsync("/app.js");
        var scalar = await client.GetAsync("/scalar/");

        root.EnsureSuccessStatusCode();
        Assert.Equal("text/html", root.Content.Headers.ContentType?.MediaType);
        Assert.Contains("<title>Nova Destek Asistanı</title>", await root.Content.ReadAsStringAsync());
        script.EnsureSuccessStatusCode();
        // Arayüz, API metinlerini HTML olarak değil düz metin olarak yazar.
        var js = await script.Content.ReadAsStringAsync();
        Assert.DoesNotContain(".innerHTML", js);
        Assert.DoesNotContain("insertAdjacentHTML", js);
        scalar.EnsureSuccessStatusCode();
        Assert.Equal("text/html", scalar.Content.Headers.ContentType?.MediaType);
    }

    [Fact]
    public async Task OpenApi_DescribesAskEndpointWithExampleQuestion()
    {
        var (client, _) = Create();

        var doc = await client.GetFromJsonAsync<JsonElement>("/openapi/v1.json");

        Assert.Equal("Bilgi Asistanı API", doc.GetProperty("info").GetProperty("title").GetString());
        var schemas = doc.GetProperty("components").GetProperty("schemas");
        Assert.Equal("Ürünü kaç gün içinde iade edebilirim?",
            schemas.GetProperty("AskRequest").GetProperty("examples")[0].GetProperty("question").GetString());
        Assert.True(schemas.TryGetProperty("AskSummary", out _));
        var askParams = doc.GetProperty("paths").GetProperty("/api/ask").GetProperty("post").GetProperty("parameters");
        Assert.Contains(askParams.EnumerateArray(), p => p.GetProperty("name").GetString() == "details");
    }

    [Fact]
    public async Task Health_ReportsCorpusAndLlmService()
    {
        var (client, _) = Create();

        var health = await client.GetFromJsonAsync<JsonElement>("/health");

        Assert.Equal(10, health.GetProperty("documents").GetInt32());
        Assert.True(health.GetProperty("llm_service").GetProperty("llm_available").GetBoolean());
    }
}
