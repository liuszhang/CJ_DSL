// CJDSL v1 DSL 模型与白名单校验（host/client 共用）
//  - 类型对齐 CJDSL.Blazor DslModels（v1 子集）
//  - validateDsl：递归白名单校验，非法结构返回错误清单；危险属性/事件/表达式一律拒绝
//  - parseDslText：解析 ```dsl 围栏文本为 JSON

// ── v1 支持矩阵（与 CJDSL.React 渲染器对齐） ──────────────────────────
export const V1_COMPONENT_TYPES = new Set([
  // 布局
  "card", "grid", "stack", "divider", "form",
  // 展示
  "textDisplay", "table", "alert", "chip", "badge",
  // 表单
  "text", "number", "select", "textarea", "date", "switch",
  // 交互
  "button", "iconButton",
  // 图表
  "chart",
]);

export const V1_EVENT_HANDLERS = new Set([
  "submit", "apiCall", "setValue", "chain", "showToast", "navigate",
]);

export const V1_VALIDATION_RULES = new Set([
  "required", "minLength", "maxLength", "regex", "min", "max",
]);

export const V1_DATA_SOURCE_TYPES = new Set(["static", "api"]);

// 危险 props：任何层级的组件 props 都不允许出现（防 XSS / 逃逸）
const FORBIDDEN_PROPS = new Set([
  "dangerouslySetInnerHTML", "innerHTML", "outerHTML", "srcdoc", "javascript",
]);

const FORBIDDEN_EXPR_PATTERN = /(document|window|globalThis|process|require|import|fetch|eval|Function|constructor|prototype|__proto__|localStorage|sessionStorage)\b/i;

export interface DslValidationRule {
  type: string;
  message?: string;
  pattern?: string;
  min?: number;
  max?: number;
  expression?: string;
}

export interface DslEvent {
  type: string;
  handler: string;
  params?: Record<string, any>;
  confirm?: { title?: string; message?: string; confirmText?: string; cancelText?: string };
  debounceMs?: number;
}

export interface DslDataSource {
  type: string;
  endpoint?: string;
  method?: string;
  code?: string;
  staticData?: any;
  params?: Record<string, any>;
  searchParam?: string;
  pagination?: boolean;
  serverSide?: boolean;
  dataPath?: string;
}

export interface DslComponent {
  id?: string;
  type: string;
  label?: string;
  fieldName?: string;
  dataBind?: string;
  span?: number;
  visibleIf?: string;
  disabledIf?: string;
  props?: Record<string, any>;
  children?: DslComponent[];
  events?: DslEvent[];
  dataSource?: DslDataSource;
  validationRules?: DslValidationRule[];
  style?: Record<string, any>;
  helpText?: string;
}

export interface DslPage {
  id?: string;
  title?: string;
  layout?: string;
  components?: DslComponent[];
}

// ── 校验 ─────────────────────────────────────────────────────────────

export interface DslValidationResult {
  ok: boolean;
  errors: string[];
  cleaned?: DslComponent;
  pages?: DslComponent[];
}

function isPlainObject(v: unknown): v is Record<string, any> {
  return typeof v === "object" && v !== null && !Array.isArray(v);
}

