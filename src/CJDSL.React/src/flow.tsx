// flow 渲染组件（对齐《CJDSL三端Flow渲染器接口定义方案》4.2）
//   - 数据兜底：props.nodes 为空时尝试 store.get(node.dataBind ?? "datasource.items")
//   - 布局：horizontal（横向 hop 链 + 关系标签）/ vertical（纵向）
//   - 节点卡片：node 编码 + type 徽标 + note（截断 ≤30 字）+ 证据强度/路径置信度百分比色阶
//   - eliminated：灰化虚线分组；candidate 可匹配主链节点码时虚线关联对应 hop
//   - 高亮：highlightOnClick=true 时点击置选中态（再次点击取消）
//   - 事件：优先 node.events（showToast/navigate/setValue/chain），未配置默认 showToast 展示 note；
//           事件参数固定 nodeId / hop / instanceId / relation
//   - 安全对齐 DslRenderer：文本经 React 转义（JSX 默认），style 仅白名单键，不 spread 未知 props
import React, { useMemo, useState } from "react";
import { DslStore } from "./store";
import type { DslNode } from "./DslRenderer";
import type { DslEvent } from "./events";
import type { FlowEliminatedBranch, FlowEdge, FlowNode, FlowProps } from "./flow";

const FLOW_STYLE_KEYS = new Set(["class", "color", "backgroundColor", "margin", "padding", "width", "height"]);

function pickFlowStyle(style?: Record<string, any>): React.CSSProperties | undefined {
  if (!style || typeof style !== "object") return undefined;
  const out: React.CSSProperties = {};
  for (const [k, v] of Object.entries(style)) {
    if (FLOW_STYLE_KEYS.has(k) && (typeof v === "string" || typeof v === "number")) {
      (out as Record<string, any>)[k] = v;
    }
  }
  return Object.keys(out).length > 0 ? out : undefined;
}

function truncateNote(note?: string, max = 30): string {
  if (!note) return "";
  return note.length <= max ? note : `${note.slice(0, max)}…`;
}

function fmtPercent(value?: number): string {
  if (value === undefined || value === null || Number.isNaN(value)) return "—";
  return `${Math.round(value * 100)}%`;
}

/** 证据强度色阶：≥0.7 绿 / 0.4-0.7 橙 / <0.4 红 */
function strengthColor(value?: number): string {
  if (value === undefined || value === null || Number.isNaN(value)) return "#9e9e9e";
  return value >= 0.7 ? "#4caf50" : value >= 0.4 ? "#ff9800" : "#f44336";
}

function toArray<T>(raw: unknown): T[] {
  if (Array.isArray(raw)) return raw as T[];
  return [];
}

