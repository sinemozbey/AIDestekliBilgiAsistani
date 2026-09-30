// Nova Destek Asistanı arayüzü. API'den gelen metinler her zaman düz metin (text node) olarak
// yazılır, HTML olarak ayrıştırılmaz; böylece doküman içeriği sayfaya kod enjekte edemez.
"use strict";

const $ = (sel) => document.querySelector(sel);

/** Küçük DOM yardımcısı: el("p", { class: "x" }, "metin", childNode, ...) */
function el(tag, attrs = {}, ...children) {
  const node = document.createElement(tag);
  for (const [key, value] of Object.entries(attrs)) {
    if (value === false || value == null) continue;
    if (key === "class") node.className = value;
    else if (key === "style") node.style.cssText = value;
    else node.setAttribute(key, value === true ? "" : value);
  }
  for (const child of children.flat()) {
    if (child == null || child === false) continue;
    node.append(child instanceof Node ? child : document.createTextNode(String(child)));
  }
  return node;
}

// Çizgi ikonları (24x24). Yalnızca bu sabit tanımlardan, createElementNS ile oluşturulur.
const ICONS = {
  check: ["M22 11.08V12a10 10 0 1 1-5.93-9.14", "m9 11 3 3L22 4"],
  info: ["M12 16v-4", "M12 8h.01", "M12 2a10 10 0 1 0 0 20 10 10 0 0 0 0-20z"],
  history: ["M3 12a9 9 0 1 0 9-9 9.75 9.75 0 0 0-6.74 2.74L3 8", "M3 3v5h5", "M12 7v5l4 2"],
  conflict: ["m21.73 18-8-14a2 2 0 0 0-3.48 0l-8 14A2 2 0 0 0 4 21h16a2 2 0 0 0 1.73-3", "M12 9v4", "M12 17h.01"],
  chevron: ["m9 18 6-6-6-6"],
};
function icon(name) {
  const svg = document.createElementNS("http://www.w3.org/2000/svg", "svg");
  svg.setAttribute("viewBox", "0 0 24 24");
  svg.setAttribute("aria-hidden", "true");
  for (const d of ICONS[name]) {
    const path = document.createElementNS("http://www.w3.org/2000/svg", "path");
    path.setAttribute("d", d);
    svg.append(path);
  }
  return svg;
}

const dateFmt = new Intl.DateTimeFormat("tr-TR", { day: "numeric", month: "long", year: "numeric" });
function formatDate(iso) {
  const [y, m, d] = iso.split("-").map(Number);
  return dateFmt.format(new Date(y, m - 1, d));
}

const STATUS_LABEL = { yururlukte: "Yürürlükte", yururlukten_kalkti: "Yürürlükten kalktı" };
const statusLabel = (status) =>
  el("span", { class: `status-label${status === "yururlukte" ? " active" : ""}` }, STATUS_LABEL[status] ?? status);

/** "claude-opus-5-5" -> "Claude Opus 5.5" */
function modelName(id) {
  if (!id) return "";
  const parts = id.split("-");
  const words = parts.filter((p) => !/^\d+$/.test(p)).map((p) => p[0].toUpperCase() + p.slice(1));
  const version = parts.filter((p) => /^\d+$/.test(p)).join(".");
  return [...words, version].filter(Boolean).join(" ");
}

// Doküman bilgileri (doc_id -> doküman); arama ayrıntılarında eski sürümleri işaretlemek için.
let docsById = new Map();
let docsPromise = null;
function loadDocs() {
  docsPromise ??= fetch("/api/documents")
    .then((r) => (r.ok ? r.json() : Promise.reject(new Error(`HTTP ${r.status}`))))
    .then((docs) => {
      docsById = new Map(docs.map((d) => [d.doc_id, d]));
      return docs;
    })
    .catch((e) => {
      docsPromise = null;
      throw e;
    });
  return docsPromise;
}

/* ---------- Bağlantı durumu ---------- */

