// 工具卡片（tool.call.toolview key=cjdsl_render）：
// 从工具结果 content 中提取 CJDSL_PAYLOAD 载荷，复用 DslRenderer 渲染。
// 后端依赖通过 api 注入（默认同源 HttpCjdslApiClient）。
import React, { useMemo, useState } from "react";
import { DslRenderer } from "./DslRenderer";
import { DslStore } from "./store";
import { toDslNode } from "./dslPayload";
import type { CjdslApiClient } from "./api";
import { defaultApiClient } from "./api";

export interface ToolCardProps {
  owner?: any;
  /** DSH tool.call.toolview 契约：owner 成员平铺为 props，工具结果块经 props.block 传入 */
  block?: any;
  api?: CjdslApiClient;
  [key: string]: any;
}

export function CjdslToolCard(props: ToolCardProps) {
  const { owner, api = defaultApiClient } = props;
  const payload = useMemo(() => {
    // DSH 契约：props.block 直达；兼容旧契约 owner.block / owner。
    const block = props.block ?? owner?.block ?? owner;
    const content = block?.content;
    if (Array.isArray(content)) {
      for (const item of content) {
        if (!item || typeof item !== "object") continue;
        const text =
          typeof item.text === "string"
            ? item.text
            : typeof item.content === "string"
              ? item.content
              : null;
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

  const [toast, setToast] = useState<{ message: string; severity: string } | null>(null);
  const dslNode = useMemo(() => (payload && payload.ok ? toDslNode(payload.render?.dsl) : null), [payload]);
  const storeRef = useMemo(() => new DslStore(), []);

  const showToast = (message: string, severity = "info") => {
    setToast({ message, severity });
    setTimeout(() => setToast(null), 3500);
  };

  if (!payload) return null;
  if (!payload.ok) {
    return (
      <div style={{ border: "1px dashed #e0c46a", borderRadius: 8, padding: "10px 12px", background: "#FFFDE7", fontSize: 13, color: "#8a6d00" }}>
        <b>CJDSL</b>：{String(payload.message ?? "渲染失败")}
      </div>
    );
  }
  if (!dslNode) {
    return <div style={{ border: "1px dashed #ef9a9a", borderRadius: 8, padding: "10px 12px", color: "#c62828", fontSize: 13 }}>render.dsl 解析失败或为空</div>;
  }

  const mode = payload.render?.mode ?? "card";
  return (
    <div style={{ border: "1px solid rgba(0,0,0,0.12)", borderRadius: 10, overflow: "hidden", margin: "4px 0", background: "#fff" }}>
      <div style={{ display: "flex", alignItems: "center", gap: 8, padding: "6px 12px", background: "#F5F7FA", borderBottom: "1px solid rgba(0,0,0,0.08)", fontSize: 12, color: "#666" }}>
        <span style={{ fontWeight: 600, color: "#1976D2" }}>CJDSL</span>
        <span style={{ background: "#E3F2FD", color: "#1565C0", borderRadius: 10, padding: "0 8px" }}>{mode}</span>
        {payload.generated && <span style={{ background: "#F3E5F5", color: "#7B1FA2", borderRadius: 10, padding: "0 8px" }}>intent 生成</span>}
      </div>
      <div style={{ padding: 10 }}>
        <DslRenderer
          root={dslNode}
          store={storeRef}
          callbacks={{
            onSubmit: async (submitCtx) => {
              try {
                const res = await api.submit({ action: submitCtx.action, formId: submitCtx.formId,  values: submitCtx.values });
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
