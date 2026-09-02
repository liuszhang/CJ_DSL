// src/cjdsl-page.ts
import { createRoot } from "react-dom/client";

// ../CJDSL.React/src/DslRenderer.tsx
import { useCallback, useEffect, useLayoutEffect, useMemo as useMemo2, useRef, useState as useState2 } from "react";

// ../CJDSL.React/src/expr.ts
function tokenize(input) {
  const tokens = [];
  let i = 0;
  const n = input.length;
  while (i < n) {
    const c = input[i];
    if (/\s/.test(c)) {
      i++;
      continue;
    }
    if (c === "(") {
      tokens.push({ t: "lparen" });
      i++;
      continue;
    }
    if (c === ")") {
      tokens.push({ t: "rparen" });
      i++;
      continue;
    }
    if (c === ",") {
      tokens.push({ t: "comma" });
      i++;
      continue;
    }
    if (c === '"' || c === "'") {
      const quote = c;
      let j = i + 1;
      let buf = "";
      let closed = false;
      while (j < n) {
        if (input[j] === "\\" && j + 1 < n) {
          buf += input[j + 1];
          j += 2;
          continue;
        }
        if (input[j] === quote) {
          closed = true;
          break;
        }
        buf += input[j];
        j++;
      }
      if (!closed) return null;
      tokens.push({ t: "str", v: buf });
      i = j + 1;
      continue;
    }
    if (/[0-9.]/.test(c) && /[0-9]/.test(c)) {
      let j = i;
      while (j < n && /[0-9.]/.test(input[j])) j++;
      const num = Number(input.slice(i, j));
      if (Number.isNaN(num)) return null;
      tokens.push({ t: "num", v: num });
      i = j;
      continue;
    }
    const two = input.slice(i, i + 2);
    if (["==", "!=", ">=", "<=", "&&", "||"].includes(two)) {
      tokens.push({ t: "op", v: two });
      i += 2;
      continue;
    }
    if (["!", ">", "<", "="].includes(c)) {
      tokens.push({ t: "op", v: c });
      i++;
      continue;
    }
    if (/[A-Za-z_]/.test(c)) {
      let j = i;
      while (j < n && /[A-Za-z0-9_.]/.test(input[j])) j++;
      const ident = input.slice(i, j);
      if (ident === "true" || ident === "false") {
        tokens.push({ t: "bool", v: ident === "true" });
      } else {
        tokens.push({ t: "ident", v: ident });
      }
      i = j;
      continue;
    }
    return null;
  }
  tokens.push({ t: "eof" });
  return tokens;
}
var Parser = class {
  constructor(tokens, store) {
    this.tokens = tokens;
    this.store = store;
    this.pos = 0;
  }
  peek() {
    return this.tokens[this.pos];
  }
  next() {
    return this.tokens[this.pos++];
  }
  expectOp(v) {
    const tok = this.peek();
    if (tok.t === "op" && tok.v === v) {
      this.pos++;
      return true;
    }
    return false;
  }
  parse() {
    const v = this.parseOr();
    return this.peek().t === "eof" ? v : void 0;
  }
  parseOr() {
    const left = this.parseAnd();
    if (left === void 0) return void 0;
    while (this.expectOp("||")) {
      const right = this.parseAnd();
      if (right === void 0) return void 0;
      if (left || right) return true;
    }
    return left;
  }
  parseAnd() {
    const left = this.parseCmp();
    if (left === void 0) return void 0;
    while (this.expectOp("&&")) {
      const right = this.parseCmp();
      if (right === void 0) return void 0;
      if (!left || !right) return false;
    }
    return left;
  }
  parseCmp() {
    const left = this.parsePrimary();
    if (left === void 0) return void 0;
    const tok = this.peek();
    if (tok.t === "op" && ["==", "!=", ">", "<", ">=", "<="].includes(tok.v)) {
      this.pos++;
      const right = this.parsePrimary();
      if (right === void 0) return void 0;
      const l = left[0];
      const r = right[0];
      switch (tok.v) {
        case "==":
          return String(l) === String(r);
        case "!=":
          return String(l) !== String(r);
        case ">":
          return l > r;
        case "<":
          return l < r;
        case ">=":
          return l >= r;
        case "<=":
          return l <= r;
      }
    }
    return left[1];
  }
  /** primary 返回 [值, 布尔语义]；支持 ! 前缀与 includes() 调用 */
  parsePrimary() {
    const tok = this.peek();
    if (tok.t === "op" && tok.v === "!") {
      this.pos++;
      const inner = this.parsePrimary();
      if (inner === void 0) return void 0;
      return [inner[0], inner[1] === void 0 ? void 0 : !inner[1]];
    }
    if (tok.t === "lparen") {
      this.pos++;
      const v = this.parseOr();
      if (v === void 0) return void 0;
      if (this.peek().t !== "rparen") return void 0;
      this.pos++;
      return [v, v];
    }
    if (tok.t === "num") {
      this.pos++;
      return [tok.v, !!tok.v];
    }
    if (tok.t === "str") {
      this.pos++;
      return [tok.v, tok.v !== ""];
    }
    if (tok.t === "bool") {
      this.pos++;
      return [tok.v, tok.v];
    }
    if (tok.t === "ident") {
      this.pos++;
      if (this.peek().t === "lparen") {
        this.pos++;
        const arg = this.parsePrimary();
        if (arg === void 0) return void 0;
        if (this.peek().t !== "rparen") return void 0;
        this.pos++;
        const haystack = this.lookupIdent(tok.v);
        const needle = String(arg[0]);
        if (Array.isArray(haystack)) return [true, haystack.some((x) => String(x) === needle)];
        if (typeof haystack === "string") return [true, haystack.includes(needle)];
        return [false, false];
      }
      return [this.lookupIdent(tok.v), void 0];
    }
    return void 0;
  }
  lookupIdent(ident) {
    const key = ident.startsWith("data.") ? ident.slice(5) : ident;
    return this.store.get(`data.${key}`);
  }
};
function evalDslExpr(expr, store) {
  if (!expr || expr.trim() === "") return void 0;
  const tokens = tokenize(expr);
  if (!tokens) return void 0;
  const parser = new Parser(tokens, store);
  try {
    return parser.parse();
  } catch {
    return void 0;
  }
}

// ../CJDSL.React/src/validate.ts
function defaultMessage(rule) {
  switch (rule.type) {
    case "required":
      return "\u8BE5\u5B57\u6BB5\u4E3A\u5FC5\u586B\u9879";
    case "minLength":
      return `\u957F\u5EA6\u4E0D\u80FD\u5C11\u4E8E ${rule.min} \u4E2A\u5B57\u7B26`;
    case "maxLength":
      return `\u957F\u5EA6\u4E0D\u80FD\u8D85\u8FC7 ${rule.max} \u4E2A\u5B57\u7B26`;
    case "regex":
      return "\u683C\u5F0F\u4E0D\u6B63\u786E";
    case "min":
      return `\u6570\u503C\u4E0D\u80FD\u5C0F\u4E8E ${rule.min}`;
    case "max":
      return `\u6570\u503C\u4E0D\u80FD\u5927\u4E8E ${rule.max}`;
    default:
      return "\u6821\u9A8C\u672A\u901A\u8FC7";
  }
}
function validateField(value, rules) {
  const errors = [];
  if (!rules || rules.length === 0) return { valid: true, errors };
  for (const rule of rules) {
    const msg = rule.message || defaultMessage(rule);
    switch (rule.type) {
      case "required": {
        const v = value;
        if (v === void 0 || v === null || String(v).trim() === "") errors.push(msg);
        break;
      }
      case "minLength": {
        const v = value;
        if (v !== void 0 && v !== null && String(v).length < (rule.min ?? 0)) errors.push(msg);
        break;
      }
      case "maxLength": {
        const v = value;
        if (v !== void 0 && v !== null && String(v).length > (rule.max ?? 0)) errors.push(msg);
        break;
      }
      case "regex": {
        const v = value;
        if (v !== void 0 && v !== null && String(v).trim() !== "") {
          try {
            const re = new RegExp(rule.pattern ?? "");
            if (!re.test(String(v))) errors.push(msg);
          } catch {
            errors.push("\u6B63\u5219\u8868\u8FBE\u5F0F\u65E0\u6548");
          }
        }
        break;
      }
      case "min": {
        const n = Number(value);
        if (!Number.isNaN(n) && n < (rule.min ?? 0)) errors.push(msg);
        break;
      }
      case "max": {
        const n = Number(value);
        if (!Number.isNaN(n) && n > (rule.max ?? 0)) errors.push(msg);
        break;
      }
    }
  }
  return { valid: errors.length === 0, errors };
}

// ../CJDSL.React/src/events.ts
var EventDispatcher = class {
  constructor() {
    this.debounceTimers = /* @__PURE__ */ new Map();
  }
  async dispatch(ev, ctx) {
    if (ev.debounceMs && ev.debounceMs > 0) {
      const key = ev.type + ":" + (ev.params?.action ?? "");
      const existing = this.debounceTimers.get(key);
      if (existing) clearTimeout(existing);
      await new Promise((resolve) => {
        this.debounceTimers.set(
          key,
          setTimeout(() => {
            this.debounceTimers.delete(key);
            resolve();
          }, ev.debounceMs ?? 0)
        );
      });
    }
    if (ev.confirm?.message) {
      const ok = window.confirm(ev.confirm.message);
      if (!ok) return false;
    }
    const handler = ev.handler;
    switch (handler) {
      case "submit":
        return await this.handleSubmit(ev, ctx);
      case "apiCall":
        return await this.handleApiCall(ev, ctx);
      case "setValue":
        return this.handleSetValue(ev, ctx);
      case "chain":
        return await this.handleChain(ev, ctx);
      case "showToast":
        return this.handleShowToast(ev, ctx);
      case "navigate":
        return this.handleNavigate(ev, ctx);
      default:
        return false;
    }
  }
  async handleSubmit(ev, ctx) {
    if (ctx.validateForm && !ctx.validateForm()) {
      ctx.callbacks.onToast?.("\u8868\u5355\u6821\u9A8C\u672A\u901A\u8FC7\uFF0C\u8BF7\u68C0\u67E5\u5FC5\u586B\u9879", "error");
      return false;
    }
    const params = ev.params ?? {};
    const action = String(params.action ?? "");
    if (!action) {
      ctx.callbacks.onToast?.("submit \u4E8B\u4EF6\u7F3A\u5C11 params.action", "error");
      return false;
    }
    if (!ctx.callbacks.onSubmit) return false;
    const result = await ctx.callbacks.onSubmit({ action, formId: ctx.formId, values: ctx.values });
    if (result.ok && ev.params?.onSuccess) {
      await this.dispatchChainItems(ev.params.onSuccess, ctx);
    }
    return result.ok;
  }
  async handleApiCall(ev, ctx) {
    const params = ev.params ?? {};
    if (!ctx.callbacks.onApiCall) return false;
    const result = await ctx.callbacks.onApiCall(params, ctx.values);
    if (result.ok && ev.params?.onSuccess) {
      await this.dispatchChainItems(ev.params.onSuccess, ctx);
    }
    return result.ok;
  }
  handleSetValue(ev, ctx) {
    const params = ev.params ?? {};
    const field = String(params.field ?? "");
    if (!field) return false;
    ctx.store.set(`data.${field}`, params.value);
    return true;
  }
  async handleChain(ev, ctx) {
    const chain = ev.params?.chain;
    if (!Array.isArray(chain)) return false;
    for (const item of chain) {
      const sub = {
        type: item?.type ?? "click",
        handler: String(item?.handler ?? ""),
        params: item?.params,
        confirm: item?.confirm,
        debounceMs: item?.debounceMs
      };
      const ok = await this.dispatch(sub, ctx);
      if (!ok) return false;
    }
    return true;
  }
  async dispatchChainItems(items, ctx) {
    if (!Array.isArray(items)) return;
    for (const item of items) {
      if (!item || typeof item !== "object") continue;
      const rec = item;
      const sub = {
        type: rec.type ?? "click",
        handler: String(rec.handler ?? ""),
        params: rec.params,
        confirm: rec.confirm,
        debounceMs: rec.debounceMs
      };
      await this.dispatch(sub, ctx);
    }
  }
  handleShowToast(ev, ctx) {
    const params = ev.params ?? {};
    ctx.callbacks.onToast?.(String(params.message ?? ""), String(params.severity ?? "info"));
    return true;
  }
  handleNavigate(ev, ctx) {
    const params = ev.params ?? {};
    const path = String(params.path ?? "");
    if (!path) return false;
    ctx.callbacks.onNavigate?.(path);
    return true;
  }
};