async function refreshStatus() {
  const box = $("#status");
  const text = box.querySelector(".status-text");
  try {
    const r = await fetch("/health", { cache: "no-store" });
    if (!r.ok) throw new Error();
    const h = await r.json();
    const llm = h.llm_service;
    const useLlm = h.answer_mode === "llm" || (h.answer_mode === "auto" && llm.llm_available);
    if (useLlm && llm.llm_available) {
      box.dataset.state = "llm";
      text.textContent = `${modelName(llm.model)} bağlı`;
      box.title = `${h.documents} doküman, ${h.chunks} bölüm yüklü. Yanıtlar LLM ile üretiliyor.`;
    } else {
      box.dataset.state = "fallback";
      text.textContent = "Yedek mod";
      box.title = `LLM kullanılamıyor${llm.reason ? `: ${llm.reason}` : ""}. Yanıtlar doğrudan doküman metninden seçiliyor.`;
    }
  } catch {
    box.dataset.state = "down";
    text.textContent = "Servise ulaşılamıyor";
    box.title = "API yanıt vermiyor.";
  }
}

/* ---------- Soru sorma ---------- */

const form = $("#ask-form");
const input = $("#question");
const button = $("#ask-btn");
const result = $("#result");

function autosize() {
  input.style.height = "auto";
  input.style.height = `${Math.min(input.scrollHeight, 240)}px`;
}
input.addEventListener("input", autosize);
input.addEventListener("keydown", (e) => {
  if (e.key === "Enter" && !e.shiftKey && !e.isComposing) {
    e.preventDefault();
    form.requestSubmit();
  }
});

form.addEventListener("submit", async (e) => {
  e.preventDefault();
  const question = input.value.trim();
  if (question.length < 3) {
    showError("Soru en az 3 karakter olmalıdır.");
    input.focus();
    return;
  }
  setLoading(true);
  result.replaceChildren(
    el("div", { class: "loading" }, el("span", { class: "spinner" }), "Dokümanlar taranıyor ve yanıt hazırlanıyor…")
  );
  const started = performance.now();
  try {
    const r = await fetch("/api/ask?details=true", {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ question }),
    });
    const body = await r.json().catch(() => null);
    if (!r.ok) {
      const detail = body?.errors ? Object.values(body.errors).flat().join(" ") : body?.detail;
      throw new Error(detail || `Sunucu hatası (HTTP ${r.status}).`);
    }
    await loadDocs().catch(() => {});
    renderAnswer(body, { ms: performance.now() - started, cached: r.headers.get("X-Cache") === "HIT" });
  } catch (err) {
    showError(err instanceof TypeError ? "API'ye ulaşılamadı. Servisin çalıştığından emin olun." : err.message);
  } finally {
    setLoading(false);
  }
});

function setLoading(on) {
  button.disabled = on;
  button.classList.toggle("is-loading", on);
  button.setAttribute("aria-busy", on ? "true" : "false");
}

function showError(message) {
  result.replaceChildren(el("div", { class: "error", role: "alert" }, icon("info"), message));
}

/* ---------- Yanıtın gösterimi ---------- */

function answerMeta(res, { ms, cached }) {
  const timing = cached ? "önbellekten" : `${(ms / 1000).toFixed(1).replace(".", ",")} sn`;
  if (res.mode === "llm") return el("span", { class: "answer-meta", title: res.model }, `${modelName(res.model)} · ${timing}`);
  const label = res.mode === "extractive_fallback" ? "Yedek mod (LLM'e ulaşılamadı)" : "Yedek mod (LLM'siz)";
  return el("span", { class: "answer-meta" }, el("span", { class: "fallback" }, label), ` · ${timing}`);
}

