// DSL 解析/格式化纯函数（自 cjdsl-page.ts 拆分）
//   全部为无副作用纯函数，供 CjdslPage 主类与 JsonViewerController 复用。

/** 解析 dsl 属性原始 JSON；退化：属性未设置时尝试读取 innerHTML JSON（便于 <cjdsl-page>{...}</cjdsl-page> 写法） */
export function parseDslSource(
  rawAttr: string | null,
  innerHtml: string,
): { parsed: unknown; rawSource: string } {
  let parsed: unknown = null;
  let raw = rawAttr;
  if (raw) {
    try {
      parsed = JSON.parse(raw);
    } catch {
      parsed = null;
    }
  }
  // 退化：dsl 属性未设置时尝试读取 innerHTML JSON
  let rawSource = raw ?? "";
  if (!parsed && innerHtml.trim()) {
    rawSource = innerHtml.trim();
    try {
      parsed = JSON.parse(rawSource);
    } catch {
      parsed = null;
    }
  }
  return { parsed, rawSource };
}

/** 解析 context 属性 JSON；非法/缺失返回空对象 */
export function parseContextJson(raw: string | null): Record<string, any> {
  if (!raw) return {};
  try {
    return JSON.parse(raw) || {};
  } catch {
    return {};
  }
}

/** 解析 submitted 属性（兼容 "true"/"false"/"1"/"0"）；未显式给出返回 undefined（保持默认未提交） */
export function parseSubmittedAttribute(raw: string | null): boolean | undefined {
  if (raw == null) return undefined;
  return raw === "true" || raw === "1";
}

/** 格式化源 JSON（JSON.stringify(JSON.parse(raw), null, 2)；非 JSON 时原样返回） */
export function formatJson(raw: string): string {
  if (!raw) return "（无 DSL 内容）";
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
}

/** 计算业务对象编码（缺省取 DSL 根节点 id / 用户上下文 objectCode） */
export function computeObjectCode(dslId: unknown, userContext: Record<string, any>): string {
  return String(dslId || userContext?.objectCode || "dsl");
}