/** 校验单个组件（递归）。返回错误列表；错误为空表示合法。 */
function validateComponent(node: unknown, path: string, errors: string[], out: DslComponent | null): DslComponent | null {
  if (!isPlainObject(node)) {
    errors.push(`${path}: 组件必须是对象`);
    return null;
  }
  const type = String(node.type ?? "");
  if (!V1_COMPONENT_TYPES.has(type)) {
    errors.push(`${path}: 组件类型 "${type || "(空)"}" 不在 v1 白名单（允许: ${[...V1_COMPONENT_TYPES].join(", ")}）`);
    return null;
  }

  // props 危险属性检查
  if (node.props !== undefined) {
    if (!isPlainObject(node.props)) {
      errors.push(`${path}: props 必须是对象`);
      return null;
    }
    for (const key of Object.keys(node.props)) {
      if (FORBIDDEN_PROPS.has(key)) {
        errors.push(`${path}: props 含危险属性 "${key}"`);
        return null;
      }
      const v = node.props[key];
      if (typeof v === "string" && /^\s*javascript:/i.test(v)) {
        errors.push(`${path}: props.${key} 含 javascript: URL`);
        return null;
      }
    }
  }

  // 表达式白名单（可见性/禁用/校验表达式一律拒绝危险代码）
  for (const exprField of ["visibleIf", "disabledIf"] as const) {
    const expr = node[exprField];
    if (typeof expr === "string" && expr.trim() !== "" && FORBIDDEN_EXPR_PATTERN.test(expr)) {
      errors.push(`${path}: ${exprField} 表达式含非法引用`);
      return null;
    }
  }

  // 事件白名单
  if (node.events !== undefined) {
    if (!Array.isArray(node.events)) {
      errors.push(`${path}: events 必须是数组`);
      return null;
    }
    for (let i = 0; i < node.events.length; i++) {
      const ev = node.events[i];
      if (!isPlainObject(ev)) {
        errors.push(`${path}.events[${i}]: 事件必须是对象`);
        return null;
      }
      const handler = String(ev.handler ?? "");
      if (!V1_EVENT_HANDLERS.has(handler)) {
        errors.push(`${path}.events[${i}]: handler "${handler}" 不在 v1 白名单（允许: ${[...V1_EVENT_HANDLERS].join(", ")}）`);
        return null;
      }
    }
  }

  // 校验规则白名单
  if (node.validationRules !== undefined) {
    if (!Array.isArray(node.validationRules)) {
      errors.push(`${path}: validationRules 必须是数组`);
      return null;
    }
    for (let i = 0; i < node.validationRules.length; i++) {
      const rule = node.validationRules[i];
      if (!isPlainObject(rule)) {
        errors.push(`${path}.validationRules[${i}]: 校验规则必须是对象`);
        return null;
      }
      const rt = String(rule.type ?? "");
      if (!V1_VALIDATION_RULES.has(rt)) {
        errors.push(`${path}.validationRules[${i}]: 规则类型 "${rt}" 不在 v1 白名单（允许: ${[...V1_VALIDATION_RULES].join(", ")}）`);
        return null;
      }
    }
  }

  // 数据源白名单
  if (node.dataSource !== undefined) {
    if (!isPlainObject(node.dataSource)) {
      errors.push(`${path}: dataSource 必须是对象`);
      return null;
    }
    const st = String(node.dataSource.type ?? "");
    if (!V1_DATA_SOURCE_TYPES.has(st)) {
      errors.push(`${path}: dataSource.type "${st}" 不在 v1 白名单（允许: static/api）`);
      return null;
    }
    if (st === "api") {
      const endpoint = String(node.dataSource.endpoint ?? "");
      if (!/^https?:\/\//i.test(endpoint)) {
        errors.push(`${path}: dataSource.endpoint 必须是 http(s):// URL`);
        return null;
      }
    }
  }

  // 图表 v1 仅支持 Pie/Donut（SVG 直出）
  if (type === "chart") {
    const chartType = String(node.props?.ChartType ?? node.props?.chartType ?? "");
    if (!["pie", "donut"].includes(chartType.toLowerCase())) {
      errors.push(`${path}: chart 仅支持 ChartType=Pie/Donut（v1 SVG 直出）`);
      return null;
    }
  }

  // 递归子组件
  const cleaned: DslComponent = { ...node } as DslComponent;
  if (node.children !== undefined) {
    if (!Array.isArray(node.children)) {
      errors.push(`${path}: children 必须是数组`);
      return null;
    }
    const childrenOut: DslComponent[] = [];
    for (let i = 0; i < node.children.length; i++) {
      const child = validateComponent(node.children[i], `${path}.children[${i}]`, errors, null);
      if (child) childrenOut.push(child);
    }
    cleaned.children = childrenOut;
  }
  return cleaned;
}

/** 校验页面/组件树 DSL。page 可为单组件或 {components:[...]} 页面。 */
export function validateDsl(input: unknown): DslValidationResult {
  const errors: string[] = [];
  if (input == null) {
    return { ok: false, errors: ["DSL 为空"] };
  }

  // 页面形态：{ type: ..., ... } 或 { components: [...] }
  if (Array.isArray(input)) {
    const cleaned: DslComponent[] = [];
    for (let i = 0; i < input.length; i++) {
      const c = validateComponent(input[i], `components[${i}]`,  errors, null);
      if (c) cleaned.push(c);
    }
    if (errors.length > 0) return { ok: false, errors };
    return { ok: true, errors, pages: cleaned };
  }

  if (isPlainObject(input)) {
    if (Array.isArray(input.components)) {
      const pages: DslComponent[] = [];
      for (let i = 0; i < input.components.length; i++) {
        const c = validateComponent(input.components[i], `page.components[${i}]`, errors, null);
        if (c) pages.push(c);
      }
      if (errors.length > 0) return { ok: false, errors };
      return { ok: true, errors, pages };
    }
    const c = validateComponent(input, "dsl", errors, null);
    if (errors.length > 0 || !c) return { ok: false, errors };
    return { ok: true, errors, cleaned: c };
  }

  return { ok: false, errors: ["DSL 必须是对象或数组"] };
}

/** 解析 + 校验工具输入的 DSL JSON 文本。返回 { ok, dsl, errors }。 */
export function parseDslText(text: string): { ok: boolean; dsl?: unknown; errors: string[] } {
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