function renderAnswer(res, timing) {
  const status = res.answerable
    ? el("span", { class: "answer-status ok" }, icon("check"), "Kaynaklı yanıt")
    : el("span", { class: "answer-status none" }, icon("info"), "Dokümanlarda bilgi yok");

  const body = el("div", { class: "answer-body" }, el("p", { class: "answer-text" }, res.answer));

  const decisions = [
    ...res.version_decisions.map(versionDecision),
    ...res.content_conflicts.map(conflictDecision),
    ...res.warnings.map((w) =>
      decision("decision-warning", "conflict", "Uyarı", el("p", {}, w))
    ),
  ];
  if (decisions.length) body.append(el("div", { class: "decisions" }, decisions));

  if (res.sources.length) {
    body.append(
      el("h3", { class: "section-label" }, "Kaynaklar"),
      el("ol", { class: "source-list" }, res.sources.map(sourceItem))
    );
  }

  if (res.candidates.length) body.append(diagnostics(res));

  result.replaceChildren(
    el(
      "article",
      { class: "answer-card" },
      el(
        "header",
        { class: "answer-head" },
        el("p", { class: "answer-question" }, `“${res.question}”`),
        el("div", { class: "answer-status-row" }, status, answerMeta(res, timing))
      ),
      body
    )
  );
}

function decision(cls, iconName, title, ...content) {
  return el(
    "div",
    { class: `decision ${cls}` },
    icon(iconName),
    el("div", { class: "decision-content" }, el("div", { class: "decision-title" }, title), ...content)
  );
}

function versionDecision(d) {
  const rejected = d.rejected
    .map((r) => `v${r.version} (${formatDate(r.effective_date)}, ${(STATUS_LABEL[r.status] ?? r.status).toLowerCase()})`)
    .join(", ");
  return decision(
    "decision-version",
    "history",
    "Güncel sürüm kullanıldı",
    el(
      "p",
      {},
      `“${d.selected.title}” için birden fazla sürüm bulundu. v${d.selected.version} `,
      `(${formatDate(d.selected.effective_date)}) kullanıldı; ${rejected} yanıtta kullanılmadı.`
    ),
    el(
      "details",
      { class: "more" },
      el("summary", {}, icon("chevron"), "Elenen sürümü göster"),
      d.rejected.map((r) =>
        el(
          "div",
          { class: "old-version" },
          el("div", { class: "meta" }, `v${r.version} · ${formatDate(r.effective_date)}${r.section ? ` · ${r.section}` : ""}`),
          r.excerpt && el("blockquote", { class: "quote" }, r.excerpt)
        )
      ),
      el("div", { class: "rule" }, `Seçim kuralı: ${d.rule}`)
    )
  );
}

function conflictDecision(c) {
  return decision(
    "decision-conflict",
    "conflict",
    "Dokümanlar arası çelişki çözüldü",
    el(
      "p",
      {},
      `${c.topic}: “${c.chosen.title}” (${formatDate(c.chosen.effective_date)}) ile “${c.rejected.title}” `,
      `(${formatDate(c.rejected.effective_date)}) çelişiyor. Daha güncel olan “${c.chosen.title}” esas alındı.`
    ),
    c.reason && el("p", { class: "reason" }, `Gerekçe: ${c.reason}`)
  );
}

function sourceItem(s, i) {
  return el(
    "li",
    { class: "source" },
    el("span", { class: "source-index", "aria-hidden": "true" }, String(i + 1)),
    el(
      "div",
      { class: "source-body" },
      el(
        "div",
        { class: "source-head" },
        el("span", { class: "source-title" }, s.title),
        el("span", { class: "meta" }, `v${s.version} · ${formatDate(s.effective_date)}`),
        statusLabel(s.status)
      ),
      el("div", { class: "source-section" }, `§ ${s.section}`),
      el("blockquote", { class: "quote" }, s.excerpt)
    )
  );
}

