// dslPayload.ts — 全局文本 DSL 载荷检测与提取（P0 全局渲染）
//   检测 assistant/message 文本块中的 CJDSL_PAYLOAD: 前缀 JSON 或 ```dsl 代码块，
//   返回 { payload, mode, dsl } 供 chat.node 渲染器消费。
//   导出 toDslNode 归一化函数，复用现有 DslRenderer。
import type { DslNode } from "./DslRenderer";

export const PAYLOAD_PREFIX = "CJDSL_PAYLOAD:";

export interface DslPayloadDetect {
  /** CJDSL_PAYLOAD 解析出的完整载荷；```dsl 块时为 null */
  payload: any;
  /** 渲染模式（card/form/dashboard） */
  mode: string;
  /** 归一化 DSL（根节点或页面容器） */
  dsl: unknown;
}

/** 根节点 type → mode 映射（```dsl 块推断）：card→card、form→form、其他→card */
export function inferMode(root: unknown): string {
  const t = (root as Record<string, any>)?.type;
  if (t === "form") return "form";
  return "card";
}

/**
 * 从字符串中提取第一个 JSON 对象/数组子串（容忍前后 UI 噪声、不可见字符、markdown 围栏变体）。
 * 返回 null 表示无法提取合法 JSON。
 */
export function extractJsonSubstring(text: string): unknown | null {
  const span = extractJsonSpan(text);
  return span ? span.value : null;
}

/**
 * 与 extractJsonSubstring 相同的提取，但额外返回命中子串的位置（相对传入文本，
 * end 为闭区间索引），供需要「从原文中剔除载荷」的消费端使用
 * （如隐藏 assistant 消息里的源 DSL 代码块后判断剩余内容）。
 */
export function extractJsonSpan(text: string): { value: unknown; start: number; end: number } | null {
  // 先尝试整段去噪后直接解析
  const cleaned = text
    .replace(/^```(?:dsl|json)?\s*\n?/, "")
    .replace(/\n?```$/i, "")
    .replace(/[\u0000-\u001F\u007F-\u009F]/g, "") // 去不可见控制字符
    .replace(/复制代码?|复制$/g, "") // 去 UI 复制按钮噪声
    .trim();
  try {
    return { value: JSON.parse(cleaned), start: 0, end: text.length - 1 };
  } catch {
    // 忽略整段失败，继续尝试子串提取
  }
  // 退化：定位首个 { 或 [ 到匹配的尾括号
  const start = text.search(/[[{]/);
  if (start < 0) return null;
  const open = text[start];
  const close = open === "{" ? "}" : "]";
  let depth = 0;
  let inStr = false;
  let esc = false;
  for (let i = start; i < text.length; i++) {
    const ch = text[i];
    if (inStr) {
      if (esc) esc = false;
      else if (ch === "\\") esc = true;
      else if (ch === '"') inStr = false;
      continue;
    }
    if (ch === '"') inStr = true;
    else if (ch === open) depth++;
    else if (ch === close) {
      depth--;
      if (depth === 0) {
        const slice = text.slice(start, i + 1);
        try {
          return { value: JSON.parse(slice), start, end: i };
        } catch {
          return null;
        }
      }
    }
  }
  return null;
}

/** 将 dsl 载荷归一化为 DslNode（兼容 ```dsl 围栏字符串、页面容器 components） */
export function toDslNode(dsl: unknown): DslNode | null {
  if (dsl === null || dsl === undefined) return null;
  if (typeof dsl === "string") {
    const parsed = extractJsonSubstring(dsl);
    if (parsed && typeof parsed === "object") return parsed as DslNode;
    return null;
  }
  if (typeof dsl === "object") {
    const rec = dsl as Record<string, any>;
    // 兼容页面容器（components 数组）与单组件树
    if (rec.components && Array.isArray(rec.components)) {
      return { type: "card", id: rec.id || "page", children: rec.components } as DslNode;
    }
    return rec as DslNode;
  }
  return null;
}

/** 从单个文本块检测 DSL 载荷；无命中返回 null */
export function detectDslPayloadInText(text: unknown): DslPayloadDetect | null {
  if (typeof text !== "string" || text.trim() === "") return null;

  // 1) CJDSL_PAYLOAD: 前缀 JSON（兼容工具载荷 {ok,render:{mode,dsl}} 与直接载荷 {mode,dsl}）
  const idx = text.indexOf(PAYLOAD_PREFIX);
  if (idx >= 0) {
    const raw = text.slice(idx + PAYLOAD_PREFIX.length).trim();
    try {
      const payload = JSON.parse(raw);
      const r = payload?.render;
      const mode = r?.mode ?? payload?.mode ?? "card";
      const dsl = r?.dsl ?? payload?.dsl ?? null;
      if (dsl !== null && dsl !== undefined) {
        return { payload, mode, dsl };
      }
    } catch {
      // 前缀块解析失败 → 继续尝试 ```dsl 块
    }
  }

  // 2) ```dsl 代码块（兼容 DSL 直觉写法）
  const m = text.match(/```dsl\s*\n?([\s\S]*?)\n?```/);
  if (m) {
    // 优先用提取器（容忍块内/块外 UI 噪声），退化再按纯块内容解析
    const root = extractJsonSubstring(m[1]) ?? extractJsonSubstring(text);
    if (root && typeof root === "object") {
      return { payload: null, mode: inferMode(root), dsl: root };
    }
  }

  // 3) 裸 JSON 兜底：聊天 UI 渲染代码块后，传入的 content.text 可能已不含 ```dsl 围栏，
  //    仅剩裸 JSON（前后可能带「复制」按钮噪声）。直接提取首个 {…} 子串，
  //    且要求含 type 字段（DSL 根节点必要特征）以避免误把普通 JSON 当 DSL。
  const bare = extractJsonSubstring(text);
  if (bare && typeof bare === "object" && !(bare as Record<string, any>).components) {
    const rec = bare as Record<string, any>;
    if (typeof rec.type === "string") {
      return { payload: null, mode: inferMode(rec), dsl: rec };
    }
  }
  return null;
}

/**
 * 从任意对象中提取可检测的文本字符串：优先 rec.text，其次 rec.content（字符串）。
 * DSH 聊天系统可能把代码块类内容放在 type/kind 为 markdown/code/content 的块里，
 * 不能只认 "text"——只要块里含字符串文本就尝试检测。
 */
function extractBlockText(b: unknown): string | null {
  if (!b || typeof b !== "object") return null;
  const rec = b as Record<string, any>;
  if (typeof rec.text === "string" && rec.text.trim() !== "") return rec.text;
  if (typeof rec.content === "string" && rec.content.trim() !== "") return rec.content;
  return null;
}

/** 遍历文本块列表（assistant/message content 或 assistant blocks），返回首个命中载荷 */
export function detectDslPayload(blocks: unknown): DslPayloadDetect | null {
  if (!Array.isArray(blocks)) return null;
  for (const b of blocks) {
    const text = extractBlockText(b);
    if (text !== null) {
      const det = detectDslPayloadInText(text);
      if (det) return det;
    }
  }
  return null;
}
