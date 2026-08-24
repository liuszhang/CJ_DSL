// ChatDslNode.tsx — conversation.chat.node 渲染器（key=cjdsl，P0 全局渲染）
//   从 node.data 取 { payload, dsl, mode, rawText }，复用 DslRenderer 渲染 CJDSL 卡片。
//   后端依赖通过 callbacks 注入（不绑定具体 HTTP 端点）。
import React, { useEffect, useMemo, useRef, useState } from "react";
import { DslRenderer } from "./DslRenderer";
import { DslStore } from "./store";
import { toDslNode, detectDslPayloadInText, extractJsonSpan } from "./dslPayload";
import type { CjdslApiClient } from "./api";

// ---------------- 源 DSL 隐藏 ----------------
// 卡片成功渲染后，把相邻 assistant 行中与已渲染载荷「内容等价」的源码
// （```dsl 围栏 / 裸 JSON 代码块）隐藏，避免图表与源码重复展示。
// 采用规范化 JSON 深比对，只隐藏与当前载荷完全一致的内容，不误伤其他代码块。

/** 键排序的规范化 JSON（跳过 undefined），用于内容等价判定。导出以便测试。 */
export function stableStringify(value: unknown): string | null {
  const normalize = (v: unknown): unknown => {
    if (Array.isArray(v)) return v.map(normalize);
    if (v && typeof v === "object") {
      const out: Record<string, unknown> = {};
      for (const k of Object.keys(v as Record<string, unknown>).sort()) {
        const inner = (v as Record<string, unknown>)[k];
        if (inner !== undefined) out[k] = normalize(inner);
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

/** 取代码块正文文本：优先 <pre>（未高亮路径）；shiki 高亮路径无 <pre>，则整体文本去掉首个子元素（语言横幅+复制按钮）。 */
function codeBlockBodyText(blockEl: Element): string {
  const pre = blockEl.querySelector("pre");
  if (pre) return pre.textContent ?? "";
  const full = blockEl.textContent ?? "";
  const bannerText = blockEl.firstElementChild?.textContent ?? "";
  return bannerText && full.startsWith(bannerText) ? full.slice(bannerText.length) : full;
}

function parseAnyJson(text: string): unknown | undefined {
  const t = text.trim();
  if (!t) return undefined;
  try {
    return JSON.parse(t);
  } catch {
    // 整段解析失败，继续子串提取
  }
  const span = extractJsonSpan(t);
  if (span) return span.value;
  return detectDslPayloadInText(t)?.dsl;
}

/** 在指定行内隐藏与目标载荷等价的代码块；若整行去掉该 JSON 后只剩少量噪声则整行隐藏。 */
function hideDslBlocksInRow(rowEl: HTMLElement, targets: Set<string>): void {
  rowEl.querySelectorAll<HTMLElement>(".md-code-block").forEach((blockEl) => {
    if (blockEl.dataset.cjdslHidden === "1" || blockEl.style.display === "none") return;
    const parsed = parseAnyJson(codeBlockBodyText(blockEl));
    if (parsed === undefined || parsed === null || typeof parsed !== "object") return;
    const key = stableStringify(parsed);
    if (key && targets.has(key)) {
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

/** 从卡片所在行出发，向上/下扫描相邻 assistant 行并隐藏其中的源 DSL。导出以便测试。 */
export function hideAdjacentDslSources(cardEl: HTMLElement, targets: Set<string>): void {
  if (targets.size === 0) return;
  const row = cardEl.closest<HTMLElement>("[data-chat-flow-kind]");
  if (!row) return; // 非 DSH 宿主无行结构，不做 DOM 干预
  const scan = (from: Element | null, dir: "prev" | "next", budget: number) => {
    let cur = from;
    let n = 0;
    while (cur && n < budget) {
      const kind = (cur as HTMLElement).dataset?.chatFlowKind;
      if (kind === "assistant-step") {
        hideDslBlocksInRow(cur as HTMLElement, targets);
        n++;
      }
      cur = dir === "prev" ? cur.previousElementSibling : cur.nextElementSibling;
    }
  };
  scan(row.previousElementSibling, "prev", 4);
  scan(row.nextElementSibling, "next", 2);
}
// ---------------- 源 DSL 隐藏（完） ----------------

interface ChatDslNodeProps {
  data?: { dsl?: unknown; mode?: string; payload?: any; rawText?: string };
  /** DSH 契约：ConversationNode 整体经 props.node 传入，业务 state 位于 node.data */
  node?: { data?: { dsl?: unknown; mode?: string; payload?: any; rawText?: string }; [key: string]: any };
  api?: CjdslApiClient;
  [key: string]: any;
}

/**
 * 全局 DSL 聊天节点渲染器。宿主若要接入 DSH 的 conversation.chat.node，
 * 直接用本组件注册 key=cjdsl 即可；api 默认使用同源 HttpCjdslApiClient，可注入自定义实现。
 */
export function ChatDslNode(props: ChatDslNodeProps) {
  // DSH conversation.chat.node 契约：整个 ConversationNode 作为 props.node 传入，
  // 业务 state 位于 node.data；部分宿主则直接传 props.data。两者兼容。
  const data = props.node?.data ?? props.data ?? {};
  const api = props.api;
  const [toast, setToast] = useState<{ message: string; severity: string } | null>(null);

  // 自愈：上游 conversationEvents 链路若未把 dsl 透传下来（data.dsl 空），
  // 但存留了原始文本 rawText，则本地重新检测一次，避免误报「未检测到」。
  const dslNode = useMemo(() => {
    if (data.dsl !== undefined && data.dsl !== null) return toDslNode(data.dsl);
    if (typeof data.rawText === "string" && data.rawText.trim() !== "") {
      const det = detectDslPayloadInText(data.rawText);
      if (det && det.dsl !== undefined && det.dsl !== null) return toDslNode(det.dsl);
    }
    return null;
  }, [data.dsl, data.rawText]);
  const storeRef = useMemo(() => new DslStore(), []);
  const mode = data.mode ?? "card";

  const cardRef = useRef<HTMLDivElement | null>(null);

  // 与已渲染载荷「内容等价」的规范化 JSON 集合，用于精确隐藏源码、不误伤其他代码块
  const hideTargets = useMemo(() => {
    const set = new Set<string>();
    const add = (v: unknown) => {
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

  // 渲染成功后隐藏相邻 assistant 行中的源 DSL（观察器用于流式补齐后自愈）
  useEffect(() => {
    const el = cardRef.current;
    if (!dslNode || !el || hideTargets.size === 0) return;
    let timer: ReturnType<typeof setTimeout> | null = null;
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

  const showToast = (message: string, severity = "info") => {
    setToast({ message, severity });
    setTimeout(() => setToast(null), 3500);
  };

  if (!dslNode) {
    const hasRaw = data.dsl !== undefined && data.dsl !== null;
    return (
      <div style={{ border: "1px dashed #e0c46a", borderRadius: 8, padding: "10px 12px", background: "#FFFDE7", fontSize: 13, color: "#8a6d00" }}>
        <b>CJDSL</b>：{hasRaw ? "检测到 DSL 载荷但解析失败，已保留原文。" : "未检测到可渲染的 DSL 载荷。"}
      </div>
    );
  }

  return (
    <div data-cjdsl-chat-node="true" ref={cardRef} style={{ border: "1px solid rgba(0,0,0,0.12)", borderRadius: 10, overflow: "hidden", margin: "4px 0", background: "#fff" }}>
      <div style={{ display: "flex", alignItems: "center", gap: 8, padding: "6px 12px", background: "#F5F7FA", borderBottom: "1px solid rgba(0,0,0,0.08)", fontSize: 12, color: "#666" }}>
        <span style={{ fontWeight: 600, color: "#1976D2" }}>CJDSL</span>
        <span style={{ background: "#E3F2FD", color: "#1565C0", borderRadius: 10, padding: "0 8px" }}>{mode}</span>
        <span style={{ color: "#999" }}>全局渲染</span>
      </div>
      <div style={{ padding: 10 }}>
        <DslRenderer
          root={dslNode}
          store={storeRef}
          callbacks={{
            onSubmit: async (submitCtx) => {
              if (!api) return { ok: false, message: "未注入 api 客户端" };
              try {
                const res = await api.submit({ action: submitCtx.action, formId: submitCtx.formId, values: submitCtx.values });
                showToast(res?.message || `已提交 ${submitCtx.action}`, "success");
                return { ok: true, message: res?.message };
              } catch (e) {
                showToast((e as Error).message, "error");
                return { ok: false, message: (e as Error).message };
              }
            },
            onToast: (message, severity) => showToast(message, severity ?? "info"),
            onNavigate: (path) => {
              if (path.startsWith("http")) window.open(path, "_blank");
              else window.location.href = path;
            },
          }}
        />
      </div>
      {toast && (
        <div style={{ padding: "6px 12px", fontSize: 12, color: toast.severity === "error" ? "#C62828" : toast.severity === "success" ? "#2E7D32" : "#0277BD", background: toast.severity === "error" ? "#FFEBEE" : toast.severity === "success" ? "#E8F5E9" : "#E1F5FE", borderTop: "1px solid rgba(0,0,0,0.06)" }}>
          {toast.message}
        </div>
      )}
    </div>
  );
}