function diagnostics(res) {
  const used = new Set(res.sources.map((s) => s.chunk_id));
  const max = Math.max(...res.candidates.map((c) => c.score), 1);
  return el(
    "details",
    { class: "more diagnostics" },
    el("summary", {}, icon("chevron"), `Arama ayrıntıları (${res.candidates.length} aday bölüm)`),
    el(
      "ul",
      { class: "cands" },
      res.candidates.map((c) => {
        const doc = docsById.get(c.chunk_id.split("#")[0]);
        const old = doc && doc.status !== "yururlukte";
        return el(
          "li",
          { class: `cand${used.has(c.chunk_id) ? " used" : ""}${old ? " old" : ""}` },
          el("span", { class: "cand-id", title: c.chunk_id }, c.chunk_id),
          el("span", { class: "cand-bar" }, el("span", { style: `width:${((c.score / max) * 100).toFixed(0)}%` })),
          el(
            "span",
            { class: "cand-tags" },
            used.has(c.chunk_id) && el("span", { class: "tag tag-used" }, "kullanıldı"),
            old && el("span", { class: "tag tag-old" }, "eski sürüm"),
            el("span", { class: "cand-score" }, c.score.toFixed(2))
          )
        );
      })
    )
  );
}

/* ---------- Bilgi tabanı ---------- */

async function renderDocs() {
  const box = $("#docs");
  if (box.dataset.loaded) return;
  box.replaceChildren(el("div", { class: "loading" }, el("span", { class: "spinner" }), "Dokümanlar yükleniyor…"));
  try {
    const docs = await loadDocs();
    const families = new Map();
    for (const d of docs) families.set(d.family, [...(families.get(d.family) ?? []), d]);
    const byCurrentThenNewest = (a, b) =>
      Number(b.status === "yururlukte") - Number(a.status === "yururlukte") || b.effective_date.localeCompare(a.effective_date);
    const cards = [...families.values()]
      .map((versions) => versions.sort(byCurrentThenNewest))
      .sort((a, b) => a[0].title.localeCompare(b[0].title, "tr"))
      .map((versions) =>
        el(
          "article",
          { class: "doc-card" },
          el("h2", {}, versions[0].title),
          versions.map((d) =>
            el(
              "div",
              { class: `doc-version${d.status === "yururlukte" ? "" : " retired"}` },
              el(
                "div",
                { class: "doc-version-head" },
                el("strong", {}, `v${d.version}`),
                el("span", { class: "meta" }, formatDate(d.effective_date)),
                statusLabel(d.status),
                d.supersedes &&
                  el("span", { class: "meta" }, `v${docsById.get(d.supersedes)?.version ?? d.supersedes} sürümünün yerini alır`),
                el("span", { class: "doc-id" }, d.doc_id)
              ),
              el("ul", { class: "section-list" }, d.sections.map((s) => el("li", {}, s)))
            )
          )
        )
      );
    box.replaceChildren(...cards);
    box.dataset.loaded = "1";
  } catch {
    box.replaceChildren(el("div", { class: "error", role: "alert" }, icon("info"), "Dokümanlar yüklenemedi."));
  }
}

/* ---------- Sekmeler ---------- */

const tabs = [...document.querySelectorAll(".tab")];
function selectTab(tab) {
  for (const t of tabs) {
    const selected = t === tab;
    t.setAttribute("aria-selected", String(selected));
    t.tabIndex = selected ? 0 : -1;
    document.getElementById(t.getAttribute("aria-controls")).hidden = !selected;
  }
  if (tab.id === "tab-btn-docs") renderDocs();
  else input.focus();
}
tabs.forEach((t, i) => {
  t.addEventListener("click", () => selectTab(t));
  t.addEventListener("keydown", (e) => {
    if (e.key !== "ArrowRight" && e.key !== "ArrowLeft") return;
    const next = tabs[(i + (e.key === "ArrowRight" ? 1 : tabs.length - 1)) % tabs.length];
    next.focus();
    selectTab(next);
  });
});

refreshStatus();
setInterval(refreshStatus, 30_000);
loadDocs().catch(() => {});
input.focus();
