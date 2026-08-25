// 本地 DSL 文本解析（纯函数，零依赖）：从 LLM 输出文本解析出 DSL JSON。
// 仅做 JSON.parse + 容忍 ```dsl/```json 代码围栏；白名单/语义校验不在此处（归属 @cj/cjdsl-react 的 validateDsl，由调用方执行）。
// 从 @cj/cjdsl-react 的 parseDslText 提取纯解析部分，使生成库自包含、可独立运行。

export interface ParseResult {
  ok: boolean;
  dsl?: unknown;
  errors: string[];
}

export function parseDslText(text: string): ParseResult {
  const trimmed = (text ?? "").trim();
  if (!trimmed) return { ok: false, errors: ["dsl 为空"] };
  // 容忍 ```dsl 围栏（模型常输出代码块）
  const fence = trimmed.match(/^```(?:dsl|json)?\s*\n([\s\S]*?)\n```$/);
  const raw = fence ? fence[1].trim() : trimmed;
  try {
    const parsed = JSON.parse(raw);
    return { ok: true, dsl: parsed, errors: [] };
  } catch (e) {
    return { ok: false, errors: [`DSL JSON 解析失败: ${(e as Error).message}`] };
  }
}
