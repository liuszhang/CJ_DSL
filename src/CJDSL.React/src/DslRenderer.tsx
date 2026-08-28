// DSL → React 递归渲染器（v1 子集）
//  安全：文本经 React 转义；危险 props 白名单禁用（dangerouslySetInnerHTML / javascript: 链接）；
//        style 仅允许白名单键；不 spread 未知 props。
//  状态：DslStore 承载（data.<fieldName>）；visibleIf/disabledIf 走 expr.ts 白名单求值。
//  事件：EventDispatcher 分发（submit/apiCall/setValue/chain/showToast/navigate，confirm/onSuccess）。
//  校验：validate.ts（required/minLength/maxLength/regex/min/max）。
//  后端依赖通过 EventCallbacks 兜底（onSubmit/onApiCall），避免绑定具体 HTTP 端点。
import React, { useCallback, useEffect, useMemo, useRef, useState } from "react";
import { DslStore } from "./store";
import { evalDslExpr } from "./expr";
import { validateField, type ValidationRule } from "./validate";
import { EventDispatcher, type DslEvent, type EventCallbacks, type FormValues, type SubmitContext } from "./events";
import { buildDonutSvg } from "./svg";

// ── 类型 ────────────────────────────────────────────────────────────
export interface DslNode {
  type: string;
  id?: string;
  label?: string;
  fieldName?: string;
  span?: number;
  visibleIf?: string;
  disabledIf?: string;
  dataBind?: string;
  helpText?: string;
  props?: Record<string, any>;
  children?: DslNode[];
  events?: DslEvent[];
  validationRules?: ValidationRule[];
  dataSource?: Record<string, any>;
  style?: Record<string, any>;
}

export interface RendererCallbacks extends EventCallbacks {
  /** 渲染模式（card/form/dashboard） */
  mode?: string;
}

interface RendererProps {
  root: DslNode;
  store: DslStore;
  callbacks: RendererCallbacks;
}

// ── 常量 ────────────────────────────────────────────────────────────
const STYLE_KEYS = new Set(["class", "color", "backgroundColor", "margin", "padding", "width", "height"]);
const DISABLED_PROP_KEYS = new Set([
  "dangerouslySetInnerHTML",
  "innerHTML",
  "onLoad",
  "onError",
  "srcDoc",
  "srcdoc",
]);
const SELECT_ITEM_FIELDS = new Set(["value", "label", "disabled", "group"]);

function pickStyle(style?: Record<string, any>): React.CSSProperties | undefined {
  if (!style || typeof style !== "object") return undefined;
  const out: React.CSSProperties = {};
  for (const [k, v] of Object.entries(style)) {
    if (STYLE_KEYS.has(k) && (typeof v === "string" || typeof v === "number")) {
      (out as Record<string, any>)[k] = v;
    }
  }
  return Object.keys(out).length > 0 ? out : undefined;
}

function itemsOf(node: DslNode): { value: string; label: string; disabled?: boolean }[] {
  const items: any[] = node.props?.items ?? node.props?.Items ?? [];
  return items.map((it: any) => {
    if (typeof it === "string") return { value: it, label: it };
    const rec = (it ?? {}) as Record<string, any>;
    const value = rec.value ?? rec.Value ?? "";
    const label = rec.label ?? rec.Label ?? String(value);
    const disabled = !!rec.disabled || !!rec.Disabled;
    return { value: String(value), label: String(label), disabled };
  });
}

function safeValue(node: DslNode, store: DslStore): unknown {
  const field = node.fieldName;
  if (field) return store.get(`data.${field}`);
  if (node.dataBind) return store.get(node.dataBind);
  return undefined;
}

