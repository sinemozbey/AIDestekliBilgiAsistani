"""LLM servisi: .NET API'nin seçtiği doküman bölümleriyle Claude'dan yanıt üretir.

Arama, sürüm seçimi ve "bilgi yok" kararı .NET tarafındadır (api/). Bu servis yalnızca
verilen kaynaklara dayanarak yapılandırılmış bir yanıt üretir.
"""
from functools import lru_cache

from fastapi import Depends, FastAPI, HTTPException
from pydantic import BaseModel, Field

from .config import settings
from .llm import ClaudeAnswerer, LLMAnswer, LLMError, Source

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


@lru_cache
def get_answerer() -> ClaudeAnswerer | None:
    return ClaudeAnswerer(settings.anthropic_model, settings.llm_effort) if settings.has_api_key else None


@app.get("/health")
def health(answerer: ClaudeAnswerer | None = Depends(get_answerer)):
    return {"status": "ok", "llm_available": answerer is not None, "model": settings.anthropic_model}


@app.post("/generate", response_model=GenerateResponse)
def generate(req: GenerateRequest, answerer: ClaudeAnswerer | None = Depends(get_answerer)):
    # Senkron uç nokta: FastAPI bunu iş parçacığı havuzunda çalıştırır, LLM çağrısı olay döngüsünü bloklamaz.
    if answerer is None:
        raise HTTPException(503, "ANTHROPIC_API_KEY tanımlı değil; LLM kullanılamıyor.")
    try:
        result = answerer.answer(req.question, req.sources)
    except LLMError as e:
        raise HTTPException(502, str(e)) from e
    return GenerateResponse(**result.model_dump(), model=answerer.model)
