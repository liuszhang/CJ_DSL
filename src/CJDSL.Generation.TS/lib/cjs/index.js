// src/dsl-parse.ts
function parseDslText(text) {
  const trimmed = (text ?? "").trim();
  if (!trimmed) return { ok: false, errors: ["dsl \u4E3A\u7A7A"] };
  const fence = trimmed.match(/^```(?:dsl|json)?\s*\n([\s\S]*?)\n```$/);
  const raw = fence ? fence[1].trim() : trimmed;
  try {
    const parsed = JSON.parse(raw);
    return { ok: true, dsl: parsed, errors: [] };
  } catch (e) {
    return { ok: false, errors: [`DSL JSON \u89E3\u6790\u5931\u8D25: ${e.message}`] };
  }
}

// src/generate.ts
var SYSTEM_PROMPT = [
  "\u4F60\u662F CJDSL v1 \u7684\u754C\u9762\u751F\u6210\u5668\u3002\u6839\u636E\u7528\u6237\u610F\u56FE\uFF0C\u8F93\u51FA\u7B26\u5408 CJDSL v1 \u8BED\u6CD5\u7684 DSL JSON\u3002",
  "\u89C4\u5219\uFF1A",
  "1. \u53EA\u8F93\u51FA\u4E00\u4E2A JSON \u5BF9\u8C61\uFF08page \u7ED3\u6784\uFF09\uFF0C\u4E0D\u8981\u4EFB\u4F55\u89E3\u91CA\u3001\u4E0D\u8981 markdown \u56F4\u680F\u3002",
  '2. \u9876\u5C42\u7ED3\u6784\u5F62\u5982 { "page": { "title": string, "components": [ ... ] } }\u3002',
  "3. components \u4EC5\u4F7F\u7528\u767D\u540D\u5355\u7EC4\u4EF6\u7C7B\u578B\uFF1Acontainer / text / button / input / select / table / chart / image / card\u3002",
  "4. \u6BCF\u4E2A component \u81F3\u5C11\u542B type \u4E0E id\uFF1Binput/select \u542B name\u3001label\uFF1Bbutton \u542B text\u3001action\u3002",
  "5. \u4E0D\u8981\u8F93\u51FA\u4EFB\u4F55\u767D\u540D\u5355\u4E4B\u5916\u7684\u5B57\u6BB5\u6216\u7EC4\u4EF6\u7C7B\u578B\u3002"
].join("\n");
function buildMessages(text) {
  return [
    { role: "system", content: SYSTEM_PROMPT },
    { role: "user", content: text }
  ];
}
async function generateFromNlp(text, creds) {
  const apiKey = (creds?.apiKey ?? "").trim();
  const baseUrl = (creds?.baseUrl ?? "").trim().replace(/\/+$/, "");
  if (!apiKey) {
    return { ok: false, errors: ["\u751F\u6210\u51ED\u8BC1\u7F3A\u5931\uFF1AapiKey \u5FC5\u586B\uFF08\u9759\u6001\u751F\u6210\u5E93\u7531\u8C03\u7528\u65B9\u663E\u5F0F\u4F20\u5165\uFF09"] };
  }
  if (!baseUrl) {
    return { ok: false, errors: ["\u751F\u6210\u51ED\u8BC1\u7F3A\u5931\uFF1AbaseUrl \u5FC5\u586B\uFF08\u9759\u6001\u751F\u6210\u5E93\u7531\u8C03\u7528\u65B9\u663E\u5F0F\u4F20\u5165\uFF09"] };
  }
  if (!text || !text.trim()) {
    return { ok: false, errors: ["\u751F\u6210\u8F93\u5165\u4E3A\u7A7A\uFF1Atext \u5FC5\u586B"] };
  }
  const model = creds.model?.trim() || "deepseek-chat";
  const timeoutMs = creds.timeoutMs && creds.timeoutMs > 0 ? creds.timeoutMs : 3e4;
  try {
    const ctrl = new AbortController();
    const timer = setTimeout(() => ctrl.abort(), timeoutMs);
    try {
      const res = await fetch(`${baseUrl}/chat/completions`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${apiKey}`
        },
        body: JSON.stringify({
          model,
          messages: buildMessages(text),
          temperature: 0.2,
          response_format: { type: "json_object" }
        }),
        signal: ctrl.signal
      });
      if (!res.ok) {
        const errText = await res.text().catch(() => "");
        return { ok: false, errors: [`LLM \u63A5\u53E3\u8FD4\u56DE HTTP ${res.status}: ${errText.slice(0, 200)}`] };
      }
      const body = await res.json();
      const content = body?.choices?.[0]?.message?.content ?? body?.output?.text ?? "";
      if (!content.trim()) {
        return { ok: false, errors: ["LLM \u8FD4\u56DE\u5185\u5BB9\u4E3A\u7A7A"] };
      }
      const parsed = parseDslText(content);
      if (!parsed.ok) {
        return { ok: false, errors: [`\u751F\u6210\u7ED3\u679C DSL \u89E3\u6790\u5931\u8D25\uFF1A${parsed.errors.join("\uFF1B")}`] };
      }
      return { ok: true, dsl: parsed.dsl };
    } finally {
      clearTimeout(timer);
    }
  } catch (e) {
    return { ok: false, errors: [`\u751F\u6210\u8C03\u7528\u5931\u8D25: ${e?.message ?? String(e)}`] };
  }
}
export {
  generateFromNlp
};
