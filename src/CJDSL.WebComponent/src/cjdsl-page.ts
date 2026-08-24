// <cjdsl-page> Custom Element —— CJDSL 框架无关 Web Component 渲染器（路线 A 包裹）
//   内部用 react-dom/client 挂载 CJDSL.React 的 DslRenderer；
//   对外仅暴露标准 DOM 契约，不绑定任何宿主框架。
//
// 用法（各产品瘦客户端）：
//   <script src=".../cjdsl-page.js"></script>
//   <cjdsl-page dsl='{...}' context='{"userId":"u1"}'></cjdsl-page>
//   宿主：el.addEventListener('cjdsl-action', e => { /* 落库/调后端 */ });
//   宿主回传：el.applyResult({ ok:true, message:'保存成功', setValues:{...} });
import React from "react";
import { createRoot, type Root } from "react-dom/client";
import {
  DslRenderer,
  DslStore,
  toDslNode,
  type DslNode,
  type RendererCallbacks,
} from "@cj/cjdsl-react";

const BASE_STYLE = `
  :host { display: block; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, "PingFang SC", "Microsoft YaHei", sans-serif; color: rgba(0,0,0,0.87); }
  * { box-sizing: border-box; }
  #cjdsl-toast { position: absolute; top: 8px; left: 8px; right: 8px; padding: 8px 12px; border-radius: 6px; font-size: 13px; z-index: 999; display: none; box-shadow: 0 2px 8px rgba(0,0,0, 0.18); }
`;

const TOAST_COLORS: Record<string, string> = {
  info: "#0277BD",
  success: "#2E7D32",
  warning: "#F57C00",
  error: "#C62828",
};

export class CjdslPage extends HTMLElement {
  static get observedAttributes(): string[] {
    return ["dsl", "context"];
  }

  private root: Root | null = null;
  private store = new DslStore();
  private dslNode: DslNode | null = null;
  private userContext: Record<string, any> = {};

  constructor() {
    super();
    const shadow = this.attachShadow({ mode: "open" });
    const style = document.createElement("style");
    style.textContent = BASE_STYLE;
    shadow.appendChild(style);
    const mount = document.createElement("div");
    mount.id = "cjdsl-mount";
    shadow.appendChild(mount);
    this.root = createRoot(mount);
  }

  connectedCallback(): void {
    this.parseDsl();
    this.parseContext();
    this.render();
    this.dispatchEvent(
      new CustomEvent("cjdsl-ready", {
        bubbles: true,
        composed: true,
        detail: { id: this.id || undefined },
      }),
    );
  }

  disconnectedCallback(): void {
    this.root?.unmount();
    this.root = null;
  }

  attributeChangedCallback(_name: string, _old: string | null, _new: string | null): void {
    // name 已在 observedAttributes；dsl/context 变化都重解析并渲染
    this.parseDsl();
    this.parseContext();
    this.render();
  }

  /** 宿主回传结果（方案：宿主回传经 Web Component 暴露的方法） */
  applyResult(result: {
    ok?: boolean;
    message?: string;
    severity?: "info" | "success" | "warning" | "error";
    setValues?: Record<string, unknown>;
    refresh?: boolean;
  }): void {
    if (result.setValues) this.store.merge(result.setValues);
    if (result.message) {
      this.showToast(result.message, result.severity || (result.ok === false ? "error" : "info"));
    }
    if (result.refresh) this.render();
  }

  private parseDsl(): void {
    const raw = this.getAttribute("dsl");
    let parsed: unknown = null;
    if (raw) {
      try {
        parsed = JSON.parse(raw);
      } catch {
        parsed = null;
      }
    }
    // 退化：dsl 属性未设置时尝试读取 innerHTML JSON（便于 <cjdsl-page>{...}</cjdsl-page> 写法）
    if (!parsed && this.innerHTML.trim()) {
      try {
        parsed = JSON.parse(this.innerHTML.trim());
      } catch {
        parsed = null;
      }
    }
    this.dslNode = toDslNode(parsed) ?? null;
  }

  private parseContext(): void {
    const raw = this.getAttribute("context");
    if (!raw) {
      this.userContext = {};
      return;
    }
    try {
      this.userContext = JSON.parse(raw) || {};
    } catch {
      this.userContext = {};
    }
  }

  private objectCode(): string {
    return String(this.dslNode?.id || this.userContext?.objectCode || "dsl");
  }

  private dispatchAction(detail: Record<string, unknown>): void {
    this.dispatchEvent(
      new CustomEvent("cjdsl-action", {
        bubbles: true,
        composed: true,
        detail: { objectCode: this.objectCode(), context: this.userContext, ...detail },
      }),
    );
  }

  private showToast(message: string, severity = "info"): void {
    // 桥接：始终抛出 cjdsl-action(type:toast)，宿主可接管；同时内部渲染轻量 toast 兜底
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
    window.clearTimeout((bar as any)._t);
    (bar as any)._t = window.setTimeout(() => {
      if (bar) bar.style.display = "none";
    }, 3000);
  }

  private callbacks(): RendererCallbacks {
    return {
      mode: this.getAttribute("mode") || undefined,
      onSubmit: (ctx) => {
        this.dispatchAction({
          type: "submit",
          action: ctx.action,
          data: ctx.values,
        });
        return { ok: true, message: "已提交，等待宿主处理" };
      },
      onApiCall: (params, formValues) => {
        this.dispatchAction({
          type: "apiCall",
          action: String(params?.action ?? ""),
          data: formValues,
          apiParams: params,
        });
        return { ok: true, message: "已发起 API 调用，等待宿主处理" };
      },
      onToast: (msg, sev) => this.showToast(msg, sev || "info"),
      onNavigate: (path) => {
        this.dispatchAction({ type: "navigate", action: "navigate", data: { path } });
      },
    };
  }

  private render(): void {
    if (!this.root) return;
    if (!this.dslNode) {
      this.root.render(
        React.createElement(
          "div",
          { style: { color: "#888", fontSize: 13, padding: 8 } },
          "（无 DSL 内容）",
        ),
      );
      return;
    }
    this.root.render(
      React.createElement(DslRenderer, {
        root: this.dslNode,
        store: this.store,
        callbacks: this.callbacks(),
      }),
    );
  }
}
