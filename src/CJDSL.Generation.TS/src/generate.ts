// CJDSL 静态生成库（TS 侧，对应 C# CJDSL.Generation 的薄封装）
// 路线 Y / §8.5 仅库生成：自然语言 → CJDSL DSL，零服务、不内嵌 Kestrel。
//
// 设计要点：
//  - 静态工具库形态：调用方 import 后直接 generateFromNlp(text, creds) 即可，
//    不依赖任何常驻服务、不内嵌 Kestrel（满足「不要 MAUI 内嵌 Kestrel」「不做成服务」约束）。
//  - LLM 凭证由调用方显式传入（方案选择 2）：{ apiKey, baseUrl, model }，
//    库自身零硬编码凭证，不与 CJCore .NET 配置耦合。
//  - 职责边界（重要）：本库只做「自然语言 → DSL JSON 解析」，即调 LLM + 解析 JSON 文本。
//    白名单/语义校验归属 DSL 契约层（@cj/cjdsl-react 的 validateDsl），由**调用方**统一执行
//    （DA.DSHPlug.CJDSL 的 execute 已自行 validateDsl）。这样本库零运行时依赖、可独立运行，
//    不在两个包间复制白名单逻辑，避免双轨漂移。
//  - 不重复 C# 侧的整套模板引擎，只做 LLM 调用 + 解析（薄封装）。
import { parseDslText } from "./dsl-parse";

export interface GenerationCredentials {
  /** LLM API Key（OpenAI 兼容格式，适配 DeepSeek / CJCore 网关）。 */
  apiKey: string;
  /** LLM 基础地址，如 https://api.deepseek.com 或 CJCore 网关；结尾斜杠可选。 */
  baseUrl: string;
  /** 模型名，默认 deepseek-chat。 */
  model?: string;
  /** 超时（毫秒），默认 30000。 */
  timeoutMs?: number;
}

export interface GenerateResult {
  ok: boolean;
  dsl?: unknown;
  errors: string[];
}

// CJDSL v1 生成系统提示：约束模型只输出可被白名单校验的 DSL JSON。
// 与 C# 侧 GenerateFromNlpAsync 的 prompt 意图保持一致（收敛版，不含整套模板）。
const SYSTEM_PROMPT = [
  "你是 CJDSL v1 的界面生成器。根据用户意图，输出符合 CJDSL v1 语法的 DSL JSON。",
  "规则：",
  "1. 只输出一个 JSON 对象（page 结构），不要任何解释、不要 markdown 围栏。",
  "2. 顶层结构形如 { \"page\": { \"title\": string, \"components\": [ ... ] } }。",
  "3. components 仅使用白名单组件类型：container / text / button / input / select / table / chart / image / card。",
  "4. 每个 component 至少含 type 与 id；input/select 含 name、label；button 含 text、action。",
  "5. 不要输出任何白名单之外的字段或组件类型。",
].join("\n");

function buildMessages(text: string) {
  return [
    { role: "system", content: SYSTEM_PROMPT },
    { role: "user", content: text },
  ];
}

/**
 * 静态生成入口：自然语言 → CJDSL DSL（仅解析 JSON，白名单校验由调用方执行）。
 * @param text 用户意图描述
 * @param creds LLM 凭证（apiKey 必填，baseUrl 必填）
 */
export async function generateFromNlp(
  text: string,
  creds: GenerationCredentials,
): Promise<GenerateResult> {
  const apiKey = (creds?.apiKey ?? "").trim();
  const baseUrl = (creds?.baseUrl ?? "").trim().replace(/\/+$/, "");
  if (!apiKey) {
    return { ok: false, errors: ["生成凭证缺失：apiKey 必填（静态生成库由调用方显式传入）"] };
  }
  if (!baseUrl) {
    return { ok: false, errors: ["生成凭证缺失：baseUrl 必填（静态生成库由调用方显式传入）"] };
  }
  if (!text || !text.trim()) {
    return { ok: false, errors: ["生成输入为空：text 必填"] };
  }

  const model = creds.model?.trim() || "deepseek-chat";
  const timeoutMs = creds.timeoutMs && creds.timeoutMs > 0 ? creds.timeoutMs : 30000;

  try {
    const ctrl = new AbortController();
    const timer = setTimeout(() => ctrl.abort(), timeoutMs);
    try {
      const res = await fetch(`${baseUrl}/chat/completions`, {
        method: "POST",
        headers: {
          "Content-Type": "application/json",
          Authorization: `Bearer ${apiKey}`,
        },
        body: JSON.stringify({
          model,
          messages: buildMessages(text),
          temperature: 0.2,
          response_format: { type: "json_object" },
        }),
        signal: ctrl.signal,
      });
      if (!res.ok) {
        const errText = await res.text().catch(() => "");
        return { ok: false, errors: [`LLM 接口返回 HTTP ${res.status}: ${errText.slice(0, 200)}`] };
      }
      const body = await res.json();
      const content: string =
        body?.choices?.[0]?.message?.content ?? body?.output?.text ?? "";
      if (!content.trim()) {
        return { ok: false, errors: ["LLM 返回内容为空"] };
      }
      // 仅解析 LLM 输出的 DSL 文本为 JSON（白名单校验由调用方统一执行）
      const parsed = parseDslText(content);
      if (!parsed.ok) {
        return { ok: false, errors: [`生成结果 DSL 解析失败：${parsed.errors.join("；")}`] };
      }
      return { ok: true, dsl: parsed.dsl };
    } finally {
      clearTimeout(timer);
    }
  } catch (e) {
    return { ok: false, errors: [`生成调用失败: ${(e as Error)?.message ?? String(e)}`] };
  }
}
