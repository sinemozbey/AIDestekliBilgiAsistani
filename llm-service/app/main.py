"""LLM servisi: .NET API'nin seçtiği doküman bölümleriyle Claude'dan yanıt üretir.

Arama, sürüm seçimi ve "bilgi yok" kararı .NET tarafındadır (api/). Bu servis yalnızca
verilen kaynaklara dayanarak yapılandırılmış bir yanıt üretir.
"""
import logging
import threading
from dataclasses import dataclass

from fastapi import Depends, FastAPI, HTTPException
from pydantic import BaseModel, Field

from .config import settings
from .llm import ClaudeAnswerer, LLMAnswer, LLMError, Source

log = logging.getLogger("uvicorn.error")

app = FastAPI(
    title="Bilgi Asistanı LLM Servisi",
    description="Verilen doküman bölümlerine dayanarak Claude ile Türkçe yanıt üretir.",
    version="1.0.0",
)


class GenerateRequest(BaseModel):
    question: str = Field(..., min_length=1, max_length=1000)
    sources: list[Source] = Field(..., min_length=1, max_length=20)


class GenerateResponse(LLMAnswer):
    model: str


@dataclass(frozen=True)
class LlmState:
    answerer: ClaudeAnswerer | None
    reason: str | None = None  # LLM kullanılamıyorsa nedeni


_state: LlmState | None = None
_state_lock = threading.Lock()


def get_llm() -> LlmState:
    """LLM'in kullanılabilir olup olmadığını ilk çağrıda doğrular ve sonucu saklar.

    Anahtar yoksa, geçersizse (ör. başka bir sağlayıcının anahtarı) ya da modele erişim yoksa servis
    "kullanılamıyor" bildirir ve .NET API çıkarımsal moda geçer. Geçici hatalar saklanmaz; sonraki
    çağrıda yeniden denenir. Anahtar değiştirilirse servis yeniden başlatılmalıdır.
    """
    global _state
    with _state_lock:
        if _state is not None:
            return _state
        if not settings.has_api_key:
            state = LlmState(None, "ANTHROPIC_API_KEY tanımlı değil")
        else:
            answerer = ClaudeAnswerer(settings.anthropic_model, settings.llm_effort)
            try:
                answerer.verify()
                state = LlmState(answerer)
            except LLMError as e:
                if e.transient:
                    log.warning("LLM doğrulanamadı (geçici): %s", e)
                    return LlmState(None, str(e))
                state = LlmState(None, str(e))
        log.info("LLM durumu: %s", "kullanılabilir" if state.answerer else f"kullanılamıyor ({state.reason})")
        _state = state
        return state


@app.get("/health")
def health(llm: LlmState = Depends(get_llm)):
    return {
        "status": "ok",
        "llm_available": llm.answerer is not None,
        "model": settings.anthropic_model,
        "reason": llm.reason,
    }


@app.post("/generate", response_model=GenerateResponse)
def generate(req: GenerateRequest, llm: LlmState = Depends(get_llm)):
    # Senkron uç nokta: FastAPI bunu iş parçacığı havuzunda çalıştırır, LLM çağrısı olay döngüsünü bloklamaz.
    answerer = llm.answerer
    if answerer is None:
        raise HTTPException(503, f"LLM kullanılamıyor: {llm.reason}")
    try:
        result = answerer.answer(req.question, req.sources)
    except LLMError as e:
        raise HTTPException(502, str(e)) from e
    return GenerateResponse(**result.model_dump(), model=answerer.model)