export function FlowView({ node, store, onEvent }: { node: DslNode; store: DslStore; onEvent: (ev: DslEvent) => Promise<void> }) {
  const props = (node.props ?? {}) as FlowProps;
  const [selectedId, setSelectedId] = useState<string | null>(null);

  const nodes: FlowNode[] = useMemo(() => {
    const direct = toArray<FlowNode>(props.nodes);
    if (direct.length > 0) return direct;
    const bound = store.get(node.dataBind ?? "datasource.items");
    return toArray<FlowNode>(bound);
    // store 变化由外层 DslRenderer 订阅触发重渲染，此处无需手动订阅
  }, [props.nodes, node.dataBind, store]);

  const edges: FlowEdge[] = useMemo(() => toArray<FlowEdge>(props.edges), [props.edges]);
  const eliminated: FlowEliminatedBranch[] = useMemo(() => toArray<FlowEliminatedBranch>(props.eliminated), [props.eliminated]);
  const highlightOnClick = props.highlightOnClick === true;
  const vertical = props.layout === "vertical";
  const interactive = highlightOnClick || (node.events ?? []).some((e) => e.type === "click" || e.type === "onClick");

  if (nodes.length === 0) {
    return <div style={{ color: "#999", fontSize: 12, padding: 6 }}>（无溯源路径数据）</div>;
  }

  const relationOf = (source: string, target: string): string => {
    const edge = edges.find((e) => e.source === source && e.target === target);
    return edge?.relation ?? "";
  };

  const handleNodeClick = (fn: FlowNode): void => {
    if (highlightOnClick) {
      setSelectedId((prev) => (prev === fn.id ? null : fn.id));
    }

    const clickEv = (node.events ?? []).find((e) => e.type === "click" || e.type === "onClick");
    const relation = edges.find((e) => e.source === fn.id)?.relation ?? "";
    const baseParams: Record<string, any> = {
      nodeId: fn.id,
      hop: fn.hop,
      instanceId: fn.instanceId ?? "",
      relation,
    };

    if (clickEv) {
      void onEvent({
        ...clickEv,
        params: { ...(clickEv.params ?? {}), ...baseParams },
      });
    } else {
      void onEvent({
        type: "click",
        handler: "showToast",
        params: { message: truncateNote(fn.note) || fn.node, severity: "info" },
      });
    }
  };

  const nodeCardStyle = (fn: FlowNode): React.CSSProperties => {
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
      boxSizing: "border-box",
    };
  };

  const chainStyle: React.CSSProperties = vertical
    ? { display: "flex", flexDirection: "column", alignItems: "stretch", gap: 12 }
    : { display: "flex", flexDirection: "row", alignItems: "stretch", gap: 4, overflowX: "auto", padding: 4 };

  return (
    <div className="cjdsl-flow" style={pickFlowStyle(node.style)} data-cjdsl-id={node.id}>
      {node.label && (
        <div style={{ fontSize: 15, fontWeight: 600, margin: "4px 0 8px" }}>{node.label}</div>
      )}

      <div style={chainStyle}>
        {nodes.map((fn, i) => {
          const next = i < nodes.length - 1 ? nodes[i + 1] : undefined;
          const relation = next ? relationOf(fn.id, next.id) : "";
          return (
            <div key={fn.id || `hop-${i}`} style={{ display: "flex", flexDirection: vertical ? "column" : "row", alignItems: "center", gap: 8, flex: "0 0 auto" }}>
              <div style={nodeCardStyle(fn)} onClick={() => handleNodeClick(fn)}>
                <div style={{ display: "flex", alignItems: "center", justifyContent: "space-between", gap: 6 }}>
                  <span style={{ fontWeight: 600, fontSize: 13, wordBreak: "break-all" }}>{fn.node}</span>
                  {fn.type && (
                    <span style={{ fontSize: 10, color: "#1565c0", border: "1px solid #90caf9", borderRadius: 10, padding: "0 6px", lineHeight: "16px", whiteSpace: "nowrap" }}>
                      {fn.type}
                    </span>
                  )}
                </div>
                {truncateNote(fn.note) && (
                  <div style={{ fontSize: 12, color: "#666", marginTop: 4, wordBreak: "break-word" }}>{truncateNote(fn.note)}</div>
                )}
                <div style={{ display: "flex", justifyContent: "space-between", marginTop: 6, fontSize: 11 }}>
                  <span style={{ color: strengthColor(fn.evidenceStrength), fontWeight: 600 }}>证据 {fmtPercent(fn.evidenceStrength)}</span>
                  <span style={{ color: "#777" }}>置信 {fmtPercent(fn.pathConfidence)}</span>
                </div>
              </div>
              {next && (
                <div style={{ display: "flex", flexDirection: "column", alignItems: "center", color: "#999", fontSize: 12, whiteSpace: "nowrap" }}>
                  <span style={{ fontSize: 16, lineHeight: 1 }}>→</span>
                  {relation && <span style={{ color: "#777", fontSize: 11 }}>{relation}</span>}
                </div>
              )}
            </div>
          );
        })}
      </div>

      {eliminated.length > 0 && (
        <div style={{ marginTop: 12 }}>
          <div style={{ fontSize: 13, fontWeight: 600, color: "#888", marginBottom: 4 }}>已排除候选</div>
          {eliminated.map((item, i) => {
            const linked = nodes.find((n) => String(n.node).toLowerCase() === String(item.candidate).toLowerCase());
            return (
              <div
                key={`${item.candidate}-${i}`}
                style={{ border: "1px dashed #bbb", background: "#fafafa", borderRadius: 8, padding: "6px 10px", marginBottom: 6 }}
              >
                <div style={{ display: "flex", alignItems: "center", gap: 6, flexWrap: "wrap" }}>
                  <span style={{ color: "#999", fontWeight: 600, fontSize: 13 }}>{item.candidate}</span>
                  {item.candidateType && (
                    <span style={{ fontSize: 10, color: "#888", border: "1px solid #ccc", borderRadius: 10, padding: "0 6px", lineHeight: "16px", whiteSpace: "nowrap" }}>
                      {item.candidateType}
                    </span>
                  )}
                  {item.strength !== undefined && <span style={{ fontSize: 11, color: "#999" }}>强度 {fmtPercent(item.strength)}</span>}
                </div>
                {item.reason && <div style={{ fontSize: 12, color: "#aaa", marginTop: 2 }}>{item.reason}</div>}
                {linked && <div style={{ fontSize: 11, color: "#999", marginTop: 2 }}>虚线关联：{linked.node}（hop-{linked.hop}）</div>}
              </div>
            );
          })}
        </div>
      )}
    </div>
  );
}

export default FlowView;