// ../CJDSL.React/src/svg.ts
var COLORS = [
  "#1976D2",
  "#388E3C",
  "#F57C00",
  "#D32F2F",
  "#7B1FA2",
  "#0097A7",
  "#C2185B",
  "#AFB42B",
  "#455A64",
  "#5D4037"
];
function buildDonutSvg(data, width = 300, height = 300, isDonut = true) {
  const total = data.reduce((s, d) => s + (Number(d.value) || 0), 0);
  console.info("[cjdsl-page][buildDonutSvg]", {
    dataLen: data.length,
    total,
    width,
    height,
    isDonut,
    firstItem: data[0]
  });
  if (total <= 0) {
    console.warn("[cjdsl-page][buildDonutSvg] total<=0\uFF0C\u8FD4\u56DE\u300C\u6240\u6709\u503C\u4E3A\u96F6\u300D\u5360\u4F4D\uFF08\u7070\u6846\u5143\u51F6\uFF1F\uFF09", {
      dataLen: data.length,
      firstItem: data[0],
      rawValues: data.slice(0, 5).map((d) => d.value)
    });
    return `<div style="color:#999;padding:20px;text-align:center;">\u6240\u6709\u503C\u4E3A\u96F6</div>`;
  }
  const cx = width / 2;
  const cy = height / 2;
  const outerR = Math.min(width, height) / 2 - 10;
  const innerR = isDonut ? Math.round(outerR * 0.65) : 0;
  let path = "";
  let start = -90;
  for (let i = 0; i < data.length; i++) {
    const v = Number(data[i].value) || 0;
    const sweep = v / total * 360;
    const end = start + sweep;
    const color = COLORS[i % COLORS.length];
    const label = data[i].label || `\u9879\u76EE${i + 1}`;
    const pct = Math.round(v / total * 1e3) / 10;
    path += `<path d="${arcPath(cx, cy, outerR, innerR, start, end)}" fill="${color}" stroke="white" stroke-width="1.5"><title>${esc(label)}: ${v} (${pct}%)</title></path>`;
    start = end;
  }
  let center = "";
  if (isDonut) {
    center = `<text x="${cx}" y="${cy - 6}" text-anchor="middle" font-size="13" fill="#888" font-family="sans-serif">\u603B\u8BA1</text><text x="${cx}" y="${cy + 18}" text-anchor="middle" font-size="24" font-weight="bold" fill="#333" font-family="sans-serif">${total}</text>`;
  }
  return `<svg width="${width}" height="${height}" viewBox="0 0 ${width} ${height}" xmlns="http://www.w3.org/2000/svg" role="img">${path}${center}</svg>`;
}
function arcPath(cx, cy, outerR, innerR, startDeg, endDeg) {
  const rad = (d) => d * Math.PI / 180;
  const x1o = cx + outerR * Math.cos(rad(startDeg));
  const y1o = cy + outerR * Math.sin(rad(startDeg));
  const x2o = cx + outerR * Math.cos(rad(endDeg));
  const y2o = cy + outerR * Math.sin(rad(endDeg));
  const largeArc = endDeg - startDeg > 180 ? 1 : 0;
  if (innerR === 0) {
    return `M${cx},${cy} L${x1o.toFixed(1)},${y1o.toFixed(1)} A${outerR},${outerR} 0 ${largeArc},1 ${x2o.toFixed(1)},${y2o.toFixed(1)} Z`;
  }
  const x1i = cx + innerR * Math.cos(rad(startDeg));
  const y1i = cy + innerR * Math.sin(rad(startDeg));
  const x2i = cx + innerR * Math.cos(rad(endDeg));
  const y2i = cy + innerR * Math.sin(rad(endDeg));
  return `M${x1o.toFixed(1)},${y1o.toFixed(1)} A${outerR},${outerR} 0 ${largeArc},1 ${x2o.toFixed(1)},${y2o.toFixed(1)} L${x2i.toFixed(1)},${y2i.toFixed(1)} A${innerR},${innerR} 0 ${largeArc},0 ${x1i.toFixed(1)},${y1i.toFixed(1)} Z`;
}
function esc(s) {
  return String(s ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
}
if (typeof console !== "undefined" && !globalThis.__cjdsl_svg_loaded__) {
  globalThis.__cjdsl_svg_loaded__ = true;
  console.info("[cjdsl-page] buildDonutSvg v2 loaded (no raw-path-text bug)");
}

// ../CJDSL.React/src/flow.tsx
import { useMemo, useState } from "react";
import { jsx, jsxs } from "react/jsx-runtime";
var FLOW_STYLE_KEYS = /* @__PURE__ */ new Set(["class", "color", "backgroundColor", "margin", "padding", "width", "height"]);
function pickFlowStyle(style) {
  if (!style || typeof style !== "object") return void 0;
  const out = {};
  for (const [k, v] of Object.entries(style)) {
    if (FLOW_STYLE_KEYS.has(k) && (typeof v === "string" || typeof v === "number")) {
      out[k] = v;
    }
  }
  return Object.keys(out).length > 0 ? out : void 0;
}
function truncateNote(note, max = 30) {
  if (!note) return "";
  return note.length <= max ? note : `${note.slice(0, max)}\u2026`;
}
function fmtPercent(value) {
  if (value === void 0 || value === null || Number.isNaN(value)) return "\u2014";
  return `${Math.round(value * 100)}%`;
}
function strengthColor(value) {
  if (value === void 0 || value === null || Number.isNaN(value)) return "#9e9e9e";
  return value >= 0.7 ? "#4caf50" : value >= 0.4 ? "#ff9800" : "#f44336";
}
function toArray(raw) {
  if (Array.isArray(raw)) return raw;
  return [];
}
function FlowView({ node, store, onEvent }) {
  const props = node.props ?? {};
  const [selectedId, setSelectedId] = useState(null);
  const nodes = useMemo(() => {
    const direct = toArray(props.nodes);
    if (direct.length > 0) return direct;
    const bound = store.get(node.dataBind ?? "datasource.items");
    return toArray(bound);
  }, [props.nodes, node.dataBind, store]);
  const edges = useMemo(() => toArray(props.edges), [props.edges]);
  const eliminated = useMemo(() => toArray(props.eliminated), [props.eliminated]);
  const highlightOnClick = props.highlightOnClick === true;
  const vertical = props.layout === "vertical";
  const interactive = highlightOnClick || (node.events ?? []).some((e) => e.type === "click" || e.type === "onClick");
  if (nodes.length === 0) {
    return /* @__PURE__ */ jsx("div", { style: { color: "#999", fontSize: 12, padding: 6 }, children: "\uFF08\u65E0\u6EAF\u6E90\u8DEF\u5F84\u6570\u636E\uFF09" });
  }
  const relationOf = (source, target) => {
    const edge = edges.find((e) => e.source === source && e.target === target);
    return edge?.relation ?? "";
  };
  const handleNodeClick = (fn) => {
    if (highlightOnClick) {
      setSelectedId((prev) => prev === fn.id ? null : fn.id);
    }
    const clickEv = (node.events ?? []).find((e) => e.type === "click" || e.type === "onClick");
    const relation = edges.find((e) => e.source === fn.id)?.relation ?? "";
    const baseParams = {
      nodeId: fn.id,
      hop: fn.hop,
      instanceId: fn.instanceId ?? "",
      relation
    };
    if (clickEv) {
      void onEvent({
        ...clickEv,
        params: { ...clickEv.params ?? {}, ...baseParams }
      });
    } else {
      void onEvent({
        type: "click",
        handler: "showToast",
        params: { message: truncateNote(fn.note) || fn.node, severity: "info" }
      });
    }
  };
  const nodeCardStyle = (fn) => {
    const color = strengthColor(fn.evidenceStrength);
    const selected = highlightOnClick && selectedId === fn.id;
    return {
      border: selected ? `3px solid ${color}` : `1px solid ${color}`,
      borderRadius: 8,
      cursor: interactive ? "pointer" : "default",
      background: "#fff",
      boxShadow: selected ? "0 4px 12px rgba(0,0,0,0.2)" : "0 1px 3px rgba(0,0,0,0.12)",
      minWidth: 170,
      maxWidth: 230,
      padding: "8px 12px",
      boxSizing: "border-box"
    };
  };
  const chainStyle = vertical ? { display: "flex", flexDirection: "column", alignItems: "stretch", gap: 12 } : { display: "flex", flexDirection: "row", alignItems: "stretch", gap: 4, overflowX: "auto", padding: 4 };
  return /* @__PURE__ */ jsxs("div", { className: "cjdsl-flow", style: pickFlowStyle(node.style), "data-cjdsl-id": node.id, children: [
    node.label && /* @__PURE__ */ jsx("div", { style: { fontSize: 15, fontWeight: 600, margin: "4px 0 8px" }, children: node.label }),
    /* @__PURE__ */ jsx("div", { style: chainStyle, children: nodes.map((fn, i) => {
      const next = i < nodes.length - 1 ? nodes[i + 1] : void 0;
      const relation = next ? relationOf(fn.id, next.id) : "";
      return /* @__PURE__ */ jsxs("div", { style: { display: "flex", flexDirection: vertical ? "column" : "row", alignItems: "center", gap: 8, flex: "0 0 auto" }, children: [
        /* @__PURE__ */ jsxs("div", { style: nodeCardStyle(fn), onClick: () => handleNodeClick(fn), children: [
          /* @__PURE__ */ jsxs("div", { style: { display: "flex", alignItems: "center", justifyContent: "space-between", gap: 6 }, children: [
            /* @__PURE__ */ jsx("span", { style: { fontWeight: 600, fontSize: 13, wordBreak: "break-all" }, children: fn.node }),
            fn.type && /* @__PURE__ */ jsx("span", { style: { fontSize: 10, color: "#1565c0", border: "1px solid #90caf9", borderRadius: 10, padding: "0 6px", lineHeight: "16px", whiteSpace: "nowrap" }, children: fn.type })
          ] }),
          truncateNote(fn.note) && /* @__PURE__ */ jsx("div", { style: { fontSize: 12, color: "#666", marginTop: 4, wordBreak: "break-word" }, children: truncateNote(fn.note) }),
          /* @__PURE__ */ jsxs("div", { style: { display: "flex", justifyContent: "space-between", marginTop: 6, fontSize: 11 }, children: [
            /* @__PURE__ */ jsxs("span", { style: { color: strengthColor(fn.evidenceStrength), fontWeight: 600 }, children: [
              "\u8BC1\u636E ",
              fmtPercent(fn.evidenceStrength)
            ] }),
            /* @__PURE__ */ jsxs("span", { style: { color: "#777" }, children: [
              "\u7F6E\u4FE1 ",
              fmtPercent(fn.pathConfidence)
            ] })
          ] })
        ] }),
        next && /* @__PURE__ */ jsxs("div", { style: { display: "flex", flexDirection: "column", alignItems: "center", color: "#999", fontSize: 12, whiteSpace: "nowrap" }, children: [
          /* @__PURE__ */ jsx("span", { style: { fontSize: 16, lineHeight: 1 }, children: "\u2192" }),
          relation && /* @__PURE__ */ jsx("span", { style: { color: "#777", fontSize: 11 }, children: relation })
        ] })
      ] }, fn.id || `hop-${i}`);
    }) }),
    eliminated.length > 0 && /* @__PURE__ */ jsxs("div", { style: { marginTop: 12 }, children: [
      /* @__PURE__ */ jsx("div", { style: { fontSize: 13, fontWeight: 600, color: "#888", marginBottom: 4 }, children: "\u5DF2\u6392\u9664\u5019\u9009" }),
      eliminated.map((item, i) => {
        const linked = nodes.find((n) => String(n.node).toLowerCase() === String(item.candidate).toLowerCase());
        return /* @__PURE__ */ jsxs(
          "div",
          {
            style: { border: "1px dashed #bbb", background: "#fafafa", borderRadius: 8, padding: "6px 10px", marginBottom: 6 },
            children: [
              /* @__PURE__ */ jsxs("div", { style: { display: "flex", alignItems: "center", gap: 6, flexWrap: "wrap" }, children: [
                /* @__PURE__ */ jsx("span", { style: { color: "#999", fontWeight: 600, fontSize: 13 }, children: item.candidate }),
                item.candidateType && /* @__PURE__ */ jsx("span", { style: { fontSize: 10, color: "#888", border: "1px solid #ccc", borderRadius: 10, padding: "0 6px", lineHeight: "16px", whiteSpace: "nowrap" }, children: item.candidateType }),
                item.strength !== void 0 && /* @__PURE__ */ jsxs("span", { style: { fontSize: 11, color: "#999" }, children: [
                  "\u5F3A\u5EA6 ",
                  fmtPercent(item.strength)
                ] })
              ] }),
              item.reason && /* @__PURE__ */ jsx("div", { style: { fontSize: 12, color: "#aaa", marginTop: 2 }, children: item.reason }),
              linked && /* @__PURE__ */ jsxs("div", { style: { fontSize: 11, color: "#999", marginTop: 2 }, children: [
                "\u865A\u7EBF\u5173\u8054\uFF1A",
                linked.node,
                "\uFF08hop-",
                linked.hop,
                "\uFF09"
              ] })
            ]
          },
          `${item.candidate}-${i}`
        );
      })
    ] })
  ] });
}

// ../CJDSL.React/src/dsl.ts
var V1_COMPONENT_TYPES = /* @__PURE__ */ new Set([
  // 布局
  "card",
  "grid",
  "stack",
  "divider",
  "form",
  // 展示
  "textDisplay",
  "table",
  "alert",
  "chip",
  "badge",
  // 内联 DSL 引用（决策 7：渲染时 parseDslText → validateDsl → 递归子界面）
  "dslRef",
  // 表单
  "text",
  "number",
  "select",
  "textarea",
  "date",
  "switch",
  // 交互
  "button",
  "iconButton",
  // 图表
  "chart",
  // 溯源路径 flow
  "flow"
]);
var V1_EVENT_HANDLERS = /* @__PURE__ */ new Set([
  "submit",
  "apiCall",
  "setValue",
  "chain",
  "showToast",
  "navigate"
]);
var V1_VALIDATION_RULES = /* @__PURE__ */ new Set([
  "required",
  "minLength",
  "maxLength",
  "regex",
  "min",
  "max"
]);
var V1_DATA_SOURCE_TYPES = /* @__PURE__ */ new Set(["static", "api"]);
var FORBIDDEN_PROPS = /* @__PURE__ */ new Set([
  "dangerouslySetInnerHTML",
  "innerHTML",
  "outerHTML",
  "srcdoc",
  "javascript"
]);
var FORBIDDEN_EXPR_PATTERN = /(document|window|globalThis|process|require|import|fetch|eval|Function|constructor|prototype|__proto__|localStorage|sessionStorage)\b/i;
function isPlainObject(v) {
  return typeof v === "object" && v !== null && !Array.isArray(v);
}
function validateComponent(node, path, errors, out) {
  if (!isPlainObject(node)) {
    errors.push(`${path}: \u7EC4\u4EF6\u5FC5\u987B\u662F\u5BF9\u8C61`);
    return null;
  }
  const type = String(node.type ?? "");
  if (!V1_COMPONENT_TYPES.has(type)) {
    errors.push(`${path}: \u7EC4\u4EF6\u7C7B\u578B "${type || "(\u7A7A)"}" \u4E0D\u5728 v1 \u767D\u540D\u5355\uFF08\u5141\u8BB8: ${[...V1_COMPONENT_TYPES].join(", ")}\uFF09`);
    return null;
  }
  if (node.props !== void 0) {
    if (!isPlainObject(node.props)) {
      errors.push(`${path}: props \u5FC5\u987B\u662F\u5BF9\u8C61`);
      return null;
    }
    for (const key of Object.keys(node.props)) {
      if (FORBIDDEN_PROPS.has(key)) {
        errors.push(`${path}: props \u542B\u5371\u9669\u5C5E\u6027 "${key}"`);
        return null;
      }
      const v = node.props[key];
      if (typeof v === "string" && /^\s*javascript:/i.test(v)) {
        errors.push(`${path}: props.${key} \u542B javascript: URL`);
        return null;
      }
    }
  }
  for (const exprField of ["visibleIf", "disabledIf"]) {
    const expr = node[exprField];
    if (typeof expr === "string" && expr.trim() !== "" && FORBIDDEN_EXPR_PATTERN.test(expr)) {
      errors.push(`${path}: ${exprField} \u8868\u8FBE\u5F0F\u542B\u975E\u6CD5\u5F15\u7528`);
      return null;
    }
  }
  if (node.events !== void 0) {
    if (!Array.isArray(node.events)) {
      errors.push(`${path}: events \u5FC5\u987B\u662F\u6570\u7EC4`);
      return null;
    }
    for (let i = 0; i < node.events.length; i++) {
      const ev = node.events[i];
      if (!isPlainObject(ev)) {
        errors.push(`${path}.events[${i}]: \u4E8B\u4EF6\u5FC5\u987B\u662F\u5BF9\u8C61`);
        return null;
      }
      const handler = String(ev.handler ?? "");
      if (!V1_EVENT_HANDLERS.has(handler)) {
        errors.push(`${path}.events[${i}]: handler "${handler}" \u4E0D\u5728 v1 \u767D\u540D\u5355\uFF08\u5141\u8BB8: ${[...V1_EVENT_HANDLERS].join(", ")}\uFF09`);
        return null;
      }
    }
  }
  if (node.validationRules !== void 0) {
    if (!Array.isArray(node.validationRules)) {
      errors.push(`${path}: validationRules \u5FC5\u987B\u662F\u6570\u7EC4`);
      return null;
    }
    for (let i = 0; i < node.validationRules.length; i++) {
      const rule = node.validationRules[i];
      if (!isPlainObject(rule)) {
        errors.push(`${path}.validationRules[${i}]: \u6821\u9A8C\u89C4\u5219\u5FC5\u987B\u662F\u5BF9\u8C61`);
        return null;
      }
      const rt = String(rule.type ?? "");
      if (!V1_VALIDATION_RULES.has(rt)) {
        errors.push(`${path}.validationRules[${i}]: \u89C4\u5219\u7C7B\u578B "${rt}" \u4E0D\u5728 v1 \u767D\u540D\u5355\uFF08\u5141\u8BB8: ${[...V1_VALIDATION_RULES].join(", ")}\uFF09`);
        return null;
      }
    }
  }
  if (node.dataSource !== void 0) {
    if (!isPlainObject(node.dataSource)) {
      errors.push(`${path}: dataSource \u5FC5\u987B\u662F\u5BF9\u8C61`);
      return null;
    }
    const st = String(node.dataSource.type ?? "");
    if (!V1_DATA_SOURCE_TYPES.has(st)) {
      errors.push(`${path}: dataSource.type "${st}" \u4E0D\u5728 v1 \u767D\u540D\u5355\uFF08\u5141\u8BB8: static/api\uFF09`);
      return null;
    }
    if (st === "api") {
      const endpoint = String(node.dataSource.endpoint ?? "");
      if (!/^https?:\/\//i.test(endpoint)) {
        errors.push(`${path}: dataSource.endpoint \u5FC5\u987B\u662F http(s):// URL`);
        return null;
      }
    }
  }
  if (type === "chart") {
    const chartType = String(node.props?.ChartType ?? node.props?.chartType ?? "");
    if (!["pie", "donut"].includes(chartType.toLowerCase())) {
      errors.push(`${path}: chart \u4EC5\u652F\u6301 ChartType=Pie/Donut\uFF08v1 SVG \u76F4\u51FA\uFF09`);
      return null;
    }
  }
  const cleaned = { ...node };
  if (node.children !== void 0) {
    if (!Array.isArray(node.children)) {
      errors.push(`${path}: children \u5FC5\u987B\u662F\u6570\u7EC4`);
      return null;
    }
    const childrenOut = [];
    for (let i = 0; i < node.children.length; i++) {
      const child = validateComponent(node.children[i], `${path}.children[${i}]`, errors, null);
      if (child) childrenOut.push(child);
    }
    cleaned.children = childrenOut;
  }
  return cleaned;
}
function validateDsl(input) {
  const errors = [];
  if (input == null) {
    return { ok: false, errors: ["DSL \u4E3A\u7A7A"] };
  }
  if (Array.isArray(input)) {
    const cleaned = [];
    for (let i = 0; i < input.length; i++) {
      const c = validateComponent(input[i], `components[${i}]`, errors, null);
      if (c) cleaned.push(c);
    }
    if (errors.length > 0) return { ok: false, errors };
    return { ok: true, errors, pages: cleaned };
  }
  if (isPlainObject(input)) {
    if (Array.isArray(input.components)) {
      const pages = [];
      for (let i = 0; i < input.components.length; i++) {
        const c2 = validateComponent(input.components[i], `page.components[${i}]`, errors, null);
        if (c2) pages.push(c2);
      }
      if (errors.length > 0) return { ok: false, errors };
      return { ok: true, errors, pages };
    }
    const c = validateComponent(input, "dsl", errors, null);
    if (errors.length > 0 || !c) return { ok: false, errors };
    return { ok: true, errors, cleaned: c };
  }
  return { ok: false, errors: ["DSL \u5FC5\u987B\u662F\u5BF9\u8C61\u6216\u6570\u7EC4"] };
}
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

// ../CJDSL.React/src/DslRenderer.tsx
import { jsx as jsx2, jsxs as jsxs2 } from "react/jsx-runtime";
var STYLE_KEYS = /* @__PURE__ */ new Set(["class", "color", "backgroundColor", "margin", "padding", "width", "height"]);
function pickStyle(style) {
  if (!style || typeof style !== "object") return void 0;
  const out = {};
  for (const [k, v] of Object.entries(style)) {
    if (STYLE_KEYS.has(k) && (typeof v === "string" || typeof v === "number")) {
      out[k] = v;
    }
  }
  return Object.keys(out).length > 0 ? out : void 0;
}
function itemsOf(node) {
  const items = node.props?.items ?? node.props?.Items ?? [];
  return items.map((it) => {
    if (typeof it === "string") return { value: it, label: it };
    const rec = it ?? {};
    const value = rec.value ?? rec.Value ?? "";
    const label = rec.label ?? rec.Label ?? String(value);
    const disabled = !!rec.disabled || !!rec.Disabled;
    return { value: String(value), label: String(label), disabled };
  });
}
function escAttr(s) {
  return String(s ?? "").replace(/[&<>"']/g, (c) => ({ "&": "&amp;", "<": "&lt;", ">": "&gt;", '"': "&quot;", "'": "&#39;" })[c]);
}
function isSafeLink(href) {
  const h = String(href ?? "");
  return h.startsWith("/") || h.startsWith("#") || h.startsWith("http://") || h.startsWith("https://") || h.startsWith("mailto:");
}
function DslRenderer(props) {
  const { root, store, callbacks } = props;
  const [, setVersion] = useState2(0);
  const storeRef = useRef(store);
  storeRef.current = store;
  useEffect(() => {
    const unsub = store.subscribe(() => setVersion((v) => v + 1));
    return unsub;
  }, [store]);
  useLayoutEffect(() => {
    const walk = (n) => {
      const field = n.fieldName;
      if (field) {
        const v = n.props?.value ?? n.props?.Value;
        if (v !== void 0 && v !== null) store.set(`data.${field}`, v);
      }
      if (n.children) n.children.forEach(walk);
    };
    walk(root);
  }, [root, store]);
  const values = useMemo2(() => {
    const out = {};
    const walk = (n) => {
      if (n.fieldName) out[n.fieldName] = store.get(`data.${n.fieldName}`);
      if (n.children) n.children.forEach(walk);
    };
    walk(root);
    return out;
  }, [root, store]);
  const dispatcher = useMemo2(() => new EventDispatcher(), []);
  const [validationErrors, setValidationErrors] = useState2({});
  const validateForm = useCallback(() => {
    const errs = {};
    const walk = (n) => {
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
    async (ev) => {
      const formId = typeof root.id === "string" ? root.id : void 0;
      const ctxValues = {};
      const walk = (n) => {
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
          onSubmit: async (submitCtx) => {
            if (callbacks.onSubmit) return await callbacks.onSubmit(submitCtx);
            return { ok: false, message: "\u672A\u914D\u7F6E onSubmit \u56DE\u8C03" };
          },
          onApiCall: async (params, formValues) => {
            if (callbacks.onApiCall) return await callbacks.onApiCall(params, formValues);
            return { ok: false, message: "\u672A\u914D\u7F6E onApiCall \u56DE\u8C03" };
          }
        }
      });
    },
    [root, dispatcher, validateForm, callbacks]
  );
  const setField = useCallback(
    (fieldName, value) => {
      store.set(`data.${fieldName}`, value);
      setValidationErrors((prev) => ({ ...prev, [fieldName]: [] }));
    },
    [store]
  );
  return /* @__PURE__ */ jsx2("div", { className: "cjdsl-root", style: pickStyle(root.style), "data-cjdsl-type": root.type, children: root.children && root.children.length > 0 ? root.children.map((child, i) => /* @__PURE__ */ jsx2(
    DslNodeView,
    {
      node: child,
      store,
      values,
      validationErrors,
      setField,
      onEvent: handleEvent
    },
    child.id || `n${i}`
  )) : /* @__PURE__ */ jsx2(DslNodeView, { node: root, store, values, validationErrors, setField, onEvent: handleEvent }) });
}
function DslNodeView({ node, store, values, validationErrors, setField, onEvent }) {
  const visible = evalDslExpr(node.visibleIf, store);
  if (visible === false) return null;
  switch (node.type) {
    case "card":
    case "grid":
    case "stack":
    case "divider":
    case "form":
      return /* @__PURE__ */ jsx2(ContainerView, { node, store, values, validationErrors, setField, onEvent });
    case "textDisplay":
      return /* @__PURE__ */ jsx2(TextDisplayView, { node, store });
    case "table":
      return /* @__PURE__ */ jsx2(TableView, { node, store });
    case "alert":
      return /* @__PURE__ */ jsx2(AlertView, { node });
    case "chip":
      return /* @__PURE__ */ jsx2(ChipView, { node });
    case "badge":
      return /* @__PURE__ */ jsx2(BadgeView, { node });
    case "text":
    case "number":
    case "select":
    case "textarea":
    case "date":
    case "switch":
      return /* @__PURE__ */ jsx2(
        FieldView,
        {
          node,
          store,
          values,
          validationErrors,
          setField,
          onEvent
        }
      );
    case "button":
    case "iconButton":
      return /* @__PURE__ */ jsx2(ButtonView, { node, store, onEvent });
    case "chart":
      return /* @__PURE__ */ jsx2(ChartView, { node });
    case "flow":
      return /* @__PURE__ */ jsx2(FlowView, { node, store, onEvent });
    case "dslRef": {
      const raw = String(node.props?.dsl ?? "");
      const parsed = parseDslText(raw);
      if (!parsed.ok || parsed.dsl == null) {
        return /* @__PURE__ */ jsxs2("div", { style: { color: "#c62828", fontSize: 12, padding: "6px 10px", border: "1px dashed #ef9a9a", borderRadius: 4 }, children: [
          "dslRef \u89E3\u6790\u5931\u8D25\uFF1A",
          escAttr((parsed.errors ?? []).join("\uFF1B"))
        ] });
      }
      const verr = validateDsl(parsed.dsl);
      if (!verr.ok) {
        return /* @__PURE__ */ jsxs2("div", { style: { color: "#c62828", fontSize: 12, padding: "6px 10px", border: "1px dashed #ef9a9a", borderRadius: 4 }, children: [
          "dslRef \u6821\u9A8C\u672A\u901A\u8FC7\uFF1A",
          escAttr((verr.errors ?? []).join("\uFF1B"))
        ] });
      }
      return /* @__PURE__ */ jsx2(
        DslNodeView,
        {
          node: parsed.dsl,
          store,
          values,
          validationErrors,
          setField,
          onEvent
        }
      );
    }
    default:
      return /* @__PURE__ */ jsxs2("div", { style: { color: "#c62828", fontSize: 12, padding: "6px 10px", border: "1px dashed #ef9a9a", borderRadius: 4 }, children: [
        "\u672A\u652F\u6301\u7684\u7EC4\u4EF6\u7C7B\u578B\uFF1A",
        escAttr(node.type),
        "\uFF08DSL v1 \u767D\u540D\u5355\u5916\uFF09"
      ] });
  }
}
function ContainerView(props) {
  const { node, store, values, validationErrors, setField, onEvent } = props;
  if (node.type === "divider") {
    return /* @__PURE__ */ jsx2("hr", { style: { border: "none", borderTop: "1px solid rgba(0,0,0,0.12)", margin: "8px 0" } });
  }
  const children = node.children ?? [];
  const inner = children.map((child, i) => /* @__PURE__ */ jsx2(DslNodeView, { node: child, store, values, validationErrors, setField, onEvent }, child.id || `c${i}`));
  const isForm = node.type === "form";
  return /* @__PURE__ */ jsxs2(
    "div",
    {
      className: `cjdsl-${node.type}`,
      "data-cjdsl-id": node.id,
      style: {
        display: node.type === "stack" ? "flex" : void 0,
        flexDirection: node.type === "stack" ? node.props?.direction === "row" ? "row" : "column" : void 0,
        gap: node.type === "stack" ? 8 : void 0,
        border: isForm ? "1px solid rgba(0,0,0,0.1)" : void 0,
        borderRadius: isForm ? 8 : void 0,
        padding: isForm ? 12 : void 0,
        margin: isForm ? "8px 0" : void 0,
        ...node.style ? pickStyle(node.style) : {}
      },
      children: [
        node.type === "grid" ? children.map((child, i) => /* @__PURE__ */ jsx2("div", { style: { width: `${Math.min(Math.max(child.span ?? 12, 1), 12) * (100 / 12)}%`, display: "inline-block", verticalAlign: "top", padding: "0 4px", boxSizing: "border-box" }, children: /* @__PURE__ */ jsx2(DslNodeView, { node: child, store, values, validationErrors, setField, onEvent }) }, child.id || `g${i}`)) : inner,
        isForm && node.props?.showFooter !== false && /* @__PURE__ */ jsx2("div", { style: { marginTop: 10, textAlign: "right" }, children: node.props?.footerButtons?.map?.((btn, i) => /* @__PURE__ */ jsx2(DslNodeView, { node: { ...btn, type: "button" }, store, values, validationErrors, setField, onEvent }, btn.id || `fb${i}`)) })
      ]
    }
  );
}
function TextDisplayView({ node, store }) {
  let text = node.props?.text ?? node.props?.Text ?? node.props?.content ?? node.props?.Content;
  if (text === void 0 && node.dataBind) text = store.get(node.dataBind);
  if (text === void 0) text = node.label ?? "";
  const typo = node.props?.typo ?? node.props?.Typo ?? "body1";
  const size = typo === "h1" ? 28 : typo === "h2" ? 24 : typo === "h3" ? 20 : typo === "h4" ? 17 : typo === "h5" ? 15 : typo === "h6" ? 13 : 14;
  const color = node.props?.color ?? node.props?.Color;
  return /* @__PURE__ */ jsx2("div", { style: { fontSize: size, color: typeof color === "string" ? color : void 0, margin: "4px 0", whiteSpace: "pre-wrap", wordBreak: "break-word" }, children: String(text ?? "") });
}
function TableView({ node, store }) {
  const columns = node.props?.columns ?? node.props?.Columns ?? [];
  const data = node.props?.items ?? node.props?.Items ?? node.props?.rows ?? node.props?.Rows ?? [];
  const finalData = data.length > 0 ? data : store.get(node.dataBind ?? "datasource.items") ?? [];
  if (finalData.length === 0) return /* @__PURE__ */ jsx2("div", { style: { color: "#999", padding: 6 }, children: "\uFF08\u65E0\u6570\u636E\uFF09" });
  const effectiveCols = columns.length > 0 ? columns : Object.keys(finalData[0] ?? {}).map((k) => ({ name: k, label: k }));
  const getValue = (row, col) => {
    const key = col.value ?? col.name ?? "";
    return row?.[key] ?? "";
  };
  return /* @__PURE__ */ jsxs2("table", { style: { width: "100%", borderCollapse: "collapse", fontSize: 13 }, children: [
    /* @__PURE__ */ jsx2("thead", { children: /* @__PURE__ */ jsx2("tr", { children: effectiveCols.map((c, i) => /* @__PURE__ */ jsx2("th", { style: { borderBottom: "1px solid rgba(0,0,0,0.12)", padding: "6px 8px", textAlign: "left", fontWeight: 600 }, children: String(c.label ?? c.name ?? "") }, i)) }) }),
    /* @__PURE__ */ jsx2("tbody", { children: finalData.map((row, ri) => /* @__PURE__ */ jsx2("tr", { children: effectiveCols.map((c, ci) => /* @__PURE__ */ jsx2("td", { style: { borderBottom: "1px solid rgba(0,0,0,0.06)", padding: "6px 8px" }, children: String(getValue(row, c) ?? "") }, ci)) }, ri)) })
  ] });
}
function AlertView({ node }) {
  const severity = node.props?.severity ?? node.props?.Severity ?? "info";
  const colorMap = { info: "#0277BD", success: "#2E7D32", warning: "#F57C00", error: "#C62828" };
  const bgMap = { info: "#E1F5FE", success: "#E8F5E9", warning: "#FFF3E0", error: "#FFEBEE" };
  return /* @__PURE__ */ jsx2("div", { style: { background: bgMap[severity] ?? bgMap.info, color: colorMap[severity] ?? colorMap.info, borderRadius: 6, padding: "8px 12px", fontSize: 13, margin: "6px 0" }, children: String(node.props?.text ?? node.props?.message ?? node.props?.content ?? node.label ?? "") });
}
function ChipView({ node }) {
  return /* @__PURE__ */ jsx2("span", { style: { display: "inline-block", background: "rgba(0,0,0,0.08)", borderRadius: 12, padding: "2px 10px", fontSize: 12, margin: "2px 4px 2px 0" }, children: String(node.props?.text ?? node.props?.label ?? node.label ?? "") });
}
function BadgeView({ node }) {
  const color = node.props?.color ?? node.props?.Color ?? "#1976D2";
  return /* @__PURE__ */ jsx2("span", { style: { display: "inline-block", background: String(color), color: "#fff", borderRadius: 10, padding: "1px 8px", fontSize: 11, margin: "0 4px 0 0" }, children: String(node.props?.text ?? node.props?.content ?? node.label ?? "") });
}
function FieldView({ node, store, values, validationErrors, setField }) {
  const field = node.fieldName;
  const value = field ? store.get(`data.${field}`) ?? "" : "";
  const required = node.props?.Required === true || node.props?.required === true;
  const disabledBase = evalDslExpr(node.disabledIf, store) === true;
  const submitted = store.get("__cjdsl_submitted") === true;
  const errors = field ? validationErrors[field] ?? [] : [];
  const baseStyle = {
    display: "flex",
    flexDirection: "column",
    gap: 4,
    margin: "6px 0"
  };
  const labelStyle = { fontSize: 13, color: "rgba(0,0,0,0.66)", fontWeight: 500 };
  const lockBg = disabledBase || submitted ? "#f5f5f5" : "#fff";
  const lockColor = disabledBase || submitted ? "#9e9e9e" : "inherit";
  const inputStyle = {
    border: errors.length > 0 ? "1px solid #C62828" : "1px solid rgba(0,0,0,0.22)",
    borderRadius: 4,
    padding: "6px 10px",
    fontSize: 14,
    outline: "none",
    background: lockBg,
    color: lockColor,
    fontFamily: "inherit"
  };
  const helpStyle = { fontSize: 12, color: "#888" };
  const errorStyle = { fontSize: 12, color: "#C62828" };
  if (!field) {
    return /* @__PURE__ */ jsx2("div", { style: { color: "#c62828", fontSize: 12 }, children: "\u8868\u5355\u7EC4\u4EF6\u7F3A\u5C11 fieldName" });
  }
  switch (node.type) {
    case "text":
    case "number":
      return /* @__PURE__ */ jsxs2("div", { style: baseStyle, children: [
        node.label && /* @__PURE__ */ jsxs2("label", { style: labelStyle, "data-kb-field": node.fieldName, children: [
          node.label,
          required && /* @__PURE__ */ jsx2("span", { style: { color: "#c62828" }, children: " *" })
        ] }),
        /* @__PURE__ */ jsx2(
          "input",
          {
            type: node.type === "number" ? "number" : "text",
            value: String(value ?? ""),
            disabled: disabledBase,
            readOnly: submitted,
            style: inputStyle,
            onChange: (e) => setField(field, node.type === "number" ? Number(e.target.value) : e.target.value)
          }
        ),
        errors.map((e, i) => /* @__PURE__ */ jsx2("div", { style: errorStyle, children: e }, i)),
        node.helpText && /* @__PURE__ */ jsx2("div", { style: helpStyle, children: node.helpText })
      ] });
    case "textarea":
      return /* @__PURE__ */ jsxs2("div", { style: baseStyle, children: [
        node.label && /* @__PURE__ */ jsxs2("label", { style: labelStyle, "data-kb-field": node.fieldName, children: [
          node.label,
          required && /* @__PURE__ */ jsx2("span", { style: { color: "#c62828" }, children: " *" })
        ] }),
        /* @__PURE__ */ jsx2(
          "textarea",
          {
            value: String(value ?? ""),
            disabled: disabledBase,
            readOnly: submitted,
            rows: node.props?.rows ?? node.props?.Lines ?? 3,
            style: inputStyle,
            onChange: (e) => setField(field, e.target.value)
          }
        ),
        errors.map((e, i) => /* @__PURE__ */ jsx2("div", { style: errorStyle, children: e }, i)),
        node.helpText && /* @__PURE__ */ jsx2("div", { style: helpStyle, children: node.helpText })
      ] });
    case "select":
      return /* @__PURE__ */ jsxs2("div", { style: baseStyle, children: [
        node.label && /* @__PURE__ */ jsxs2("label", { style: labelStyle, "data-kb-field": node.fieldName, children: [
          node.label,
          required && /* @__PURE__ */ jsx2("span", { style: { color: "#c62828" }, children: " *" })
        ] }),
        /* @__PURE__ */ jsxs2("select", { value: String(value ?? ""), disabled: disabledBase || submitted, style: inputStyle, onChange: (e) => setField(field, e.target.value), children: [
          /* @__PURE__ */ jsx2("option", { value: "", children: "\u8BF7\u9009\u62E9" }),
          itemsOf(node).map((it, i) => /* @__PURE__ */ jsx2("option", { value: it.value, disabled: it.disabled, children: it.label }, i))
        ] }),
        errors.map((e, i) => /* @__PURE__ */ jsx2("div", { style: errorStyle, children: e }, i)),
        node.helpText && /* @__PURE__ */ jsx2("div", { style: helpStyle, children: node.helpText })
      ] });
    case "date":
      return /* @__PURE__ */ jsxs2("div", { style: baseStyle, children: [
        node.label && /* @__PURE__ */ jsxs2("label", { style: labelStyle, "data-kb-field": node.fieldName, children: [
          node.label,
          required && /* @__PURE__ */ jsx2("span", { style: { color: "#c62828" }, children: " *" })
        ] }),
        /* @__PURE__ */ jsx2("input", { type: "date", value: String(value ?? ""), disabled: disabledBase, readOnly: submitted, style: inputStyle, onChange: (e) => setField(field, e.target.value) }),
        errors.map((e, i) => /* @__PURE__ */ jsx2("div", { style: errorStyle, children: e }, i)),
        node.helpText && /* @__PURE__ */ jsx2("div", { style: helpStyle, children: node.helpText })
      ] });
    case "switch":
      return /* @__PURE__ */ jsxs2("div", { style: baseStyle, children: [
        /* @__PURE__ */ jsxs2("label", { style: { display: "flex", alignItems: "center", gap: 8, fontSize: 14, cursor: disabledBase || submitted ? "not-allowed" : "pointer" }, "data-kb-field": node.fieldName, children: [
          /* @__PURE__ */ jsx2(
            "input",
            {
              type: "checkbox",
              checked: value === true || value === "true" || value === 1,
              disabled: disabledBase || submitted,
              onChange: (e) => setField(field, e.target.checked)
            }
          ),
          node.label,
          required && /* @__PURE__ */ jsx2("span", { style: { color: "#c62828" }, children: " *" })
        ] }),
        errors.map((e, i) => /* @__PURE__ */ jsx2("div", { style: errorStyle, children: e }, i)),
        node.helpText && /* @__PURE__ */ jsx2("div", { style: helpStyle, children: node.helpText })
      ] });
    default:
      return null;
  }
}
function ButtonView({ node, store, onEvent }) {
  const submitted = store.get("__cjdsl_submitted") === true;
  const disabled = evalDslExpr(node.disabledIf, store) === true || submitted;
  const label = submitted && node.type !== "iconButton" ? "\u5DF2\u63D0\u4EA4" : node.label ?? "";
  const variant = node.props?.variant ?? node.props?.Variant ?? "text";
  const color = node.props?.color ?? node.props?.Color ?? "default";
  const colorMap = {
    primary: ["#1976D2", "#fff"],
    secondary: ["#7B1FA2", "#fff"],
    success: ["#2E7D32", "#fff"],
    error: ["#C62828", "#fff"],
    default: ["rgba(0,0,0,0.08)", "rgba(0,0,0,0.87)"]
  };
  const [bg, fg] = colorMap[color] ?? colorMap.default;
  const style = variant === "filled" ? { background: bg, color: fg, border: "none", borderRadius: 4, padding: "7px 16px", fontSize: 14, cursor: disabled ? "not-allowed" : "pointer", opacity: disabled ? 0.5 : 1, fontFamily: "inherit" } : variant === "outlined" ? { background: "transparent", color: bg, border: `1px solid ${bg}`, borderRadius: 4, padding: "6px 15px", fontSize: 14, cursor: disabled ? "not-allowed" : "pointer", opacity: disabled ? 0.5 : 1, fontFamily: "inherit" } : { background: "transparent", color: bg, border: "none", borderRadius: 4, padding: "6px 14px", fontSize: 14, cursor: disabled ? "not-allowed" : "pointer", opacity: disabled ? 0.5 : 1, fontFamily: "inherit" };
  const clickEv = (node.events ?? []).find((e) => e.type === "click" || e.type === "onClick");
  const href = node.props?.href;
  const common = { style, disabled, "data-cjdsl-id": node.id };
  if (href && isSafeLink(href)) {
    return /* @__PURE__ */ jsx2("a", { href: String(href), ...common, children: node.type === "iconButton" ? node.props?.icon ?? "\u26A1" : label });
  }
  if (clickEv) {
    return /* @__PURE__ */ jsx2("button", { ...common, onClick: () => void onEvent(clickEv), children: node.type === "iconButton" ? node.props?.icon ?? "\u26A1" : label });
  }
  return /* @__PURE__ */ jsx2("button", { ...common, onClick: () => void onEvent({ type: "click", handler: "showToast", params: { message: "\u6309\u94AE\u672A\u914D\u7F6E\u4E8B\u4EF6", severity: "warning" } }), children: node.type === "iconButton" ? node.props?.icon ?? "\u26A1" : label });
}
function ChartView({ node }) {
  const chartType = node.props?.ChartType ?? node.props?.chartType ?? "donut";
  if (chartType !== "pie" && chartType !== "donut") {
    return /* @__PURE__ */ jsxs2("div", { style: { color: "#888", fontSize: 12, padding: 6 }, children: [
      "chart v1 \u4EC5\u652F\u6301 Pie/Donut\uFF08\u5F53\u524D ",
      String(chartType),
      "\uFF09"
    ] });
  }
  const raw = node.props?.PieData ?? node.props?.pieData ?? [];
  const data = raw.map((d) => ({
    value: Number(d?.value ?? d?.Value ?? 0),
    label: String(d?.label ?? d?.Label ?? "")
  }));
  const labels = node.props?.Labels ?? node.props?.labels ?? [];
  data.forEach((d, i) => {
    if (!d.label && labels[i]) d.label = String(labels[i]);
  });
  const width = Number(node.props?.width ?? node.props?.Width ?? 300);
  const height = Number(node.props?.height ?? node.props?.Height ?? 300);
  const svg = buildDonutSvg(data, width, height, chartType === "donut");
  console.info("[cjdsl-page][ChartView]", {
    chartType,
    propsKeys: Object.keys(node.props ?? {}),
    rawLen: raw.length,
    firstRaw: raw[0],
    dataLen: data.length,
    firstData: data[0],
    total: data.reduce((s, d) => s + (Number(d.value) || 0), 0),
    width,
    height,
    svgLen: svg.length,
    svgHead: svg.slice(0, 120)
  });
  return /* @__PURE__ */ jsx2("div", { style: { display: "flex", justifyContent: "center", margin: "10px 0" }, dangerouslySetInnerHTML: { __html: svg } });
}

// ../CJDSL.React/src/store.ts
function getPath(obj, path) {
  if (path === "") return obj;
  const parts = path.split(".");
  let cur = obj;
  for (const p of parts) {
    if (cur === null || cur === void 0) return void 0;
    if (typeof cur !== "object") return void 0;
    cur = cur[p];
  }
  return cur;
}
var DslStore = class {
  constructor() {
    this.data = {};
    this.listeners = /* @__PURE__ */ new Set();
  }
  get(key) {
    if (key.startsWith("data.")) return getPath(this.data, key.slice(5));
    return getPath(this.data, key);
  }
  set(key, value) {
    const normalized = key.startsWith("data.") ? key.slice(5) : key;
    const parts = normalized.split(".");
    let cur = this.data;
    for (let i = 0; i < parts.length - 1; i++) {
      const p = parts[i];
      if (typeof cur[p] !== "object" || cur[p] === null) cur[p] = {};
      cur = cur[p];
    }
    cur[parts[parts.length - 1]] = value;
    this.emit();
  }
  merge(obj) {
    for (const [k, v] of Object.entries(obj)) {
      if (k.startsWith("data.")) this.set(k, v);
      else this.set(`data.${k}`, v);
    }
  }
  snapshot() {
    return JSON.parse(JSON.stringify(this.data));
  }
  subscribe(fn) {
    this.listeners.add(fn);
    return () => this.listeners.delete(fn);
  }
  emit() {
    for (const fn of [...this.listeners]) {
      try {
        fn();
      } catch {
      }
    }
  }
};

// ../CJDSL.React/src/dslPayload.ts
function extractJsonSubstring(text) {
  const span = extractJsonSpan(text);
  return span ? span.value : null;
}
function extractJsonSpan(text) {
  const cleaned = text.replace(/^```(?:dsl|json)?\s*\n?/, "").replace(/\n?```$/i, "").replace(/[\u0000-\u001F\u007F-\u009F]/g, "").replace(/复制代码?|复制$/g, "").trim();
  try {
    return { value: JSON.parse(cleaned), start: 0, end: text.length - 1 };
  } catch {
  }
  const start = text.search(/[[{]/);
  if (start < 0) return null;
  const open = text[start];
  const close = open === "{" ? "}" : "]";
  let depth = 0;
  let inStr = false;
  let esc2 = false;
  for (let i = start; i < text.length; i++) {
    const ch = text[i];
    if (inStr) {
      if (esc2) esc2 = false;
      else if (ch === "\\") esc2 = true;
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
function toDslNode(dsl) {
  if (dsl === null || dsl === void 0) return null;
  if (typeof dsl === "string") {
    const parsed = extractJsonSubstring(dsl);
    if (parsed && typeof parsed === "object") return parsed;
    return null;
  }
  if (typeof dsl === "object") {
    const rec = dsl;
    if (rec.components && Array.isArray(rec.components)) {
      return { type: "card", id: rec.id || "page", children: rec.components };
    }
    return rec;
  }
  return null;
}

// ../CJDSL.React/src/api.ts
var HttpCjdslApiClient = class {
  constructor(opts = {}) {
    this.base = (opts.baseUrl ?? (typeof location !== "undefined" ? location.origin : "")).replace(/\/+$/, "");
  }
  async handle(res) {
    if (!res.ok) {
      let detail = "";
      try {
        const body = await res.json();
        detail = body?.error || body?.message || JSON.stringify(body);
      } catch {
        detail = res.statusText;
      }
      throw new Error(`HTTP ${res.status}: ${detail}`);
    }
    return await res.json();
  }
  validateDsl(dsl) {
    return fetch(`${this.base}/api/cjdsl/validate`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ dsl })
    }).then((r) => this.handle(r));
  }
  submit(payload) {
    return fetch(`${this.base}/api/cjdsl/submit`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    }).then((r) => this.handle(r));
  }
  datasource(source) {
    return fetch(`${this.base}/api/cjdsl/datasource`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ source })
    }).then((r) => this.handle(r));
  }
  action(payload) {
    return fetch(`${this.base}/api/cjdsl/action`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload)
    }).then((r) => this.handle(r));
  }
};
var defaultApiClient = new HttpCjdslApiClient();

