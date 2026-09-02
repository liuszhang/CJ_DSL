window.__ModuleLoader__.load({ id: "@cj/cjdsl-react", factory: (require) => { var module = { exports: {} }; var exports = module.exports;
"use strict";
var __defProp = Object.defineProperty;
var __getOwnPropDesc = Object.getOwnPropertyDescriptor;
var __getOwnPropNames = Object.getOwnPropertyNames;
var __hasOwnProp = Object.prototype.hasOwnProperty;
var __export = (target, all) => {
  for (var name in all)
    __defProp(target, name, { get: all[name], enumerable: true });
};
var __copyProps = (to, from, except, desc) => {
  if (from && typeof from === "object" || typeof from === "function") {
    for (let key of __getOwnPropNames(from))
      if (!__hasOwnProp.call(to, key) && key !== except)
        __defProp(to, key, { get: () => from[key], enumerable: !(desc = __getOwnPropDesc(from, key)) || desc.enumerable });
  }
  return to;
};
var __toCommonJS = (mod) => __copyProps(__defProp({}, "__esModule", { value: true }), mod);

// src/client-entry.tsx
var client_entry_exports = {};
__export(client_entry_exports, {
  apply: () => apply,
  inject: () => inject
});
module.exports = __toCommonJS(client_entry_exports);

// src/DslRenderer.tsx
var import_react2 = require("react");

// src/expr.ts
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

// src/validate.ts
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

// src/events.ts
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

// src/svg.ts
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

// src/flow.tsx
var import_react = require("react");
var import_jsx_runtime = require("react/jsx-runtime");
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
  const [selectedId, setSelectedId] = (0, import_react.useState)(null);
  const nodes = (0, import_react.useMemo)(() => {
    const direct = toArray(props.nodes);
    if (direct.length > 0) return direct;
    const bound = store.get(node.dataBind ?? "datasource.items");
    return toArray(bound);
  }, [props.nodes, node.dataBind, store]);
  const edges = (0, import_react.useMemo)(() => toArray(props.edges), [props.edges]);
  const eliminated = (0, import_react.useMemo)(() => toArray(props.eliminated), [props.eliminated]);
  const highlightOnClick = props.highlightOnClick === true;
  const vertical = props.layout === "vertical";
  const interactive = highlightOnClick || (node.events ?? []).some((e) => e.type === "click" || e.type === "onClick");
  if (nodes.length === 0) {
    return /* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", { style: { color: "#999", fontSize: 12, padding: 6 }, children: "\uFF08\u65E0\u6EAF\u6E90\u8DEF\u5F84\u6570\u636E\uFF09" });
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
  return /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", { className: "cjdsl-flow", style: pickFlowStyle(node.style), "data-cjdsl-id": node.id, children: [
    node.label && /* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", { style: { fontSize: 15, fontWeight: 600, margin: "4px 0 8px" }, children: node.label }),
    /* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", { style: chainStyle, children: nodes.map((fn, i) => {
      const next = i < nodes.length - 1 ? nodes[i + 1] : void 0;
      const relation = next ? relationOf(fn.id, next.id) : "";
      return /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", { style: { display: "flex", flexDirection: vertical ? "column" : "row", alignItems: "center", gap: 8, flex: "0 0 auto" }, children: [
        /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", { style: nodeCardStyle(fn), onClick: () => handleNodeClick(fn), children: [
          /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", { style: { display: "flex", alignItems: "center", justifyContent: "space-between", gap: 6 }, children: [
            /* @__PURE__ */ (0, import_jsx_runtime.jsx)("span", { style: { fontWeight: 600, fontSize: 13, wordBreak: "break-all" }, children: fn.node }),
            fn.type && /* @__PURE__ */ (0, import_jsx_runtime.jsx)("span", { style: { fontSize: 10, color: "#1565c0", border: "1px solid #90caf9", borderRadius: 10, padding: "0 6px", lineHeight: "16px", whiteSpace: "nowrap" }, children: fn.type })
          ] }),
          truncateNote(fn.note) && /* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", { style: { fontSize: 12, color: "#666", marginTop: 4, wordBreak: "break-word" }, children: truncateNote(fn.note) }),
          /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", { style: { display: "flex", justifyContent: "space-between", marginTop: 6, fontSize: 11 }, children: [
            /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("span", { style: { color: strengthColor(fn.evidenceStrength), fontWeight: 600 }, children: [
              "\u8BC1\u636E ",
              fmtPercent(fn.evidenceStrength)
            ] }),
            /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("span", { style: { color: "#777" }, children: [
              "\u7F6E\u4FE1 ",
              fmtPercent(fn.pathConfidence)
            ] })
          ] })
        ] }),
        next && /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", { style: { display: "flex", flexDirection: "column", alignItems: "center", color: "#999", fontSize: 12, whiteSpace: "nowrap" }, children: [
          /* @__PURE__ */ (0, import_jsx_runtime.jsx)("span", { style: { fontSize: 16, lineHeight: 1 }, children: "\u2192" }),
          relation && /* @__PURE__ */ (0, import_jsx_runtime.jsx)("span", { style: { color: "#777", fontSize: 11 }, children: relation })
        ] })
      ] }, fn.id || `hop-${i}`);
    }) }),
    eliminated.length > 0 && /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", { style: { marginTop: 12 }, children: [
      /* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", { style: { fontSize: 13, fontWeight: 600, color: "#888", marginBottom: 4 }, children: "\u5DF2\u6392\u9664\u5019\u9009" }),
      eliminated.map((item, i) => {
        const linked = nodes.find((n) => String(n.node).toLowerCase() === String(item.candidate).toLowerCase());
        return /* @__PURE__ */ (0, import_jsx_runtime.jsxs)(
          "div",
          {
            style: { border: "1px dashed #bbb", background: "#fafafa", borderRadius: 8, padding: "6px 10px", marginBottom: 6 },
            children: [
              /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", { style: { display: "flex", alignItems: "center", gap: 6, flexWrap: "wrap" }, children: [
                /* @__PURE__ */ (0, import_jsx_runtime.jsx)("span", { style: { color: "#999", fontWeight: 600, fontSize: 13 }, children: item.candidate }),
                item.candidateType && /* @__PURE__ */ (0, import_jsx_runtime.jsx)("span", { style: { fontSize: 10, color: "#888", border: "1px solid #ccc", borderRadius: 10, padding: "0 6px", lineHeight: "16px", whiteSpace: "nowrap" }, children: item.candidateType }),
                item.strength !== void 0 && /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("span", { style: { fontSize: 11, color: "#999" }, children: [
                  "\u5F3A\u5EA6 ",
                  fmtPercent(item.strength)
                ] })
              ] }),
              item.reason && /* @__PURE__ */ (0, import_jsx_runtime.jsx)("div", { style: { fontSize: 12, color: "#aaa", marginTop: 2 }, children: item.reason }),
              linked && /* @__PURE__ */ (0, import_jsx_runtime.jsxs)("div", { style: { fontSize: 11, color: "#999", marginTop: 2 }, children: [
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

// src/DslRenderer.tsx
var import_jsx_runtime2 = require("react/jsx-runtime");
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
  const [, setVersion] = (0, import_react2.useState)(0);
  const storeRef = (0, import_react2.useRef)(store);
  storeRef.current = store;
  (0, import_react2.useEffect)(() => {
    const unsub = store.subscribe(() => setVersion((v) => v + 1));
    return unsub;
  }, [store]);
  (0, import_react2.useLayoutEffect)(() => {
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
  const values = (0, import_react2.useMemo)(() => {
    const out = {};
    const walk = (n) => {
      if (n.fieldName) out[n.fieldName] = store.get(`data.${n.fieldName}`);
      if (n.children) n.children.forEach(walk);
    };
    walk(root);
    return out;
  }, [root, store]);
  const dispatcher = (0, import_react2.useMemo)(() => new EventDispatcher(), []);
  const [validationErrors, setValidationErrors] = (0, import_react2.useState)({});
  const validateForm = (0, import_react2.useCallback)(() => {
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
  const handleEvent = (0, import_react2.useCallback)(
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
  const setField = (0, import_react2.useCallback)(
    (fieldName, value) => {
      store.set(`data.${fieldName}`, value);
      setValidationErrors((prev) => ({ ...prev, [fieldName]: [] }));
    },
    [store]
  );
  return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("div", { className: "cjdsl-root", style: pickStyle(root.style), "data-cjdsl-type": root.type, children: root.children && root.children.length > 0 ? root.children.map((child, i) => /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(
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
  )) : /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(DslNodeView, { node: root, store, values, validationErrors, setField, onEvent: handleEvent }) });
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
      return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(ContainerView, { node, store, values, validationErrors, setField, onEvent });
    case "textDisplay":
      return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(TextDisplayView, { node, store });
    case "table":
      return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(TableView, { node, store });
    case "alert":
      return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(AlertView, { node });
    case "chip":
      return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(ChipView, { node });
    case "badge":
      return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(BadgeView, { node });
    case "text":
    case "number":
    case "select":
    case "textarea":
    case "date":
    case "switch":
      return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(
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
      return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(ButtonView, { node, store, onEvent });
    case "chart":
      return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(ChartView, { node });
    case "flow":
      return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(FlowView, { node, store, onEvent });
    default:
      return /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("div", { style: { color: "#c62828", fontSize: 12, padding: "6px 10px", border: "1px dashed #ef9a9a", borderRadius: 4 }, children: [
        "\u672A\u652F\u6301\u7684\u7EC4\u4EF6\u7C7B\u578B\uFF1A",
        escAttr(node.type),
        "\uFF08DSL v1 \u767D\u540D\u5355\u5916\uFF09"
      ] });
  }
}
function ContainerView(props) {
  const { node, store, values, validationErrors, setField, onEvent } = props;
  if (node.type === "divider") {
    return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("hr", { style: { border: "none", borderTop: "1px solid rgba(0,0,0,0.12)", margin: "8px 0" } });
  }
  const children = node.children ?? [];
  const inner = children.map((child, i) => /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(DslNodeView, { node: child, store, values, validationErrors, setField, onEvent }, child.id || `c${i}`));
  const isForm = node.type === "form";
  return /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)(
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
        node.type === "grid" ? children.map((child, i) => /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("div", { style: { width: `${Math.min(Math.max(child.span ?? 12, 1), 12) * (100 / 12)}%`, display: "inline-block", verticalAlign: "top", padding: "0 4px", boxSizing: "border-box" }, children: /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(DslNodeView, { node: child, store, values, validationErrors, setField, onEvent }) }, child.id || `g${i}`)) : inner,
        isForm && node.props?.showFooter !== false && /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("div", { style: { marginTop: 10, textAlign: "right" }, children: node.props?.footerButtons?.map?.((btn, i) => /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(DslNodeView, { node: { ...btn, type: "button" }, store, values, validationErrors, setField, onEvent }, btn.id || `fb${i}`)) })
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
  return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("div", { style: { fontSize: size, color: typeof color === "string" ? color : void 0, margin: "4px 0", whiteSpace: "pre-wrap", wordBreak: "break-word" }, children: String(text ?? "") });
}
function TableView({ node, store }) {
  const columns = node.props?.columns ?? node.props?.Columns ?? [];
  const data = node.props?.items ?? node.props?.Items ?? node.props?.rows ?? node.props?.Rows ?? [];
  const finalData = data.length > 0 ? data : store.get(node.dataBind ?? "datasource.items") ?? [];
  if (finalData.length === 0) return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("div", { style: { color: "#999", padding: 6 }, children: "\uFF08\u65E0\u6570\u636E\uFF09" });
  const effectiveCols = columns.length > 0 ? columns : Object.keys(finalData[0] ?? {}).map((k) => ({ name: k, label: k }));
  const getValue = (row, col) => {
    const key = col.value ?? col.name ?? "";
    return row?.[key] ?? "";
  };
  return /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("table", { style: { width: "100%", borderCollapse: "collapse", fontSize: 13 }, children: [
    /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("thead", { children: /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("tr", { children: effectiveCols.map((c, i) => /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("th", { style: { borderBottom: "1px solid rgba(0,0,0,0.12)", padding: "6px 8px", textAlign: "left", fontWeight: 600 }, children: String(c.label ?? c.name ?? "") }, i)) }) }),
    /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("tbody", { children: finalData.map((row, ri) => /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("tr", { children: effectiveCols.map((c, ci) => /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("td", { style: { borderBottom: "1px solid rgba(0,0,0,0.06)", padding: "6px 8px" }, children: String(getValue(row, c) ?? "") }, ci)) }, ri)) })
  ] });
}
function AlertView({ node }) {
  const severity = node.props?.severity ?? node.props?.Severity ?? "info";
  const colorMap = { info: "#0277BD", success: "#2E7D32", warning: "#F57C00", error: "#C62828" };
  const bgMap = { info: "#E1F5FE", success: "#E8F5E9", warning: "#FFF3E0", error: "#FFEBEE" };
  return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("div", { style: { background: bgMap[severity] ?? bgMap.info, color: colorMap[severity] ?? colorMap.info, borderRadius: 6, padding: "8px 12px", fontSize: 13, margin: "6px 0" }, children: String(node.props?.text ?? node.props?.message ?? node.props?.content ?? node.label ?? "") });
}
function ChipView({ node }) {
  return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("span", { style: { display: "inline-block", background: "rgba(0,0,0,0.08)", borderRadius: 12, padding: "2px 10px", fontSize: 12, margin: "2px 4px 2px 0" }, children: String(node.props?.text ?? node.props?.label ?? node.label ?? "") });
}
function BadgeView({ node }) {
  const color = node.props?.color ?? node.props?.Color ?? "#1976D2";
  return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("span", { style: { display: "inline-block", background: String(color), color: "#fff", borderRadius: 10, padding: "1px 8px", fontSize: 11, margin: "0 4px 0 0" }, children: String(node.props?.text ?? node.props?.content ?? node.label ?? "") });
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
    return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("div", { style: { color: "#c62828", fontSize: 12 }, children: "\u8868\u5355\u7EC4\u4EF6\u7F3A\u5C11 fieldName" });
  }
  switch (node.type) {
    case "text":
    case "number":
      return /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("div", { style: baseStyle, children: [
        node.label && /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("label", { style: labelStyle, "data-kb-field": node.fieldName, children: [
          node.label,
          required && /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("span", { style: { color: "#c62828" }, children: " *" })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(
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
        errors.map((e, i) => /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("div", { style: errorStyle, children: e }, i)),
        node.helpText && /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("div", { style: helpStyle, children: node.helpText })
      ] });
    case "textarea":
      return /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("div", { style: baseStyle, children: [
        node.label && /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("label", { style: labelStyle, "data-kb-field": node.fieldName, children: [
          node.label,
          required && /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("span", { style: { color: "#c62828" }, children: " *" })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(
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
        errors.map((e, i) => /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("div", { style: errorStyle, children: e }, i)),
        node.helpText && /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("div", { style: helpStyle, children: node.helpText })
      ] });
    case "select":
      return /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("div", { style: baseStyle, children: [
        node.label && /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("label", { style: labelStyle, "data-kb-field": node.fieldName, children: [
          node.label,
          required && /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("span", { style: { color: "#c62828" }, children: " *" })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("select", { value: String(value ?? ""), disabled: disabledBase || submitted, style: inputStyle, onChange: (e) => setField(field, e.target.value), children: [
          /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("option", { value: "", children: "\u8BF7\u9009\u62E9" }),
          itemsOf(node).map((it, i) => /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("option", { value: it.value, disabled: it.disabled, children: it.label }, i))
        ] }),
        errors.map((e, i) => /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("div", { style: errorStyle, children: e }, i)),
        node.helpText && /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("div", { style: helpStyle, children: node.helpText })
      ] });
    case "date":
      return /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("div", { style: baseStyle, children: [
        node.label && /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("label", { style: labelStyle, "data-kb-field": node.fieldName, children: [
          node.label,
          required && /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("span", { style: { color: "#c62828" }, children: " *" })
        ] }),
        /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("input", { type: "date", value: String(value ?? ""), disabled: disabledBase, readOnly: submitted, style: inputStyle, onChange: (e) => setField(field, e.target.value) }),
        errors.map((e, i) => /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("div", { style: errorStyle, children: e }, i)),
        node.helpText && /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("div", { style: helpStyle, children: node.helpText })
      ] });
    case "switch":
      return /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("div", { style: baseStyle, children: [
        /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("label", { style: { display: "flex", alignItems: "center", gap: 8, fontSize: 14, cursor: disabledBase || submitted ? "not-allowed" : "pointer" }, "data-kb-field": node.fieldName, children: [
          /* @__PURE__ */ (0, import_jsx_runtime2.jsx)(
            "input",
            {
              type: "checkbox",
              checked: value === true || value === "true" || value === 1,
              disabled: disabledBase || submitted,
              onChange: (e) => setField(field, e.target.checked)
            }
          ),
          node.label,
          required && /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("span", { style: { color: "#c62828" }, children: " *" })
        ] }),
        errors.map((e, i) => /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("div", { style: errorStyle, children: e }, i)),
        node.helpText && /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("div", { style: helpStyle, children: node.helpText })
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
    return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("a", { href: String(href), ...common, children: node.type === "iconButton" ? node.props?.icon ?? "\u26A1" : label });
  }
  if (clickEv) {
    return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("button", { ...common, onClick: () => void onEvent(clickEv), children: node.type === "iconButton" ? node.props?.icon ?? "\u26A1" : label });
  }
  return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("button", { ...common, onClick: () => void onEvent({ type: "click", handler: "showToast", params: { message: "\u6309\u94AE\u672A\u914D\u7F6E\u4E8B\u4EF6", severity: "warning" } }), children: node.type === "iconButton" ? node.props?.icon ?? "\u26A1" : label });
}
function ChartView({ node }) {
  const chartType = node.props?.ChartType ?? node.props?.chartType ?? "donut";
  if (chartType !== "pie" && chartType !== "donut") {
    return /* @__PURE__ */ (0, import_jsx_runtime2.jsxs)("div", { style: { color: "#888", fontSize: 12, padding: 6 }, children: [
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
  return /* @__PURE__ */ (0, import_jsx_runtime2.jsx)("div", { style: { display: "flex", justifyContent: "center", margin: "10px 0" }, dangerouslySetInnerHTML: { __html: svg } });
}

// src/store.ts
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

// src/dslPayload.ts
var PAYLOAD_PREFIX = "CJDSL_PAYLOAD:";
function inferMode(root) {
  const t = root?.type;
  if (t === "form") return "form";
  return "card";
}
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
function detectDslPayloadInText(text) {
  if (typeof text !== "string" || text.trim() === "") return null;
  const idx = text.indexOf(PAYLOAD_PREFIX);
  if (idx >= 0) {
    const raw = text.slice(idx + PAYLOAD_PREFIX.length).trim();
    try {
      const payload = JSON.parse(raw);
      const r = payload?.render;
      const mode = r?.mode ?? payload?.mode ?? "card";
      const dsl = r?.dsl ?? payload?.dsl ?? null;
      if (dsl !== null && dsl !== void 0) {
        return { payload, mode, dsl };
      }
    } catch {
    }
  }
  const m = text.match(/```dsl\s*\n?([\s\S]*?)\n?```/);
  if (m) {
    const root = extractJsonSubstring(m[1]) ?? extractJsonSubstring(text);
    if (root && typeof root === "object") {
      return { payload: null, mode: inferMode(root), dsl: root };
    }
  }
  const bare = extractJsonSubstring(text);
  if (bare && typeof bare === "object" && !bare.components) {
    const rec = bare;
    if (typeof rec.type === "string") {
      return { payload: null, mode: inferMode(rec), dsl: rec };
    }
  }
  return null;
}
function extractBlockText(b) {
  if (!b || typeof b !== "object") return null;
  const rec = b;
  if (typeof rec.text === "string" && rec.text.trim() !== "") return rec.text;
  if (typeof rec.content === "string" && rec.content.trim() !== "") return rec.content;
  return null;
}
function detectDslPayload(blocks) {
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

// src/api.ts
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

// src/ChatDslNode.tsx
var import_react3 = require("react");
var import_jsx_runtime3 = require("react/jsx-runtime");
function stableStringify(value) {
  const normalize = (v) => {
    if (Array.isArray(v)) return v.map(normalize);
    if (v && typeof v === "object") {
      const out = {};
      for (const k of Object.keys(v).sort()) {
        const inner = v[k];
        if (inner !== void 0) out[k] = normalize(inner);
      }
      return out;
    }
    return v;
  };
  try {
    return JSON.stringify(normalize(value));
  } catch {
    return null;
  }
}
function codeBlockBodyText(blockEl) {
  const pre = blockEl.querySelector("pre");
  if (pre) return pre.textContent ?? "";
  const full = blockEl.textContent ?? "";
  const bannerText = blockEl.firstElementChild?.textContent ?? "";
  return bannerText && full.startsWith(bannerText) ? full.slice(bannerText.length) : full;
}
function parseAnyJson(text) {
  const t = text.trim();
  if (!t) return void 0;
  try {
    return JSON.parse(t);
  } catch {
  }
  const span = extractJsonSpan(t);
  if (span) return span.value;
  return detectDslPayloadInText(t)?.dsl;
}
function hideDslBlocksInRow(rowEl, targets) {
  rowEl.querySelectorAll(".md-code-block").forEach((blockEl) => {
    if (blockEl.dataset.cjdslHidden === "1" || blockEl.style.display === "none") return;
    const parsed = parseAnyJson(codeBlockBodyText(blockEl));
    if (parsed === void 0 || parsed === null || typeof parsed !== "object") return;
    const key2 = stableStringify(parsed);
    if (key2 && targets.has(key2)) {
      blockEl.style.display = "none";
      blockEl.dataset.cjdslHidden = "1";
    }
  });
  const rowText = (rowEl.textContent ?? "").trim();
  if (!rowText) return;
  const span = extractJsonSpan(rowText);
  if (!span) return;
  const key = stableStringify(span.value);
  if (!key || !targets.has(key)) return;
  const remainder = (rowText.slice(0, span.start) + rowText.slice(span.end + 1)).replace(/\s+/g, "");
  if (remainder.length <= 2) {
    rowEl.style.display = "none";
    rowEl.dataset.cjdslHidden = "1";
  }
}
function hideAdjacentDslSources(cardEl, targets) {
  if (targets.size === 0) return;
  const row = cardEl.closest("[data-chat-flow-kind]");
  if (!row) return;
  const scan = (from, dir, budget) => {
    let cur = from;
    let n = 0;
    while (cur && n < budget) {
      const kind = cur.dataset?.chatFlowKind;
      if (kind === "assistant-step") {
        hideDslBlocksInRow(cur, targets);
        n++;
      }
      cur = dir === "prev" ? cur.previousElementSibling : cur.nextElementSibling;
    }
  };
  scan(row.previousElementSibling, "prev", 4);
  scan(row.nextElementSibling, "next", 2);
}
function ChatDslNode(props) {
  const data = props.node?.data ?? props.data ?? {};
  const api = props.api;
  const [toast, setToast] = (0, import_react3.useState)(null);
  const dslNode = (0, import_react3.useMemo)(() => {
    if (data.dsl !== void 0 && data.dsl !== null) return toDslNode(data.dsl);
    if (typeof data.rawText === "string" && data.rawText.trim() !== "") {
      const det = detectDslPayloadInText(data.rawText);
      if (det && det.dsl !== void 0 && det.dsl !== null) return toDslNode(det.dsl);
    }
    return null;
  }, [data.dsl, data.rawText]);
  const storeRef = (0, import_react3.useMemo)(() => new DslStore(), []);
  const mode = data.mode ?? "card";
  const cardRef = (0, import_react3.useRef)(null);
  const hideTargets = (0, import_react3.useMemo)(() => {
    const set = /* @__PURE__ */ new Set();
    const add = (v) => {
      if (!v || typeof v !== "object") return;
      const key = stableStringify(v);
      if (key) set.add(key);
    };
    add(data.dsl);
    add(data.payload);
    add(dslNode);
    if (typeof data.rawText === "string" && data.rawText.trim() !== "") {
      const span = extractJsonSpan(data.rawText);
      if (span) add(span.value);
    }
    return set;
  }, [data.dsl, data.payload, data.rawText, dslNode]);
  (0, import_react3.useEffect)(() => {
    const el = cardRef.current;
    if (!dslNode || !el || hideTargets.size === 0) return;
    let timer = null;
    const run = () => hideAdjacentDslSources(el, hideTargets);
    run();
    const scope = el.closest("[data-conversation-scroll]") ?? el.ownerDocument?.body;
    if (!scope) return;
    const observer = new MutationObserver(() => {
      if (timer) clearTimeout(timer);
      timer = setTimeout(run, 160);
    });
    observer.observe(scope, { childList: true, subtree: true });
    return () => {
      observer.disconnect();
      if (timer) clearTimeout(timer);
    };
  }, [dslNode, hideTargets]);
  const showToast = (message, severity = "info") => {
    setToast({ message, severity });
    setTimeout(() => setToast(null), 3500);
  };
  if (!dslNode) {
    const hasRaw = data.dsl !== void 0 && data.dsl !== null;
    return /* @__PURE__ */ (0, import_jsx_runtime3.jsxs)("div", { style: { border: "1px dashed #e0c46a", borderRadius: 8, padding: "10px 12px", background: "#FFFDE7", fontSize: 13, color: "#8a6d00" }, children: [
      /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("b", { children: "CJDSL" }),
      "\uFF1A",
      hasRaw ? "\u68C0\u6D4B\u5230 DSL \u8F7D\u8377\u4F46\u89E3\u6790\u5931\u8D25\uFF0C\u5DF2\u4FDD\u7559\u539F\u6587\u3002" : "\u672A\u68C0\u6D4B\u5230\u53EF\u6E32\u67D3\u7684 DSL \u8F7D\u8377\u3002"
    ] });
  }
  return /* @__PURE__ */ (0, import_jsx_runtime3.jsxs)("div", { "data-cjdsl-chat-node": "true", ref: cardRef, style: { border: "1px solid rgba(0,0,0,0.12)", borderRadius: 10, overflow: "hidden", margin: "4px 0", background: "#fff" }, children: [
    /* @__PURE__ */ (0, import_jsx_runtime3.jsxs)("div", { style: { display: "flex", alignItems: "center", gap: 8, padding: "6px 12px", background: "#F5F7FA", borderBottom: "1px solid rgba(0,0,0,0.08)", fontSize: 12, color: "#666" }, children: [
      /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("span", { style: { fontWeight: 600, color: "#1976D2" }, children: "CJDSL" }),
      /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("span", { style: { background: "#E3F2FD", color: "#1565C0", borderRadius: 10, padding: "0 8px" }, children: mode }),
      /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("span", { style: { color: "#999" }, children: "\u5168\u5C40\u6E32\u67D3" })
    ] }),
    /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("div", { style: { padding: 10 }, children: /* @__PURE__ */ (0, import_jsx_runtime3.jsx)(
      DslRenderer,
      {
        root: dslNode,
        store: storeRef,
        callbacks: {
          onSubmit: async (submitCtx) => {
            if (!api) return { ok: false, message: "\u672A\u6CE8\u5165 api \u5BA2\u6237\u7AEF" };
            try {
              const res = await api.submit({ action: submitCtx.action, formId: submitCtx.formId, values: submitCtx.values });
              showToast(res?.message || `\u5DF2\u63D0\u4EA4 ${submitCtx.action}`, "success");
              return { ok: true, message: res?.message };
            } catch (e) {
              showToast(e.message, "error");
              return { ok: false, message: e.message };
            }
          },
          onToast: (message, severity) => showToast(message, severity ?? "info"),
          onNavigate: (path) => {
            if (path.startsWith("http")) window.open(path, "_blank");
            else window.location.href = path;
          }
        }
      }
    ) }),
    toast && /* @__PURE__ */ (0, import_jsx_runtime3.jsx)("div", { style: { padding: "6px 12px", fontSize: 12, color: toast.severity === "error" ? "#C62828" : toast.severity === "success" ? "#2E7D32" : "#0277BD", background: toast.severity === "error" ? "#FFEBEE" : toast.severity === "success" ? "#E8F5E9" : "#E1F5FE", borderTop: "1px solid rgba(0,0,0,0.06)" }, children: toast.message })
  ] });
}

// src/ToolCard.tsx
var import_react4 = require("react");
var import_jsx_runtime4 = require("react/jsx-runtime");
function CjdslToolCard(props) {
  const { owner, api = defaultApiClient } = props;
  const payload = (0, import_react4.useMemo)(() => {
    const block = props.block ?? owner?.block ?? owner;
    const content = block?.content;
    if (Array.isArray(content)) {
      for (const item of content) {
        if (!item || typeof item !== "object") continue;
        const text = typeof item.text === "string" ? item.text : typeof item.content === "string" ? item.content : null;
        if (text === null) continue;
        const idx = text.indexOf("CJDSL_PAYLOAD:");
        if (idx >= 0) {
          try {
            return JSON.parse(text.slice(idx + "CJDSL_PAYLOAD:".length).trim());
          } catch {
            return null;
          }
        }
      }
    }
    return null;
  }, [owner, props.block]);
  const [toast, setToast] = (0, import_react4.useState)(null);
  const dslNode = (0, import_react4.useMemo)(() => payload && payload.ok ? toDslNode(payload.render?.dsl) : null, [payload]);
  const storeRef = (0, import_react4.useMemo)(() => new DslStore(), []);
  const showToast = (message, severity = "info") => {
    setToast({ message, severity });
    setTimeout(() => setToast(null), 3500);
  };
  if (!payload) return null;
  if (!payload.ok) {
    return /* @__PURE__ */ (0, import_jsx_runtime4.jsxs)("div", { style: { border: "1px dashed #e0c46a", borderRadius: 8, padding: "10px 12px", background: "#FFFDE7", fontSize: 13, color: "#8a6d00" }, children: [
      /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("b", { children: "CJDSL" }),
      "\uFF1A",
      String(payload.message ?? "\u6E32\u67D3\u5931\u8D25")
    ] });
  }
  if (!dslNode) {
    return /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("div", { style: { border: "1px dashed #ef9a9a", borderRadius: 8, padding: "10px 12px", color: "#c62828", fontSize: 13 }, children: "render.dsl \u89E3\u6790\u5931\u8D25\u6216\u4E3A\u7A7A" });
  }
  const mode = payload.render?.mode ?? "card";
  return /* @__PURE__ */ (0, import_jsx_runtime4.jsxs)("div", { style: { border: "1px solid rgba(0,0,0,0.12)", borderRadius: 10, overflow: "hidden", margin: "4px 0", background: "#fff" }, children: [
    /* @__PURE__ */ (0, import_jsx_runtime4.jsxs)("div", { style: { display: "flex", alignItems: "center", gap: 8, padding: "6px 12px", background: "#F5F7FA", borderBottom: "1px solid rgba(0,0,0,0.08)", fontSize: 12, color: "#666" }, children: [
      /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("span", { style: { fontWeight: 600, color: "#1976D2" }, children: "CJDSL" }),
      /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("span", { style: { background: "#E3F2FD", color: "#1565C0", borderRadius: 10, padding: "0 8px" }, children: mode }),
      payload.generated && /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("span", { style: { background: "#F3E5F5", color: "#7B1FA2", borderRadius: 10, padding: "0 8px" }, children: "intent \u751F\u6210" })
    ] }),
    /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("div", { style: { padding: 10 }, children: /* @__PURE__ */ (0, import_jsx_runtime4.jsx)(
      DslRenderer,
      {
        root: dslNode,
        store: storeRef,
        callbacks: {
          onSubmit: async (submitCtx) => {
            try {
              const res = await api.submit({ action: submitCtx.action, formId: submitCtx.formId, values: submitCtx.values });
              showToast(res?.message || `\u5DF2\u63D0\u4EA4 ${submitCtx.action}`, "success");
              return { ok: true, message: res?.message };
            } catch (e) {
              showToast(e.message, "error");
              return { ok: false, message: e.message };
            }
          },
          onToast: (message, severity) => showToast(message, severity ?? "info"),
          onNavigate: (path) => {
            if (path.startsWith("http")) window.open(path, "_blank");
            else window.location.href = path;
          }
        }
      }
    ) }),
    toast && /* @__PURE__ */ (0, import_jsx_runtime4.jsx)("div", { style: { padding: "6px 12px", fontSize: 12, color: toast.severity === "error" ? "#C62828" : toast.severity === "success" ? "#2E7D32" : "#0277BD", background: toast.severity === "error" ? "#FFEBEE" : toast.severity === "success" ? "#E8F5E9" : "#E1F5FE", borderTop: "1px solid rgba(0,0,0,0.06)" }, children: toast.message })
  ] });
}

// src/client-entry.tsx
var import_jsx_runtime5 = require("react/jsx-runtime");
var inject = ["slots", "locale", "conversationEvents"];
var cjdslPayloadDefinition = {
  kind: "cjdsl",
  target: "chat",
  match: (event) => {
    if (!event) return null;
    const isAssistant = event.type === "assistant/message";
    const isPluginInjected = event.type === "user/message" && event.data?.source?.kind === "plugin";
    if (!isAssistant && !isPluginInjected) return null;
    const content = event.data?.message?.content ?? event.data?.content;
    if (!Array.isArray(content)) return null;
    if (!detectDslPayload(content)) return null;
    const id = `${event.data?.turn ?? 0}:${event.data?.step ?? 0}:${String(event.data?.message?.id ?? event.seq)}`;
    return { id, role: "start" };
  },
  start: (_context, match) => {
    const content = match.event?.data?.message?.content ?? match.event?.data?.content;
    const rawText = Array.isArray(content) ? content.map(
      (b) => typeof b?.text === "string" ? b.text : typeof b?.content === "string" ? b.content : typeof b === "string" ? b : ""
    ).filter(Boolean).join("\n") : typeof content === "string" ? content : "";
    const det = detectDslPayload(content) ?? (rawText ? detectDslPayloadInText(rawText) : null);
    if (!det) return { payload: null, dsl: null, mode: "card", rawText };
    return { payload: det.payload, dsl: det.dsl, mode: det.mode, rawText };
  },
  update: (context) => context.state,
  buildViewNode: (context) => {
    if (context.state === void 0) return null;
    return {
      key: context.key,
      kind: "cjdsl",
      id: context.id,
      target: "chat",
      anchorSeq: context.start?.event?.seq ?? context.matches?.[0]?.event?.seq ?? 0,
      location: context.start?.location ?? { kind: "unresolved" },
      visibility: "visible",
      data: context.state
    };
  }
};
function apply(ctx) {
  console.log("[cjdsl-react] apply entered");
  try {
    const slots = ctx.slots;
    if (!slots) {
      console.log("[cjdsl-react] slots unavailable, skip");
      return;
    }
    slots.inject(
      "tool.call.toolview",
      () => slots.register(
        { name: "tool.call.toolview", key: "cjdsl_render", id: "cjdsl_render", label: "CJDSL" },
        CjdslToolCard
      )
    );
    slots.inject(
      "conversation.chat.node",
      () => slots.register(
        { name: "conversation.chat.node", key: "cjdsl", id: "cjdsl", label: "CJDSL" },
        (props) => /* @__PURE__ */ (0, import_jsx_runtime5.jsx)(ChatDslNode, { ...props, api: defaultApiClient })
      )
    );
    const ce = ctx.conversationEvents;
    if (ce && typeof ce.register === "function") {
      ce.register(cjdslPayloadDefinition);
      console.log("[cjdsl-react] conversationEvents Definition registered (kind=cjdsl)");
    } else {
      console.log("[cjdsl-react] conversationEvents unavailable, global DSL node skipped");
    }
  } catch (e) {
    console.log(`[cjdsl-react] apply failed: ${e.message}`);
  }
}
return module.exports; } });