function escAttr(s: unknown): string {
  return String(s ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" }[c] as string));
}

function isSafeLink(href: unknown): boolean {
  const h = String(href ?? "");
  return h.startsWith("/") || h.startsWith("#") || h.startsWith("http://") || h.startsWith("https://") || h.startsWith("mailto:");
}

// ── 主渲染器 ────────────────────────────────────────────────────────
export function DslRenderer(props: RendererProps) {
  const { root, store, callbacks } = props;
  const [, setVersion] = useState(0);
  const storeRef = useRef(store);
  storeRef.current = store;

  useEffect(() => {
    const unsub = store.subscribe(() => setVersion((v) => v + 1));
    return unsub;
  }, [store]);

  // 表单值收集
  const values: FormValues = useMemo(() => {
    const out: FormValues = {};
    const walk = (n: DslNode) => {
      if (n.fieldName) out[n.fieldName] = store.get(`data.${n.fieldName}`);
      if (n.children) n.children.forEach(walk);
    };
    walk(root);
    return out;
    // 订阅 store 变化由 useEffect 触发重渲染，此处依赖 root/store 即可
  }, [root, store]);

  const dispatcher = useMemo(() => new EventDispatcher(), []);
  const [validationErrors, setValidationErrors] = useState<Record<string, string[]>>({});

  const validateForm = useCallback((): boolean => {
    const errs: Record<string, string[]> = {};
    const walk = (n: DslNode) => {
      if (n.fieldName && n.validationRules && n.validationRules.length > 0) {
        const r = validateField(store.get(`data.${n.fieldName}`), n.validationRules);
        if (!r.valid) errs[n.fieldName] = r.errors;
      }
      if (n.children) n.children.forEach(walk);
    };
    walk(root);
    setValidationErrors(errs);
    return Object.keys(errs).length === 0;
  }, [root, store]);

  const handleEvent = useCallback(
    async (ev: DslEvent) => {
      const formId = typeof root.id === "string" ? root.id : undefined;
      const ctxValues: FormValues = {};
      const walk = (n: DslNode) => {
        if (n.fieldName) ctxValues[n.fieldName] = storeRef.current.get(`data.${n.fieldName}`);
        if (n.children) n.children.forEach(walk);
      };
      walk(root);

      await dispatcher.dispatch(ev, {
        store: storeRef.current,
        formId,
        values: ctxValues,
        validateForm,
        callbacks: {
          onToast: (msg, severity) => callbacks.onToast?.(msg, severity),
          onNavigate: (path) => callbacks.onNavigate?.(path),
          onSubmit: async (submitCtx: SubmitContext) => {
            if (callbacks.onSubmit) return await callbacks.onSubmit(submitCtx);
            return { ok: false, message: "未配置 onSubmit 回调" };
          },
          onApiCall: async (params, formValues) => {
            if (callbacks.onApiCall) return await callbacks.onApiCall(params, formValues);
            return { ok: false, message: "未配置 onApiCall 回调" };
          },
        },
      });
    },
    [root, dispatcher, validateForm, callbacks],
  );

  const setField = useCallback(
    (fieldName: string, value: unknown) => {
      store.set(`data.${fieldName}`, value);
      setValidationErrors((prev) => ({ ...prev, [fieldName]: [] }));
    },
    [store],
  );

  return (
    <div className="cjdsl-root" style={pickStyle(root.style)} data-cjdsl-type={root.type}>
      {root.children && root.children.length > 0 ? (
        root.children.map((child, i) => (
          <DslNodeView
            key={child.id || `n${i}`}
            node={child}
            store={store}
            values={values}
            validationErrors={validationErrors}
            setField={setField}
            onEvent={handleEvent}
          />
        ))
      ) : (
        <DslNodeView node={root} store={store} values={values} validationErrors={validationErrors} setField={setField} onEvent={handleEvent} />
      )}
    </div>
  );
}

// ── 节点视图（递归） ────────────────────────────────────────────────
interface NodeViewProps {
  node: DslNode;
  store: DslStore;
  values: FormValues;
  validationErrors: Record<string, string[]>;
  setField: (field: string, value: unknown) => void;
  onEvent: (ev: DslEvent) => Promise<void>;
}

function DslNodeView({ node, store, values, validationErrors, setField, onEvent }: NodeViewProps) {
  const visible = evalDslExpr(node.visibleIf, store);
  if (visible === false) return null;

  switch (node.type) {
    case "card":
    case "grid":
    case "stack":
    case "divider":
    case "form":
      return <ContainerView node={node} store={store} values={values} validationErrors={validationErrors} setField={setField} onEvent={onEvent} />;
    case "textDisplay":
      return <TextDisplayView node={node} store={store} />;
    case "table":
      return <TableView node={node} store={store} />;
    case "alert":
      return <AlertView node={node} />;
    case "chip":
      return <ChipView node={node} />;
    case "badge":
      return <BadgeView node={node} />;
    case "text":
    case "number":
    case "select":
    case "textarea":
    case "date":
    case "switch":
      return (
        <FieldView
          node={node}
          store={store}
          values={values}
          validationErrors={validationErrors}
          setField={setField}
        />
      );
    case "button":
    case "iconButton":
      return <ButtonView node={node} store={store} onEvent={onEvent} />;
    case "chart":
      return <ChartView node={node} />;
    default:
      return (
        <div style={{ color: "#c62828", fontSize: 12, padding: "6px 10px", border: "1px dashed #ef9a9a", borderRadius: 4 }}>
          未支持的组件类型：{escAttr(node.type)}（DSL v1 白名单外）
        </div>
      );
  }
}

// ── 容器 ────────────────────────────────────────────────────────────
function ContainerView(props: NodeViewProps & { node: DslNode }) {
  const { node, store, values, validationErrors, setField, onEvent } = props;

  if (node.type === "divider") {
    return <hr style={{ border: "none", borderTop: "1px solid rgba(0,0,0,0.12)", margin: "8px 0" }} />;
  }

  const children = node.children ?? [];
  const inner = children.map((child, i) => (
    <DslNodeView key={child.id || `c${i}`} node={child} store={store} values={values} validationErrors={validationErrors} setField={setField} onEvent={onEvent} />
  ));

  const isForm = node.type === "form";
  return (
    <div
      className={`cjdsl-${node.type}`}
      data-cjdsl-id={node.id}
      style={{
        display: node.type === "stack" ? "flex" : undefined,
        flexDirection: node.type === "stack" ? (node.props?.direction === "row" ? "row" : "column") : undefined,
        gap: node.type === "stack" ? 8 : undefined,
        border: isForm ? "1px solid rgba(0,0,0,0.1)" : undefined,
        borderRadius: isForm ? 8 : undefined,
        padding: isForm ? 12 : undefined,
        margin: isForm ? "8px 0" : undefined,
        ...(node.style ? (pickStyle(node.style) as object) : {}),
      }}
    >
      {node.type === "grid"
        ? children.map((child, i) => (
            <div key={child.id || `g${i}`} style={{ width: `${Math.min(Math.max(child.span ?? 12, 1), 12) * (100 / 12)}%`, display: "inline-block", verticalAlign: "top", padding: "0 4px", boxSizing: "border-box" }}>
              <DslNodeView node={child} store={store} values={values} validationErrors={validationErrors} setField={setField} onEvent={onEvent} />
            </div>
          ))
        : inner}
      {isForm && node.props?.showFooter !== false && (
        <div style={{ marginTop: 10, textAlign: "right" }}>
          {node.props?.footerButtons?.map?.((btn: DslNode, i: number) => (
            <DslNodeView key={btn.id || `fb${i}`} node={{ ...btn, type: "button" }} store={store} values={values} validationErrors={validationErrors} setField={setField} onEvent={onEvent} />
          ))}
        </div>
      )}
    </div>
  );
}

// ── 展示组件 ────────────────────────────────────────────────────────
function TextDisplayView({ node, store }: { node: DslNode; store: DslStore }) {
  let text: unknown = node.props?.text ?? node.props?.Text ?? node.props?.content ?? node.props?.Content;
  if (text === undefined && node.dataBind) text = store.get(node.dataBind);
  if (text === undefined) text = node.label ?? "";
  const typo = node.props?.typo ?? node.props?.Typo ?? "body1";
  const size = typo === "h1" ? 28 : typo === "h2" ? 24 : typo === "h3" ? 20 : typo === "h4" ? 17 : typo === "h5" ? 15 : typo === "h6" ? 13 : 14;
  const color = node.props?.color ?? node.props?.Color;
  return (
    <div style={{ fontSize: size, color: typeof color === "string" ? color : undefined, margin: "4px 0", whiteSpace: "pre-wrap", wordBreak: "break-word" }}>
      {String(text ?? "")}
    </div>
  );
}

function TableView({ node, store }: { node: DslNode; store: DslStore }) {
  const columns: { name?: string; label?: string; value?: string; render?: string }[] = node.props?.columns ?? node.props?.Columns ?? [];
  const data: any[] = node.props?.items ?? node.props?.Items ?? node.props?.rows ?? node.props?.Rows ?? [];
  const finalData = data.length > 0 ? data : (store.get(node.dataBind ?? "datasource.items") as any[]) ?? [];
  if (finalData.length === 0) return <div style={{ color: "#999", padding: 6 }}>（无数据）</div>;

  const effectiveCols = columns.length > 0 ? columns : Object.keys(finalData[0] ?? {}).map((k) => ({ name: k, label: k }));
  const getValue = (row: any, col: { name?: string; value?: string }) => {
    const key = col.value ?? col.name ?? "";
    return (row as Record<string, any>)?.[key] ?? "";
  };
  return (
    <table style={{ width: "100%", borderCollapse: "collapse", fontSize: 13 }}>
      <thead>
        <tr>
          {effectiveCols.map((c, i) => (
            <th key={i} style={{ borderBottom: "1px solid rgba(0,0,0,0.12)", padding: "6px 8px", textAlign: "left", fontWeight: 600 }}>
              {String(c.label ?? c.name ?? "")}
            </th>
          ))}
        </tr>
      </thead>
      <tbody>
        {finalData.map((row, ri) => (
          <tr key={ri}>
            {effectiveCols.map((c, ci) => (
              <td key={ci} style={{ borderBottom: "1px solid rgba(0,0,0,0.06)", padding: "6px 8px" }}>
                {String(getValue(row, c) ?? "")}
              </td>
            ))}
          </tr>
        ))}
      </tbody>
    </table>
  );
}

function AlertView({ node }: { node: DslNode }) {
  const severity = node.props?.severity ?? node.props?.Severity ?? "info";
  const colorMap: Record<string, string> = { info: "#0277BD", success: "#2E7D32", warning: "#F57C00", error: "#C62828" };
  const bgMap: Record<string, string> = { info: "#E1F5FE", success: "#E8F5E9", warning: "#FFF3E0", error: "#FFEBEE" };
  return (
    <div style={{ background: bgMap[severity] ?? bgMap.info, color: colorMap[severity] ?? colorMap.info, borderRadius: 6, padding: "8px 12px", fontSize: 13, margin: "6px 0" }}>
      {String(node.props?.text ?? node.props?.message ?? node.props?.content ?? node.label ?? "")}
    </div>
  );
}

function ChipView({ node }: { node: DslNode }) {
  return (
    <span style={{ display: "inline-block", background: "rgba(0,0,0,0.08)", borderRadius: 12, padding: "2px 10px", fontSize: 12, margin: "2px 4px 2px 0" }}>
      {String(node.props?.text ?? node.props?.label ?? node.label ?? "")}
    </span>
  );
}

function BadgeView({ node }: { node: DslNode }) {
  const color = node.props?.color ?? node.props?.Color ?? "#1976D2";
  return (
    <span style={{ display: "inline-block", background: String(color), color: "#fff", borderRadius: 10, padding: "1px 8px", fontSize: 11, margin: "0 4px 0 0" }}>
      {String(node.props?.text ?? node.props?.content ?? node.label ?? "")}
    </span>
  );
}

// ── 表单字段 ────────────────────────────────────────────────────────
function FieldView({ node, store, values, validationErrors, setField }: NodeViewProps) {
  const field = node.fieldName;
  const value = field ? (store.get(`data.${field}`) as string | number | boolean | undefined) ?? "" : "";
  const required = node.props?.Required === true || node.props?.required === true;
  const disabledBase = evalDslExpr(node.disabledIf, store) === true;
  // 提交乐观锁：表单已提交（__cjdsl_submitted）后，字段整体只读/禁用，防重复填写
  const submitted = store.get("__cjdsl_submitted") === true;
  const errors = field ? (validationErrors[field] ?? []) : [];

  const baseStyle: React.CSSProperties = {
    display: "flex",
    flexDirection: "column",
    gap: 4,
    margin: "6px 0",
  };
  const labelStyle: React.CSSProperties = { fontSize: 13, color: "rgba(0,0,0,0.66)", fontWeight: 500 };
  const lockBg = disabledBase || submitted ? "#f5f5f5" : "#fff";
  const lockColor = disabledBase || submitted ? "#9e9e9e" : "inherit";
  const inputStyle: React.CSSProperties = {
    border: errors.length > 0 ? "1px solid #C62828" : "1px solid rgba(0,0,0,0.22)",
    borderRadius: 4,
    padding: "6px 10px",
    fontSize: 14,
    outline: "none",
    background: lockBg,
    color: lockColor,
    fontFamily: "inherit",
  };
  const helpStyle: React.CSSProperties = { fontSize: 12, color: "#888" };
  const errorStyle: React.CSSProperties = { fontSize: 12, color: "#C62828" };

  if (!field) {
    return <div style={{ color: "#c62828", fontSize: 12 }}>表单组件缺少 fieldName</div>;
  }

  switch (node.type) {
    case "text":
    case "number":
      return (
        <div style={baseStyle}>
          {node.label && <label style={labelStyle}>{node.label}{required && <span style={{ color: "#c62828" }}> *</span>}</label>}
          <input
            type={node.type === "number" ? "number" : "text"}
            value={String(value ?? "")}
            disabled={disabledBase}
            readOnly={submitted}
            style={inputStyle}
            onChange={(e) => setField(field, node.type === "number" ? Number(e.target.value) : e.target.value)}
          />
          {errors.map((e, i) => <div key={i} style={errorStyle}>{e}</div>)}
          {node.helpText && <div style={helpStyle}>{node.helpText}</div>}
        </div>
      );
    case "textarea":
      return (
        <div style={baseStyle}>
          {node.label && <label style={labelStyle}>{node.label}{required && <span style={{ color: "#c62828" }}> *</span>}</label>}
          <textarea
            value={String(value ?? "")}
            disabled={disabledBase}
            readOnly={submitted}
            rows={node.props?.rows ?? node.props?.Lines ?? 3}
            style={inputStyle}
            onChange={(e) => setField(field, e.target.value)}
          />
          {errors.map((e, i) => <div key={i} style={errorStyle}>{e}</div>)}
          {node.helpText && <div style={helpStyle}>{node.helpText}</div>}
        </div>
      );
    case "select":
      return (
        <div style={baseStyle}>
          {node.label && <label style={labelStyle}>{node.label}{required && <span style={{ color: "#c62828" }}> *</span>}</label>}
          <select value={String(value ?? "")} disabled={disabledBase || submitted} style={inputStyle} onChange={(e) => setField(field, e.target.value)}>
            <option value="">请选择</option>
            {itemsOf(node).map((it, i) => (
              <option key={i} value={it.value} disabled={it.disabled}>{it.label}</option>
            ))}
          </select>
          {errors.map((e, i) => <div key={i} style={errorStyle}>{e}</div>)}
          {node.helpText && <div style={helpStyle}>{node.helpText}</div>}
        </div>
      );
    case "date":
      return (
        <div style={baseStyle}>
          {node.label && <label style={labelStyle}>{node.label}{required && <span style={{ color: "#c62828" }}> *</span>}</label>}
          <input type="date" value={String(value ?? "")} disabled={disabledBase} readOnly={submitted} style={inputStyle} onChange={(e) => setField(field, e.target.value)} />
          {errors.map((e, i) => <div key={i} style={errorStyle}>{e}</div>)}
          {node.helpText && <div style={helpStyle}>{node.helpText}</div>}
        </div>
      );
    case "switch":
      return (
        <div style={baseStyle}>
          <label style={{ display: "flex", alignItems: "center", gap: 8, fontSize: 14, cursor: disabledBase || submitted ? "not-allowed" : "pointer" }}>
            <input
              type="checkbox"
              checked={value === true || value === "true" || value === 1}
              disabled={disabledBase || submitted}
              onChange={(e) => setField(field, e.target.checked)}
            />
            {node.label}{required && <span style={{ color: "#c62828" }}> *</span>}
          </label>
          {errors.map((e, i) => <div key={i} style={errorStyle}>{e}</div>)}
          {node.helpText && <div style={helpStyle}>{node.helpText}</div>}
        </div>
      );
    default:
      return null;
  }
}

// ── 按钮 ────────────────────────────────────────────────────────────
function ButtonView({ node, store, onEvent }: { node: DslNode; store: DslStore; onEvent: (ev: DslEvent) => Promise<void> }) {
  // 提交乐观锁：表单已提交（__cjdsl_submitted）后，所有按钮置灰防重复点击；失败时由 applyResult 清除该标志解锁
  const submitted = store.get("__cjdsl_submitted") === true;
  const disabled = evalDslExpr(node.disabledIf, store) === true || submitted;
  const variant = node.props?.variant ?? node.props?.Variant ?? "text";
  const color = node.props?.color ?? node.props?.Color ?? "default";
  const colorMap: Record<string, [string, string]> = {
    primary: ["#1976D2", "#fff"],
    secondary: ["#7B1FA2", "#fff"],
    success: ["#2E7D32", "#fff"],
    error: ["#C62828", "#fff"],
    default: ["rgba(0,0,0,0.08)", "rgba(0,0,0,0.87)"],
  };
  const [bg, fg] = colorMap[color] ?? colorMap.default;
  const style: React.CSSProperties =
    variant === "filled"
      ? { background: bg, color: fg, border: "none", borderRadius: 4, padding: "7px 16px", fontSize: 14, cursor: disabled ? "not-allowed" : "pointer", opacity: disabled ? 0.5 : 1, fontFamily: "inherit" }
      : variant === "outlined"
        ? { background: "transparent", color: bg, border: `1px solid ${bg}`, borderRadius: 4, padding: "6px 15px", fontSize: 14, cursor: disabled ? "not-allowed" : "pointer", opacity: disabled ? 0.5 : 1, fontFamily: "inherit" }
        : { background: "transparent", color: bg, border: "none", borderRadius: 4, padding: "6px 14px", fontSize: 14, cursor: disabled ? "not-allowed" : "pointer", opacity: disabled ? 0.5 : 1, fontFamily: "inherit" };

  const clickEv = (node.events ?? []).find((e) => e.type === "click" || e.type === "onClick");
  const href = node.props?.href;
  const common = { style, disabled, "data-cjdsl-id": node.id };

  if (href && isSafeLink(href)) {
    return (
      <a href={String(href)} {...common}>
        {node.type === "iconButton" ? (node.props?.icon ?? "⚡") : (node.label ?? "")}
      </a>
    );
  }
  if (clickEv) {
    return (
      <button {...common} onClick={() => void onEvent(clickEv)}>
        {node.type === "iconButton" ? (node.props?.icon ?? "⚡") : (node.label ?? "")}
      </button>
    );
  }
  return (
    <button {...common} onClick={() => void onEvent({ type: "click", handler: "showToast", params: { message: "按钮未配置事件", severity: "warning" } })}>
      {node.type === "iconButton" ? (node.props?.icon ?? "⚡") : (node.label ?? "")}
    </button>
  );
}

// ── 图表（Pie/Donut SVG 直出） ──────────────────────────────────────
function ChartView({ node }: { node: DslNode }) {
  const chartType = node.props?.ChartType ?? node.props?.chartType ?? "donut";
  if (chartType !== "pie" && chartType !== "donut") {
    return <div style={{ color: "#888", fontSize: 12, padding: 6 }}>chart v1 仅支持 Pie/Donut（当前 {String(chartType)}）</div>;
  }
  const raw: any[] = node.props?.PieData ?? node.props?.pieData ?? [];
  const data = raw.map((d) => ({
    value: Number(d?.value ?? d?.Value ?? 0),
    label: String(d?.label ?? d?.Label ?? ""),
  }));
  const labels: string[] = node.props?.Labels ?? node.props?.labels ?? [];
  data.forEach((d, i) => {
    if (!d.label && labels[i]) d.label = String(labels[i]);
  });
  const width = Number(node.props?.width ?? node.props?.Width ?? 300);
  const height = Number(node.props?.height ?? node.props?.Height ?? 300);
  const svg = buildDonutSvg(data, width, height, chartType === "donut");
  return <div style={{ display: "flex", justifyContent: "center", margin: "10px 0" }} dangerouslySetInnerHTML={{ __html: svg }} />;
}