// ../CJDSL.React/src/ChatDslNode.tsx
import { useEffect as useEffect2, useMemo as useMemo3, useRef as useRef2, useState as useState3 } from "react";
import { jsx as jsx3, jsxs as jsxs3 } from "react/jsx-runtime";

// ../CJDSL.React/src/ToolCard.tsx
import { useMemo as useMemo4, useState as useState4 } from "react";
import { jsx as jsx4, jsxs as jsxs4 } from "react/jsx-runtime";

// src/styles.ts
var BASE_STYLE = `
  /* min-height:48px \u2014\u2014 \u4FDD\u5E95\u9AD8\u5EA6\uFF1ADSL \u89E3\u6790\u5931\u8D25/\u4E3A\u7A7A/\u6E32\u67D3\u5F02\u5E38\u65F6\u5361\u7247\u4ECD\u7559\u6709\u7A7A\u95F4\u5BB9\u7EB3
     \u6E90\u7801\u6309\u94AE\uFF08top:8 + 28px \u9AD8 = 36px\uFF09\uFF0C\u907F\u514D\u6309\u94AE\u88AB\u88C1\u5207\u5BFC\u81F4\u65E0\u6CD5\u515C\u5E95\u67E5\u770B\u6E90\u7801\u6392\u67E5\u3002
     \u6B63\u5E38\u6E32\u67D3\u65F6\u5185\u5BB9\u9AD8\u4E8E 48px\uFF0Cmin-height \u4E0D\u4EA7\u751F\u4EFB\u4F55\u89C6\u89C9\u5F71\u54CD\u3002 */
  :host { position: relative; display: block; min-height: 48px; box-sizing: border-box; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, "PingFang SC", "Microsoft YaHei", sans-serif; color: rgba(0,0,0,0.87); }
  * { box-sizing: border-box; }
  /* mount div \u81EA\u8EAB\u4E5F\u8BBE min-height \u2014\u2014 \u53CC\u4FDD\u9669\uFF1A\u5373\u4FBF React ErrorBoundary \u6CA1\u515C\u4F4F\uFF08web component +
     React 18 commit \u8FB9\u754C\u7684\u5DF2\u77E5\u9677\u9631\uFF09\uFF0Cshadow \u5185\u5BB9\u4ECD\u81F3\u5C11 48px \u9AD8\uFF0Cbridge \u5BB9\u5668\u4E0D\u584C\u6210 1-2px \u7070\u7EBF */
  #cjdsl-mount { min-height: 48px; display: block; }
  /* \u540C\u6B65 fallback div\uFF08\u72EC\u7ACB\u4E8E React \u6811\uFF09\uFF1AReact \u9996\u6B21 commit \u6210\u529F\u524D\u663E\u793A\uFF0Ccommit \u6210\u529F\u540E\u518D\u9690\u85CF\u3002
     position:relative + z-index:1 \u8BA9 React \u5185\u5BB9\u82E5\u6302\u8F7D\u4E0A\u53BB\u4F1A\u76D6\u5728\u5B83\u4E0A\u9762\uFF08\u4E0D\u51B2\u7A81\uFF09\u3002 */
  #cjdsl-fallback {
    display: flex; align-items: center; justify-content: center;
    min-height: 48px; padding: 8px 10px;
    color: #c62828; font-size: 12px; line-height: 1.5; text-align: center;
    border: 1px dashed #ef9a9a; border-radius: 8px; background: #fff8f8;
    position: relative; z-index: 1;
  }
  #cjdsl-fallback[hidden] { display: none; }
  #cjdsl-toast { position: absolute; top: 8px; left: 8px; right: 8px; padding: 8px 12px; border-radius: 6px; font-size: 13px; z-index: 999; display: none; box-shadow: 0 2px 8px rgba(0,0,0, 0.18); }
  /* \u6E90 JSON \u67E5\u770B\u6309\u94AE\uFF1A\u9ED8\u8BA4\u9690\u85CF\uFF0C\u9F20\u6807\u5212\u5165\u5361\u7247\uFF08\u6216\u952E\u76D8\u805A\u7126\uFF09\u540E\u663E\u793A\uFF08\u65B9\u6848 \xA73.2\uFF09 */
  .cjdsl-json-viewer-btn {
    position: absolute; top: 8px; right: 8px; z-index: 10;
    width: 28px; height: 28px; padding: 0; border: none; cursor: pointer;
    border-radius: 6px; background: rgba(0,0,0,0.45); color: #fff;
    display: inline-flex; align-items: center; justify-content: center;
    opacity: 0; pointer-events: none; transition: opacity .18s ease, background-color .18s ease;
    font-size: 14px; line-height: 1;
  }
  .cjdsl-json-viewer-btn:hover { background: rgba(0,0,0,0.65); }
  :host(:hover) .cjdsl-json-viewer-btn,
  :host(:focus-within) .cjdsl-json-viewer-btn { opacity: 1; pointer-events: auto; }
  /* \u6E32\u67D3\u5931\u8D25\u6001\uFF08host \u5E26 data-cjdsl-error\uFF09\uFF1A\u6E90\u7801\u6309\u94AE\u5E38\u9A7B\u53EF\u89C1 + \u8B66\u793A\u7EA2\uFF0C
     \u8BA9\u5931\u8D25\u5361\u7247\u4E00\u773C\u53EF\u8FA8\uFF0C\u4E14\u65E0\u9700 hover \u5373\u53EF\u70B9\u5F00\u6E90\u7801\u515C\u5E95\u6392\u67E5\uFF08\u6B63\u5E38\u6E32\u67D3\u4ECD\u8D70 hover \u903B\u8F91\uFF09\u3002 */
  :host([data-cjdsl-error]) .cjdsl-json-viewer-btn {
    opacity: 1; pointer-events: auto; background: #c62828;
  }
  :host([data-cjdsl-error]) .cjdsl-json-viewer-btn:hover { background: #b71c1c; }
  /* \u5931\u8D25\u6001\u89C6\u89C9\uFF08\u7EA2\u8272\u865A\u7EBF + \u6D45\u7EA2\u5E95\uFF09\u7531 #cjdsl-fallback div \u627F\u62C5\uFF08\u540C\u6B65\u3001\u4E0D\u4F9D\u8D56 React\uFF09\u3002
     \u8FD9\u91CC\u4E0D\u518D\u5728 :host \u4E0A\u52A0 border/background\uFF0C\u907F\u514D\u4E0E fallback div \u53CC\u5C42\u63CF\u8FB9\u3002 */
  /* \u6E90 JSON \u6D6E\u5C42\uFF08\u5361\u7247\u8DDF\u968F\u5F0F Popover\uFF0C\u65B9\u6848 \xA73.3 \u4FEE\u8BA2\uFF09
     position: fixed\uFF1Aescapes CjdslPageBridge \u7684 overflow:hidden \u88C1\u5207\uFF08bridge \u6E32\u67D3\u5BB9\u5668
     borderRadius:10 + overflow:hidden \u4F1A\u5207\u65AD absolute \u5B50\u5143\u7D20\u7684\u4E0B\u6CBF\uFF09\uFF0C\u7531 JsonViewerController
     \u6309 host.getBoundingClientRect() \u52A8\u6001\u8BA1\u7B97 top/right + flip\u3002
     max-height \u6536\u7D27\u5230 400px\uFF1ADA.DSH.PA \u662F MAUI \u6DF7\u5408\u5E94\u7528\uFF0CWebView2 \u89C6\u53E3\u5E95\u90E8\u88AB MAUI \u539F\u751F\u8F93\u5165\u6846
     \u8986\u76D6\u7EA6 100px\uFF08\u5177\u4F53\u503C\u7528\u6237\u6001\u53D8\u5316\uFF09\uFF0C100vh \u5305\u542B\u88AB\u906E\u7684\u90E8\u5206\u4F1A\u5BFC\u81F4\u6D6E\u5C42\u5E95\u90E8\u8FDB MAUI \u8F93\u5165\u6846\u5C42\uFF1B
     JS \u5728 syncPosition \u91CC\u6309\u300Chost \u4E0B\u65B9\u53EF\u7528\u7A7A\u95F4 vs \u4E0A\u65B9\u53EF\u7528\u7A7A\u95F4\u300D\u9009\u5927\u7684\u4E00\u4FA7\u653E\u7F6E + \u5FC5\u8981\u65F6\u518D
     \u6536\u7F29 max-height \u5230\u5B9E\u9645\u53EF\u7528\u503C\uFF0C\u4FDD\u8BC1\u6D6E\u5C42\u5B8C\u6574\u53EF\u89C1\u3002
     \u5E03\u5C40\u7528\u666E\u901A block\uFF08\u975E flex\uFF09\uFF1A\u907F\u514D flex-basis \u8986\u76D6 height \u5BFC\u81F4\u6EDA\u52A8\u5BB9\u5668\u9AD8\u5EA6\u5931\u6548\uFF0C
     body \u9AD8\u5EA6\u7531 JsonViewerController \u6253\u5F00\u65F6 JS \u663E\u5F0F\u8BA1\u7B97\u8BBE\u7F6E\uFF08panel.clientHeight - headH\uFF09 */
  .cjdsl-json-viewer-panel {
    position: fixed;
    display: none;
    width: min(560px, calc(100vw - 16px)); min-width: 320px;
    max-height: min(60vh, 400px, calc(100vh - 16px));
    background: #fff; border: 1px solid rgba(0,0,0,0.15); border-radius: 8px;
    box-shadow: 0 4px 16px rgba(0,0,0,0.18); overflow: hidden;
  }
  .cjdsl-json-viewer-panel-head {
    display: flex; align-items: center; justify-content: space-between;
    padding: 6px 10px; background: #f6f8fa; border-bottom: 1px solid rgba(0,0,0,0.08);
    font-size: 12px; font-weight: 600; color: #3a3f47;
  }
  .cjdsl-json-viewer-actions { display: flex; gap: 6px; }
  .cjdsl-json-viewer-actions button {
    padding: 2px 8px; font-size: 12px; border: 1px solid rgba(0,0,0,0.15);
    border-radius: 4px; background: #fff; color: #3a3f47; cursor: pointer;
  }
  .cjdsl-json-viewer-actions button:hover { background: #f0f2f5; }
  /* body \u4E3A\u552F\u4E00\u6EDA\u52A8\u5BB9\u5668\uFF1ACSS overflow:auto \u8986\u76D6 X+Y\uFF0CJS \u663E\u5F0F\u8BBE height/maxHeight \u7EA6\u675F Y\uFF1B
     \u5185\u5BB9\u8D85\u51FA\u5373\u53EF\u6EDA\u52A8\uFF0C\u4E0D\u4F9D\u8D56 flex \u6536\u7F29\u3002\u539F\u5148 pre \u4E0A\u4E5F\u6709 overflow:auto \u5F62\u6210\u53CC\u6EDA\u52A8\u6761\uFF0C
     \u89C6\u89C9\u4E0A\u53EA\u6709\u6700\u5185\u5C42\u751F\u6548\uFF0C\u5916\u5C42 body \u53CD\u800C\u770B\u4E0D\u51FA\u5728\u6EDA\u2014\u2014\u6545\u79FB\u9664 pre \u7684 overflow\u3002 */
  .cjdsl-json-viewer-body { overflow: auto; }
  .cjdsl-json-viewer-body pre {
    margin: 0; padding: 10px 12px; font-size: 12px; line-height: 1.55;
    font-family: Consolas, "SF Mono", Menlo, "Courier New", monospace; color: #24292f;
    white-space: pre;
  }
  /* \u6EDA\u52A8\u6761\u589E\u5F3A\u53EF\u89C1\u6027\uFF08WebKit / Firefox\uFF09 */
  .cjdsl-json-viewer-body::-webkit-scrollbar { width: 10px; height: 10px; }
  .cjdsl-json-viewer-body::-webkit-scrollbar-thumb { background: rgba(0,0,0,0.25); border-radius: 5px; border: 2px solid transparent; background-clip: content-box; }
  .cjdsl-json-viewer-body::-webkit-scrollbar-thumb:hover { background: rgba(0,0,0,0.4); border: 2px solid transparent; background-clip: content-box; }
  .cjdsl-json-viewer-body { scrollbar-width: thin; scrollbar-color: rgba(0,0,0,0.25) transparent; }
`;
var TOAST_COLORS = {
  info: "#0277BD",
  success: "#2E7D32",
  warning: "#F57C00",
  error: "#C62828"
};

