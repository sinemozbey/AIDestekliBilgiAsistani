"""Claude ile kaynaklara dayalı yanıt üretimi (yapılandırılmış çıktı)."""
from datetime import date
from html import escape

import anthropic
from pydantic import BaseModel, Field

SYSTEM_PROMPT = """Sen Nova Ev Teknolojileri müşteri destek ekibine yardım eden bir bilgi asistanısın.
Destek temsilcilerinin sorularını YALNIZCA sana verilen <source> etiketli doküman bölümlerine dayanarak Türkçe yanıtlarsın.

Kurallar:
- Genel bilgini veya tahminini kullanma. Kaynaklarda açıkça yazmayan hiçbir bilgiyi (süre, ücret, adım, ürün özelliği) ekleme.
- Sorunun cevabı kaynaklarda yoksa ya da yalnızca konuyla ilgili ama soruyu yanıtlamayan bilgi varsa answerable=false yap ve answer alanında hangi bilginin dokümanlarda bulunmadığını tek cümleyle belirt.
- Soru birden fazla parçadan oluşuyorsa ve bir kısmı kaynaklarda yoksa, bulunan kısmı yanıtla ve eksik kısmı açıkça belirt.
- Kaynaklar birbiriyle çelişirse effective_date değeri daha yeni olan ve status="yururlukte" olan kaynağı esas al. Her çelişkiyi conflicts listesine ekle: hangi konu, seçilen ve reddedilen kaynak kimliği, gerekçe.
- Yalnızca sorulan soruyu yanıtla. Soruda istenmeyen ek ayrıntıları (ücretler, süreler, başka süreçler) ekleme: bu ayrıntıların güncel hâli sana verilen kaynaklarda olmayabilir.
- used_source_ids alanına yalnızca yanıtında gerçekten dayandığın kaynakların id değerlerini yaz.
- Yanıt kısa ve net olsun (en fazla 3-4 cümle), temsilcinin müşteriye aktarabileceği bir dille yazılsın."""


class Source(BaseModel):
    id: str
    title: str
    version: str
    effective_date: date
    status: str
    section: str
    text: str


class LLMConflict(BaseModel):
    topic: str
    chosen_source_id: str
    rejected_source_id: str
    reason: str


class LLMAnswer(BaseModel):
    answerable: bool = Field(description="Kaynaklar soruyu yanıtlamaya yetiyor mu?")
    answer: str
    used_source_ids: list[str]
    conflicts: list[LLMConflict]


class LLMError(RuntimeError):
    pass


def format_sources(sources: list[Source]) -> str:
    return "\n".join(
        f'<source id="{escape(s.id)}" doc="{escape(s.title)}" version="{escape(s.version)}" '
        f'effective_date="{s.effective_date}" status="{escape(s.status)}" section="{escape(s.section)}">\n'
        f"{escape(s.text, quote=False)}\n</source>"
        for s in sources
    )


class ClaudeAnswerer:
    def __init__(self, model: str, effort: str):
        self.model = model
        self.effort = effort
        self.client = anthropic.Anthropic()

    def answer(self, question: str, sources: list[Source]) -> LLMAnswer:
        user = f"<sources>\n{format_sources(sources)}\n</sources>\n\n<question>{escape(question)}</question>"
        try:
            response = self.client.beta.messages.parse(
                model=self.model,
                max_tokens=8000,
                system=SYSTEM_PROMPT,
                messages=[{"role": "user", "content": user}],
                output_config={"effort": self.effort},
                output_format=LLMAnswer,
                # Güvenlik sınıflandırıcısı reddederse istek sunucu tarafında uygun modele yönlendirilir.
                betas=["server-side-fallback-2026-07-01"],
                fallbacks="default",
            )
        except anthropic.AuthenticationError as e:
            raise LLMError("Anthropic API anahtarı geçersiz") from e
        except anthropic.RateLimitError as e:
            raise LLMError("Anthropic API hız sınırına ulaşıldı") from e
        except anthropic.APIStatusError as e:
            raise LLMError(f"Anthropic API hatası ({e.status_code}): {e.message}") from e
        except anthropic.APIConnectionError as e:
            raise LLMError("Anthropic API'ye bağlanılamadı") from e

        if response.stop_reason == "refusal":
            raise LLMError("Model isteği yanıtlamayı reddetti")
        if response.parsed_output is None:
            raise LLMError(f"Model yapılandırılmış yanıt döndürmedi (stop_reason={response.stop_reason})")
        return response.parsed_output