// src/dsl-utils.ts
function parseDslSource(rawAttr, innerHtml) {
  let parsed = null;
  let raw = rawAttr;
  if (raw) {
    try {
      parsed = JSON.parse(raw);
    } catch {
      parsed = null;
    }
  }
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
function parseContextJson(raw) {
  if (!raw) return {};
  try {
    return JSON.parse(raw) || {};
  } catch {
    return {};
  }
}
function parseSubmittedAttribute(raw) {
  if (raw == null) return void 0;
  return raw === "true" || raw === "1";
}
function formatJson(raw) {
  if (!raw) return "\uFF08\u65E0 DSL \u5185\u5BB9\uFF09";
  try {
    return JSON.stringify(JSON.parse(raw), null, 2);
  } catch {
    return raw;
  }
}
function computeObjectCode(dslId, userContext) {
  return String(dslId || userContext?.objectCode || "dsl");
}

// src/json-viewer.ts
var JsonViewerController = class {
  constructor(deps) {
    this.deps = deps;
    this.button = null;
    this.panel = null;
    this.open = false;
    // 跟随 host 用的 passive 监听 + ResizeObserver（仅 open 时挂上，close/dispose 移除）
    this.reposition = () => {
      if (this.open) this.syncPosition();
    };
    this.hostResizeObs = null;
    this.onKeyDown = (e) => {
      if (e.key === "Escape" && this.open) this.toggle(false);
    };
    window.addEventListener("keydown", this.onKeyDown);
    if (typeof ResizeObserver !== "undefined") {
      const host = deps.shadowRoot.host;
      if (host) {
        this.hostResizeObs = new ResizeObserver(() => this.reposition());
        this.hostResizeObs.observe(host);
      }
    }
  }
  /** 元素卸载时移除 window 级监听（由主类 disconnectedCallback 调用） */
  dispose() {
    window.removeEventListener("keydown", this.onKeyDown);
    this.detachFollowListeners();
    this.hostResizeObs?.disconnect();
    this.hostResizeObs = null;
  }
  /** 渲染级开关同步（json-viewer 缺省 true；显式 false 时隐藏按钮并关闭浮层） */
  sync() {
    const on = this.deps.getEnabled();
    if (!on) {
      if (this.button) this.button.style.display = "none";
      if (this.panel) this.panel.style.display = "none";
      this.open = false;
      this.detachFollowListeners();
      return;
    }
    this.ensureUi();
    if (this.button) this.button.style.display = "";
  }
  /** 切换源 JSON 浮层（force 指定开/关，缺省翻转当前态） */
  toggle(force) {
    if (!this.deps.getEnabled()) return;
    this.ensureUi();
    if (!this.panel) return;
    const open = force ?? !this.open;
    this.panel.style.display = open ? "block" : "none";
    this.open = open;
    if (open) {
      const code = this.panel.querySelector("code");
      if (code) {
        const raw = this.deps.getRaw();
        const formatted = formatJson(raw);
        if (raw.length > 500 * 1024) {
          code.textContent = "\u5185\u5BB9\u8FC7\u5927\uFF0C\u8BF7\u4ECE\u63A7\u5236\u53F0\u67E5\u770B\u3002\n\n" + formatted.slice(0, 2e4) + "\n\u2026\uFF08\u5DF2\u622A\u65AD\uFF09";
        } else {
          code.textContent = formatted;
        }
      }
      requestAnimationFrame(() => {
        this.syncPosition();
        this.attachFollowListeners();
      });
    } else {
      this.detachFollowListeners();
      if (this.panel) {
        this.panel.style.top = "";
        this.panel.style.bottom = "";
        this.panel.style.right = "";
        this.panel.style.maxHeight = "";
      }
    }
    this.deps.onOpenChange(open);
  }
  /**
   * 按 host 视口位置放置 panel（fixed 定位 + flip）。
   *   默认下方：top = host.top + 44（按钮底下方 8px），maxH = cssMax
   *   若 host 下方可用空间 < 上方可用空间 → flip 到 host 上方：
   *     bottom = vh - (host.top - 8)，maxH = min(cssMax, 上方空间)
   *   两边都放不下时取较大一边，maxH 收到实际可用值，确保浮层完整可见
   *   （特别针对 MAUI 混合应用：WebView2 视口底部被 MAUI 原生输入框覆盖，
   *   100vh 包含被遮区域，浮层若只放下方必然被输入框盖住下半部分）。
   *   right = (vw - host.right) + 8，沿用原 absolute 时期的 8px 右外边距。
   *   host 不可见（rect 退化）时跳过，让 panel 留在上次位置避免跳动。
   */
  syncPosition() {
    if (!this.panel) return;
    const rect = this.deps.getHostRect();
    if (rect.width <= 0 && rect.height <= 0) return;
    const vw = window.innerWidth;
    const vh = window.innerHeight;
    const cssMax = Math.min(vh - 16, Math.min(vh * 0.6, 400));
    const MIN_PANEL_H = 160;
    const belowTop = rect.top + 44;
    const belowAvail = Math.max(0, vh - belowTop - 16);
    const aboveBottom = rect.top - 8;
    const aboveAvail = Math.max(0, aboveBottom - 8);
    const placeBelow = belowAvail >= aboveAvail;
    const avail = placeBelow ? belowAvail : aboveAvail;
    const targetH = Math.max(MIN_PANEL_H, Math.min(cssMax, avail));
    if (placeBelow) {
      this.panel.style.top = `${belowTop}px`;
      this.panel.style.bottom = "auto";
    } else {
      this.panel.style.bottom = `${vh - aboveBottom}px`;
      this.panel.style.top = "auto";
    }
    this.panel.style.maxHeight = `${targetH}px`;
    const right = Math.max(8, vw - rect.right + 8);
    this.panel.style.right = `${right}px`;
    this.syncBodyHeight();
  }
  /** open 期间挂跟随监听：滚动（capture 截获嵌套滚动容器）+ 视口 resize + host 尺寸变化（已由 ResizeObserver 覆盖） */
  attachFollowListeners() {
    window.addEventListener("scroll", this.reposition, { passive: true, capture: true });
    window.addEventListener("resize", this.reposition, { passive: true });
  }
  /** close/dispose 时清理跟随监听，避免泄漏 */
  detachFollowListeners() {
    window.removeEventListener("scroll", this.reposition, { capture: true });
    window.removeEventListener("resize", this.reposition);
  }
  /**
   * 显式约束滚动容器（body）高度 = 面板可用高度 - 头部高度，确保内容超出时 overflow 滚动生效。
   * panel 为 block 布局，clientHeight 受 max-height:min(70vh,520px) 限制；
   * body 为独立滚动容器（非 flex item，height 内联生效），内容超出即可滚动。
   */
  syncBodyHeight() {
    if (!this.panel) return;
    const head = this.panel.querySelector(".cjdsl-json-viewer-panel-head");
    const body = this.panel.querySelector(".cjdsl-json-viewer-body");
    if (!head || !body) return;
    const panelH = this.panel.clientHeight;
    const headH = head.offsetHeight;
    const bodyH = Math.max(80, panelH - headH);
    body.style.height = bodyH + "px";
    body.style.maxHeight = bodyH + "px";
    body.style.overflowY = "auto";
  }
  /** 复制源 JSON（navigator.clipboard 优先，失败降级 execCommand） */
  copy() {
    const text = formatJson(this.deps.getRaw());
    const done = () => {
      if (this.button) {
        const old = this.button.title;
        this.button.title = "\u5DF2\u590D\u5236";
        window.setTimeout(() => {
          if (this.button) this.button.title = old;
        }, 1200);
      }
    };
    const fallback = () => {
      try {
        const ta = document.createElement("textarea");
        ta.value = text;
        ta.style.position = "fixed";
        ta.style.opacity = "0";
        document.body.appendChild(ta);
        ta.select();
        document.execCommand("copy");
        ta.remove();
        done();
      } catch {
      }
    };
    if (navigator.clipboard?.writeText) {
      navigator.clipboard.writeText(text).then(done).catch(fallback);
    } else {
      fallback();
    }
  }
  /** 惰性创建按钮与浮层骨架（关闭时 DOM 不创建，零运行时开销） */
  ensureUi() {
    const shadow = this.deps.shadowRoot;
    if (!shadow || this.button) return;
    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = "cjdsl-json-viewer-btn";
    btn.setAttribute("aria-label", "\u67E5\u770B CJDSL \u6E90 JSON");
    btn.title = "\u67E5\u770B\u6E90 JSON";
    btn.innerHTML = '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true"><path d="M8 3H7a2 2 0 0 0-2 2v5a2 2 0 0 1-2 2 2 2 0 0 1 2 2v5c0 1.1.9 2 2 2h1"/><path d="M16 21h1a2 2 0 0 0 2-2v-5c0-1.1.9-2 2-2a2 2 0 0 1-2-2V5a2 2 0 0 0-2-2h-1"/></svg>';
    btn.addEventListener("click", (e) => {
      e.stopPropagation();
      this.toggle();
    });
    shadow.appendChild(btn);
    this.button = btn;
    const panel = document.createElement("div");
    panel.className = "cjdsl-json-viewer-panel";
    panel.innerHTML = '<div class="cjdsl-json-viewer-panel-head"><span>CJDSL \u6E90 JSON</span><span class="cjdsl-json-viewer-actions"><button type="button" data-copy>\u590D\u5236</button><button type="button" data-close>\u5173\u95ED</button></span></div><div class="cjdsl-json-viewer-body"><pre><code></code></pre></div>';
    panel.querySelector("[data-copy]").addEventListener("click", (e) => {
      e.stopPropagation();
      this.copy();
    });
    panel.querySelector("[data-close]").addEventListener("click", (e) => {
      e.stopPropagation();
      this.toggle(false);
    });
    panel.addEventListener("click", (e) => e.stopPropagation());
    shadow.appendChild(panel);
    this.panel = panel;
    shadow.addEventListener("click", (e) => {
      if (!this.open) return;
      const t = e.target;
      if (t === this.button) return;
      if (this.panel && !this.panel.contains(t)) this.toggle(false);
    });
  }
};

// src/render-mount.ts
import React5, { useEffect as useEffect3 } from "react";
function createEmptyPlaceholder(reason = "empty", onCommit) {
  const placeholder = React5.createElement(
    "div",
    {
      style: reason === "invalid" ? {
        color: "#c62828",
        fontSize: 12,
        padding: "8px 10px",
        border: "1px dashed #ef9a9a",
        borderRadius: 4,
        background: "#ffebee"
      } : { color: "#888", fontSize: 13, padding: 8 }
    },
    reason === "invalid" ? "DSL \u89E3\u6790\u5931\u8D25\u6216\u6E32\u67D3\u5F02\u5E38\uFF0C\u70B9\u51FB\u53F3\u4E0A\u89D2\u6309\u94AE\u67E5\u770B\u6E90\u7801\u6392\u67E5" : "\uFF08\u65E0 DSL \u5185\u5BB9\uFF09"
  );
  if (!onCommit) return placeholder;
  return React5.createElement(
    React5.Fragment,
    null,
    React5.createElement(CommitSuccessNotifier, { onCommit }),
    placeholder
  );
}
var DslRenderErrorBoundary = class extends React5.Component {
  constructor(props) {
    super(props);
    this.state = { failed: false };
  }
  static getDerivedStateFromError() {
    return { failed: true };
  }
  componentDidCatch(error) {
    this.props.onError(error);
  }
  render() {
    if (this.state.failed) return createEmptyPlaceholder("invalid");
    return this.props.children;
  }
};
function createDslRendererElement(root, store, callbacks) {
  return React5.createElement(DslRenderer, { root, store, callbacks });
}
function createErrorBoundedRendererElement(root, store, callbacks, onError, onCommit) {
  return React5.createElement(
    DslRenderErrorBoundary,
    { onError },
    React5.createElement(CommitSuccessNotifier, { onCommit }),
    createDslRendererElement(root, store, callbacks)
  );
}
function CommitSuccessNotifier({ onCommit }) {
  useEffect3(() => {
    onCommit();
  }, [onCommit]);
  return null;
}
function createRendererCallbacks(deps) {
  return {
    mode: deps.getMode(),
    onSubmit: (ctx) => {
      deps.store.set("__cjdsl_submitted", true);
      deps.onSubmitted();
      deps.dispatchAction({
        type: "submit",
        action: ctx.action,
        data: ctx.values
      });
      return { ok: true, message: "\u5DF2\u63D0\u4EA4\uFF0C\u7B49\u5F85\u5BBF\u4E3B\u5904\u7406" };
    },
    onApiCall: (params, formValues) => {
      deps.dispatchAction({
        type: "apiCall",
        action: String(params?.action ?? ""),
        data: formValues,
        apiParams: params
      });
      return { ok: true, message: "\u5DF2\u53D1\u8D77 API \u8C03\u7528\uFF0C\u7B49\u5F85\u5BBF\u4E3B\u5904\u7406" };
    },
    onToast: (msg, sev) => deps.showToast(msg, sev || "info"),
    onNavigate: (path) => {
      deps.dispatchAction({ type: "navigate", action: "navigate", data: { path } });
    }
  };
}

// src/cjdsl-page.ts
if (typeof Document !== "undefined" && typeof Document.prototype.createElement === "function") {
  const _origCreateElement = Document.prototype.createElement;
  Document.prototype.createElement = function(localName, options) {
    try {
      if (options && typeof localName === "string" && localName.indexOf("-") >= 0) {
        console.warn("[cjdsl-page] autonomous CE \u8C03\u7528\u5E26 options\uFF0C\u5DF2\u4E22\u5F03\uFF08\u542B\u8FDE\u5B57\u7B26\u7684\u5143\u7D20\u4E0D\u63A5\u53D7 is\uFF09", {
          localName,
          is: options.is
        });
        return _origCreateElement.call(this, localName);
      }
      try {
        return _origCreateElement.call(this, localName, options);
      } catch (e) {
        const errMsg = e?.message;
        if (options) {
          try {
            return _origCreateElement.call(this, localName);
          } catch (_) {
          }
        }
        try {
          const el = this.createElementNS("http://www.w3.org/1999/xhtml", localName);
          console.warn("[cjdsl-page] createElement \u629B\u9519\uFF0C\u5DF2\u7528 createElementNS \u515C\u5E95\u6210\u529F", {
            localName,
            options,
            err: errMsg
          });
          return el;
        } catch (_) {
        }
        console.error("[cjdsl-page] createElement \u5F7B\u5E95\u5931\u8D25\uFF08\u5DF2\u964D\u7EA7\u4E3A\u7A7A div\uFF0C\u4E0D\u629B\u5F02\u5E38\uFF09", {
          localName,
          localNameType: typeof localName,
          options,
          err: errMsg
        });
        try {
          return _origCreateElement.call(this, "div");
        } catch (_) {
          return void 0;
        }
      }
    } catch (outer) {
      console.error("[cjdsl-page][GUARD] \u5916\u5C42\u5B89\u5168\u7F51\uFF1A\u5185\u90E8\u4ECD\u629B\u9519\uFF0C\u964D\u7EA7\u4E3A\u7A7A div", {
        localName,
        options,
        err: outer?.message
      });
      try {
        return _origCreateElement.call(this, "div");
      } catch (_) {
        return void 0;
      }
    }
  };
  console.info("[cjdsl-page] createElement guard v9 active (chart-fix + visible-logs + never-throw)");
}
var CjdslPage = class extends HTMLElement {
  constructor() {
    super();
    this.root = null;
    this.store = new DslStore();
    this.dslNode = null;
    this.userContext = {};
    // 源 JSON 查看按钮（方案 §3.5）：rawDslJson 保存 dsl 属性原始 JSON 字符串（浮层展示源）
    this.rawDslJson = "";
    // 同步兜底 div（独立于 React 树）：**默认 hidden（不显示）**，只有「渲染健康检查」
    // 判定 React 未挂载任何内容时才显示。这样正常渲染完全不受干扰（关键：不能默认显示，
    // 否则正常卡片下方也会多出一块占位）。
    this.fallbackEl = null;
    /**
     * 渲染健康检查定时器。
     *
     * 为什么需要它：React 18 的 ErrorBoundary / useEffect 在 Web Component shadow DOM 内
     * 并非 100% 可靠 —— 实测「DSL 解析成功但 React commit 阶段失败」（如 90 项 PieData 的
     * 超大 SVG）时，render() 走的是**成功分支**（dslNode != null），于是 removeAttribute
     * 把按钮打回 hover 隐藏态，而 ErrorBoundary 未兜住 → 卡片彻底变成空壳，源码按钮点不到。
     *
     * 解法：render() 后延迟 150ms 直接查 DOM —— mount div 是否真的有子节点。
     * 这是唯一不依赖 React 内部机制的可靠判定。commit 成功则由 CommitSuccessNotifier
     * 的 useEffect 提前取消检查。
     */
    this.healthCheckTimer = null;
    // render 监听：用 MutationObserver 替代纯定时器，可靠捕获 React 真实提交（shadow DOM 内 useEffect/ErrorBoundary 不可靠）
    this.renderObserver = null;
    // 已判定失败标记：防止 observer/effect 在错误被捕获后又误回滚成成功态
    this.renderFailed = false;
    const shadow = this.attachShadow({ mode: "open" });
    const style = document.createElement("style");
    style.textContent = BASE_STYLE;
    shadow.appendChild(style);
    const mount = document.createElement("div");
    mount.id = "cjdsl-mount";
    shadow.appendChild(mount);
    this.fallbackEl = document.createElement("div");
    this.fallbackEl.id = "cjdsl-fallback";
    this.fallbackEl.hidden = true;
    this.fallbackEl.textContent = "DSL \u89E3\u6790\u5931\u8D25\u6216\u6E32\u67D3\u5F02\u5E38\uFF0C\u70B9\u51FB\u53F3\u4E0A\u89D2\u6309\u94AE\u67E5\u770B\u6E90\u7801\u6392\u67E5";
    shadow.appendChild(this.fallbackEl);
    this.root = createRoot(mount);
    this.jsonViewer = new JsonViewerController({
      shadowRoot: shadow,
      getEnabled: () => this.getAttribute("json-viewer") !== "false",
      getRaw: () => this.rawDslJson,
      getObjectCode: () => this.objectCode(),
      getHostRect: () => this.getBoundingClientRect(),
      // panel 改 fixed 后按 host 视口矩形动态定位
      onOpenChange: (open) => {
        this.dispatchEvent(
          new CustomEvent("cjdsl-json-view", {
            bubbles: true,
            composed: true,
            detail: { open, objectCode: this.objectCode() }
          })
        );
      }
    });
  }
  static get observedAttributes() {
    return ["dsl", "context", "submitted", "values", "json-viewer"];
  }
  connectedCallback() {
    console.info("[cjdsl-page] v9 instance connected", {
      instanceId: this.__id ?? (this.__id = ++globalThis.__cjdsl_n__),
      mode: this.getAttribute("mode"),
      dslLen: this.getAttribute("dsl")?.length ?? 0
    });
    this.style.minHeight = "48px";
    this.style.display = "block";
    if (!this.root) {
      const mount = this.shadowRoot?.getElementById("cjdsl-mount");
      if (mount) this.root = createRoot(mount);
    }
    if (!this.fallbackEl) {
      this.fallbackEl = this.shadowRoot?.getElementById("cjdsl-fallback") ?? null;
    }
    this.parseDsl();
    this.parseContext();
    this.restoreSubmitted();
    this.jsonViewer.sync();
    this.render();
    this.dispatchEvent(
      new CustomEvent("cjdsl-ready", {
        bubbles: true,
        composed: true,
        detail: { id: this.id || void 0 }
      })
    );
  }
  disconnectedCallback() {
    if (this.healthCheckTimer !== null) {
      window.clearTimeout(this.healthCheckTimer);
      this.healthCheckTimer = null;
    }
    if (this.renderObserver) {
      this.renderObserver.disconnect();
      this.renderObserver = null;
    }
    this.renderFailed = false;
    this.jsonViewer.dispose();
    this.root?.unmount();
    this.root = null;
    this.fallbackEl = null;
  }
  attributeChangedCallback(name, _old, _new) {
    if (name === "json-viewer") {
      this.jsonViewer.sync();
      return;
    }
    this.parseDsl();
    this.parseContext();
    if (name === "submitted") this.restoreSubmitted();
    if (name === "values") this.restoreValues();
    this.render();
  }
  /** 宿主回传结果（方案：宿主回传经 Web Component 暴露的方法） */
  applyResult(result) {
    if (result.setValues) this.store.merge(result.setValues);
    if (result.ok === true) {
      this.store.set("__cjdsl_submitted", true);
      this.notifySubmitted();
    } else if (result.ok === false) {
      this.store.set("__cjdsl_submitted", false);
      this.notifySubmitted();
    }
    if (result.message) {
      this.showToast(result.message, result.severity || (result.ok === false ? "error" : "info"));
    }
    if (result.refresh) this.render();
  }
  /** 从 submitted 属性恢复提交态（兼容 "true"/"false"/"1"/"0"；未显式给出时不动，保持默认未提交） */
  restoreSubmitted() {
    const submitted = parseSubmittedAttribute(this.getAttribute("submitted"));
    if (submitted === void 0) return;
    this.store.set("__cjdsl_submitted", submitted);
  }
  /** 收集当前表单字段值（供提交时随事件上抛持久化；剔除内部提交态键） */
  collectValues() {
    const snap = this.store.snapshot();
    const out = {};
    for (const [k, v] of Object.entries(snap)) {
      if (k === "__cjdsl_submitted") continue;
      out[k] = v;
    }
    return out;
  }
  /** 从 values 属性回填持久化字段值（延迟到 React layout effect seed 之后 merge，保证已提交值优先于预填） */
  restoreValues() {
    const raw = this.getAttribute("values");
    if (raw == null) return;
    let parsed = null;
    try {
      parsed = JSON.parse(raw);
    } catch {
      parsed = null;
    }
    if (!parsed || typeof parsed !== "object") return;
    queueMicrotask(() => {
      this.store.merge(parsed);
    });
  }
  /** 提交态变更通知：上抛 CustomEvent，供宿主桥（CjdslPageBridge）持久化到 PA 端本地 */
  notifySubmitted() {
    this.dispatchEvent(
      new CustomEvent("cjdsl-submitted", {
        bubbles: true,
        composed: true,
        detail: {
          submitted: this.store.get("__cjdsl_submitted") === true,
          values: this.collectValues()
        }
      })
    );
  }
  parseDsl() {
    const { parsed, rawSource } = parseDslSource(this.getAttribute("dsl"), this.innerHTML);
    this.rawDslJson = rawSource;
    this.dslNode = toDslNode(parsed) ?? null;
  }
  parseContext() {
    this.userContext = parseContextJson(this.getAttribute("context"));
  }
  objectCode() {
    return computeObjectCode(this.dslNode?.id, this.userContext);
  }
  dispatchAction(detail) {
    this.dispatchEvent(
      new CustomEvent("cjdsl-action", {
        bubbles: true,
        composed: true,
        detail: { objectCode: this.objectCode(), context: this.userContext, ...detail }
      })
    );
  }
  showToast(message, severity = "info") {
    this.dispatchAction({ type: "toast", action: "toast", message, severity });
    const shadow = this.shadowRoot;
    if (!shadow) return;
    let bar = shadow.getElementById("cjdsl-toast");
    if (!bar) {
      bar = document.createElement("div");
      bar.id = "cjdsl-toast";
      shadow.appendChild(bar);
    }
    bar.textContent = message;
    bar.style.color = "#fff";
    bar.style.display = "block";
    bar.style.background = TOAST_COLORS[severity] || TOAST_COLORS.info;
    window.clearTimeout(bar._t);
    bar._t = window.setTimeout(() => {
      if (bar) bar.style.display = "none";
    }, 3e3);
  }
  render() {
    if (!this.root) return;
    if (!this.dslNode) {
      const hasRaw = this.rawDslJson.trim().length > 0;
      this.root.render(createEmptyPlaceholder(hasRaw ? "invalid" : "empty"));
      this.markRenderFailed();
      return;
    }
    this.removeAttribute("data-cjdsl-error");
    this.root.render(
      // ErrorBoundary 兜住 DslRenderer 运行时抛错（尽力而为，shadow DOM 内不保证生效）。
      // onCommit 由内嵌 CommitSuccessNotifier 的 useEffect 在 commit 成功后调 → clearRenderWatch（secondary 成功信号）。
      // 主成功信号是 startRenderWatch 里的 MutationObserver，不依赖此 useEffect。
      createErrorBoundedRendererElement(
        this.dslNode,
        this.store,
        createRendererCallbacks({
          getMode: () => this.getAttribute("mode") || void 0,
          store: this.store,
          onSubmitted: () => this.notifySubmitted(),
          dispatchAction: (detail) => this.dispatchAction(detail),
          showToast: (message, severity) => this.showToast(message, severity)
        }),
        (err) => this.onRenderError(err),
        () => this.cancelHealthCheck()
      )
    );
    this.restoreValues();
    console.info("[cjdsl-page] render success-branch", {
      dslId: this.dslNode?.id,
      nodeTypes: this.collectNodeTypes(this.dslNode)
    });
    this.startRenderWatch();
    queueMicrotask(() => {
      const mount = this.shadowRoot?.getElementById("cjdsl-mount");
      if (!mount) return;
      const childInfo = Array.from(mount.children).map((el) => ({
        tag: el.tagName.toLowerCase(),
        cls: el.className?.toString().slice(0, 40) || "",
        text0: (el.textContent || "").slice(0, 30)
      }));
      console.info("[cjdsl-page] v9 mount.children after render", {
        childCount: mount.children.length,
        childInfo
      });
    });
  }
  /** 递归收集 DSL 树里出现的所有节点类型（排障用：确认到底有没有 chart 节点） */
  collectNodeTypes(node, out = []) {
    if (!node || typeof node !== "object") return out;
    const n = node;
    if (typeof n.type === "string") out.push(n.type);
    if (Array.isArray(n.children)) {
      for (const c of n.children) this.collectNodeTypes(c, out);
    }
    return out;
  }
  /**
   * 启动渲染监听（替代旧版纯 150ms 定时器）。
   *
   * 为什么改：旧逻辑靠 (a) 150ms 后查 mount.childNodes.length 与 (b) CommitSuccessNotifier
   * 的 useEffect 回滚来判定成功/失败。但实测 Web Component shadow DOM 内 useEffect 不可靠——
   * 慢提交（如 90 项 PieData 大 SVG）超过 150ms 才 commit，定时器先误判失败显示灰框，而
   * useEffect 又未必触发回滚，于是正常卡片被永久误伤成灰框。
   *
   * 新逻辑：用 MutationObserver 监听 mount 的 childList —— 这是 DOM 级、shadow DOM 内可靠
   * 的提交信号。React 一旦把任何内容（含注释占位节点）挂进 mount，observer 立即触发 → 判定成功、
   * 断开观察、隐藏兜底。仅当 800ms 宽限内始终无内容时才判定失败（覆盖 React 完全不 commit 的极端场景）。
   */
  startRenderWatch() {
    this.renderFailed = false;
    this.clearRenderWatch();
    const mount = this.shadowRoot?.getElementById("cjdsl-mount");
    if (!mount) {
      this.markRenderFailed("no-mount");
      return;
    }
    if (mount.childNodes.length > 0) {
      console.info("[cjdsl-page] mount \u5DF2\u6709\u5185\u5BB9\uFF0C\u5224\u5B9A\u6210\u529F");
      return;
    }
    this.renderObserver = new MutationObserver(() => {
      if (mount.childNodes.length > 0) {
        console.info("[cjdsl-page] MutationObserver \u6355\u83B7\u5230\u63D0\u4EA4\uFF0C\u5224\u5B9A\u6210\u529F");
        this.clearRenderWatch();
      }
    });
    this.renderObserver.observe(mount, { childList: true, subtree: false });
    this.healthCheckTimer = window.setTimeout(() => {
      if (mount.childNodes.length === 0) {
        console.error("[cjdsl-page] \u6E32\u67D3\u76D1\u542C\u8D85\u65F6\uFF1A800ms \u5185 mount \u65E0\u5185\u5BB9\uFF0C\u5224\u5B9A\u6E32\u67D3\u5931\u8D25", {
          dslId: this.dslNode?.id
        });
        this.markRenderFailed("timeout-empty");
      }
      this.clearRenderWatch();
    }, 800);
  }
  /**
   * 清理渲染监听（成功路径调用）：断开 MutationObserver、清除宽限定时器、隐藏兜底 div、移除 error 标记。
   * 同时作为 CommitSuccessNotifier useEffect（secondary 信号，shadow DOM 内不可靠）的 onCommit 目标。
   * 已判定失败时直接 no-op，避免 observer/effect 误回滚把失败态擦掉。
   */
  clearRenderWatch() {
    if (this.renderFailed) return;
    if (this.healthCheckTimer !== null) {
      window.clearTimeout(this.healthCheckTimer);
      this.healthCheckTimer = null;
    }
    if (this.renderObserver) {
      this.renderObserver.disconnect();
      this.renderObserver = null;
    }
    if (this.fallbackEl) this.fallbackEl.hidden = true;
    this.removeAttribute("data-cjdsl-error");
  }
  /** 旧名兼容：CommitSuccessNotifier 的 useEffect 触发时调用（仅作 secondary 成功信号） */
  cancelHealthCheck() {
    this.clearRenderWatch();
  }
  /**
   * 判定渲染失败：打 error 标记（源码按钮常驻 + 警示红）+ 显示同步兜底 div，并停止渲染监听。
   * reason 仅用于诊断日志，方便在 DevTools 控制台区分失败来源。
   */
  markRenderFailed(reason = "unknown") {
    this.renderFailed = true;
    console.warn("[cjdsl-page] \u6E32\u67D3\u5224\u5B9A\u5931\u8D25", { reason, dslId: this.dslNode?.id });
    this.toggleAttribute("data-cjdsl-error", true);
    if (this.fallbackEl) this.fallbackEl.hidden = false;
    if (this.healthCheckTimer !== null) {
      window.clearTimeout(this.healthCheckTimer);
      this.healthCheckTimer = null;
    }
    if (this.renderObserver) {
      this.renderObserver.disconnect();
      this.renderObserver = null;
    }
  }
  /** DslRenderer 运行时异常回调（ErrorBoundary 生效时触发）：留痕 + 判定失败 */
  onRenderError(err) {
    console.error("[cjdsl-page] DSL \u6E32\u67D3\u5F02\u5E38\uFF0C\u5DF2\u964D\u7EA7\u4E3A\u5931\u8D25\u5360\u4F4D\uFF08\u53EF\u70B9\u6E90\u7801\u6309\u94AE\u67E5\u770B\u539F\u59CB DSL\uFF09", err);
    this.markRenderFailed();
  }
};

// src/index.ts
var TAG_NAME = "cjdsl-page";
function defineCjdslPage(tag = TAG_NAME) {
  if (typeof customElements === "undefined") return;
  if (!customElements.get(tag)) {
    customElements.define(tag, CjdslPage);
  }
}
defineCjdslPage();
if (typeof window !== "undefined") {
  window.defineCjdslPage = defineCjdslPage;
}
export {
  CjdslPage,
  TAG_NAME,
  defineCjdslPage
};
