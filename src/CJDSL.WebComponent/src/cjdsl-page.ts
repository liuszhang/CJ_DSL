// <cjdsl-page> Custom Element —— CJDSL 框架无关 Web Component 渲染器（路线 A 包裹）
//   内部用 react-dom/client 挂载 CJDSL.React 的 DslRenderer；
//   对外仅暴露标准 DOM 契约，不绑定任何宿主框架。
//
//   本文件为拆分后的主类文件：仅保留 CjdslPage 类本体（对外导出与注册方式不变），
//   样式/常量、DSL 解析工具、JSON 查看器、渲染挂载辅助分别拆至：
//     styles.ts / dsl-utils.ts / json-viewer.ts / render-mount.ts
//
// 用法（各产品瘦客户端）：
//   <script src=".../cjdsl-page.js"></script>
//   <cjdsl-page dsl='{...}' context='{"userId":"u1"}'></cjdsl-page>
//   宿主：el.addEventListener('cjdsl-action', e => { /* 落库/调后端 */ });
//   宿主回传：el.applyResult({ ok:true, message:'保存成功', setValues:{...} });
import { createRoot, type Root } from "react-dom/client";
import {
  DslStore,
  toDslNode,
  type DslNode,
} from "@cj/cjdsl-react";
import { BASE_STYLE, TOAST_COLORS } from "./styles";
import {
  parseDslSource,
  parseContextJson,
  parseSubmittedAttribute,
  computeObjectCode,
} from "./dsl-utils";
import { JsonViewerController, type JsonViewerDeps } from "./json-viewer";
import {
  createEmptyPlaceholder,
  createErrorBoundedRendererElement,
  createRendererCallbacks,
} from "./render-mount";

// ── 防御性兜底：React + autonomous custom element（名字带连字符的自定义元素）不兼容 ──
// 根因：react-dom 的 createElement —— **仅当 props.is 是字符串**才 `createElement(type, { is })`，否则直接 `createElement(type)`（无 options）。
//   autonomous CE（名字含连字符，如 cjdsl-page）**永远不该带 is**，带了浏览器就抛 NotSupportedError
//   "The result must not have attributes"，整棵子树渲染中断。
//   （is 只对「定制内置元素」（customized built-in，如 <button is="my-btn">）合法。）
//   **关键（v5 修正，实测推翻 v4 认知，勿再据 v4 注释推断）**：
//   读产物内真实 react-dom 源码确认，它**并非**无条件透传 options：
//     `else if (typeof props.is === "string") { createElement(type, { is: props.is }) } else { createElement(type) }`
//   即只有 props.is 是字符串时才带 options，否则 createElement(type) **不带任何 options**。
//   v4 部署后实测：崩溃调用**根本没有 options**（层 1/层 2 的 options 判据均为假，才会走到重抛）。
//   ⇒ 真实情况：某个 type 在「不带 options」时也会抛该错误，**未必与 is 有关**。
//   ⇒ 故 v5 兜底不再依赖 options，改用 createElementNS（无 is 参数，绕开一切 is 校验）。
//   ⇒ 若仍失败，层 2 末尾的 console.error 会打出真实 localName，据此定位根因。
// 兜底（v5）：层 1 预防 + 层 2 多级兜底，判据尽量只依赖入参，不依赖异常类型/message：
//   层 1 预防：options 存在且 localName 含连字符 → 直接丢弃 options 调原生，**从源头不抛异常**。
//   层 2 兜底：万一仍抛 → 依次尝试「丢弃 options」→「改用 createElementNS（没有 is 参数）」；
//             两者都失败才抛原异常，并 console.error 打出 localName / options / 错误消息 以定位根因。
//   教训（勿回退，前两版都栽在判据上）：
//     v2 预判标签名正则 → 漏网；v3 判 `e instanceof DOMException` + message → WebView2 下为假，原样重抛。
//     v3 失败时 options.is 为空却仍抛错，v4 曾归因于「{ is: undefined } 也被当有效 is」；v5 实测证实：崩溃调用根本没带 options。
// 该崩溃的真实触发元素在宿主 DA.DSH.PA React 树（本仓不可见），此兜底无论哪个元素触发都能解。
if (typeof Document !== "undefined" && typeof Document.prototype.createElement === "function") {
  const _origCreateElement = Document.prototype.createElement;
  Document.prototype.createElement = function (
    this: Document,
    localName: string,
    options?: ElementCreationOptions,
  ): HTMLElement {
    // 外层安全网：内部任何路径抛错都不得逸出（v6 曾漏掉「层1 丢弃 options 后」这条未包裹路径）
    try {
    // === v7：安全网（根因已在 CjdslPage 构造函数修复，本守卫仅作纵深防御）===
    // 真正的崩溃是「构造期写 style 属性」导致 createElement 抛 NotSupportedError，
    // 已把 inline 样式挪到 connectedCallback。本守卫保留，且仅在兜底路径才打印日志。
    // 层 1（预防 / 根治）：autonomous custom element（名字含连字符）永远不该带 options/is。
    //   （v4 曾推测连 { is: undefined } 也会触发，v5 实测不成立：崩溃调用未带 options；本层仍作为预防保留）
    //   —— react-dom 的 createInstance 无条件透传 { is: props.is }，故经 React 创建的 autonomous CE 必踩雷，
    //   与是否显式写了 is 无关。所以「含连字符」时一律丢弃 options，从源头不抛异常。
    //   判据只依赖入参（options 存在 + localName 含连字符），不依赖异常类型/message/options.is 是否为空。
    if (options && typeof localName === "string" && localName.indexOf("-") >= 0) {
      console.warn("[cjdsl-page] autonomous CE 调用带 options，已丢弃（含连字符的元素不接受 is）", {
        localName,
        is: options.is,
      });
      return _origCreateElement.call(this, localName);
    }
    // 层 2（兜底）：万一仍抛 —— 依次尝试「丢弃 options」→「改用 createElementNS」。
    //   createElementNS 根本没有 is 参数，可绕开一切与 is 相关的校验；
    //   **实测崩溃调用常常 options 为空**（react-dom 仅在 props.is 为字符串时才传 options，
    //   否则直接 createElement(type)），所以不能只靠「丢弃 options」这一招。
    //   两者都失败才抛原异常，并打完整现场（localName / options / 错误消息）用于定位真实标签名。
    try {
      return _origCreateElement.call(this, localName, options);
    } catch (e) {
      const errMsg = (e as Error | undefined)?.message;
      if (options) {
        try {
          return _origCreateElement.call(this, localName);
        } catch (_) {
          /* 丢弃 options 仍失败 → 继续下一个兜底 */
        }
      }
      try {
        const el = this.createElementNS("http://www.w3.org/1999/xhtml", localName);
        console.warn("[cjdsl-page] createElement 抛错，已用 createElementNS 兜底成功", {
          localName,
          options,
          err: errMsg,
        });
        return el;
      } catch (_) {
        /* createElementNS 兜底也失败 → 抛原异常 */
      }
      console.error("[cjdsl-page] createElement 彻底失败（已降级为空 div，不抛异常）", {
        localName,
        localNameType: typeof localName,
        options,
        err: errMsg,
      });
      // 兜底降级：绝不再抛，返回空 div 保证渲染链不中断（排障：确认 26011 是否真出自本守卫）
      try {
        return _origCreateElement.call(this, "div");
      } catch (_) {
        return undefined as unknown as HTMLElement;
      }
    }
    } catch (outer) {
      console.error("[cjdsl-page][GUARD] 外层安全网：内部仍抛错，降级为空 div", {
        localName,
        options,
        err: (outer as Error | undefined)?.message,
      });
      try {
        return _origCreateElement.call(this, "div");
      } catch (_) {
        return undefined as unknown as HTMLElement;
      }
    }
  } as typeof Document.prototype.createElement;
  // 版本标记：重启后 Console 出现本行即证明新 bundle 已加载（排障用，确认无缓存干扰）。
  console.info("[cjdsl-page] createElement guard v9 active (chart-fix + visible-logs + never-throw)");
}

export class CjdslPage extends HTMLElement {
  static get observedAttributes(): string[] {
    return ["dsl", "context", "submitted", "values", "json-viewer"]; // submitted：表单提交态恢复；values：持久化字段值回填（宿主桥挂载时注入）；json-viewer：源 JSON 查看按钮渲染级开关（缺省 true）
  }

  private root: Root | null = null;
  private store = new DslStore();
  private dslNode: DslNode | null = null;
  private userContext: Record<string, any> = {};
  // 源 JSON 查看按钮（方案 §3.5）：rawDslJson 保存 dsl 属性原始 JSON 字符串（浮层展示源）
  private rawDslJson = "";
  private readonly jsonViewer: JsonViewerController;
  // 同步兜底 div（独立于 React 树）：**默认 hidden（不显示）**，只有「渲染健康检查」
// 判定 React 未挂载任何内容时才显示。这样正常渲染完全不受干扰（关键：不能默认显示，
// 否则正常卡片下方也会多出一块占位）。
private fallbackEl: HTMLDivElement | null = null;
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
  private healthCheckTimer: number | null = null;
  // render 监听：用 MutationObserver 替代纯定时器，可靠捕获 React 真实提交（shadow DOM 内 useEffect/ErrorBoundary 不可靠）
  private renderObserver: MutationObserver | null = null;
  // 已判定失败标记：防止 observer/effect 在错误被捕获后又误回滚成成功态
  private renderFailed = false;

  constructor() {
    super();
    // 注意（重要，勿再改回）：自定义元素构造函数**禁止**给自身新增属性（HTML 规范硬性要求）。
    //   构造期写 this.style.* 会新增 style 属性，DOM 规范 createElement 第 6 步
    //   「attribute list 非空」会直接抛
    //     NotSupportedError: The result must not have attributes
    //   ⇒ document.createElement("cjdsl-page") 必然失败（与 is/options 完全无关）。
    //   inline 保底样式已统一挪到 connectedCallback（构造期之后再写属性是合法的）。
    const shadow = this.attachShadow({ mode: "open" });
    const style = document.createElement("style");
    style.textContent = BASE_STYLE;
    shadow.appendChild(style);
    const mount = document.createElement("div");
    mount.id = "cjdsl-mount";
    shadow.appendChild(mount);
    // 同步兜底 div：默认隐藏，仅健康检查判定失败时 showFallback 显示。
    this.fallbackEl = document.createElement("div");
    this.fallbackEl.id = "cjdsl-fallback";
    this.fallbackEl.hidden = true;
    this.fallbackEl.textContent = "DSL 解析失败或渲染异常，点击右上角按钮查看源码排查";
    shadow.appendChild(this.fallbackEl);
    this.root = createRoot(mount);
    // 源 JSON 查看器：构造时注册 Esc 关闭监听，disconnectedCallback 中 dispose 移除
    this.jsonViewer = new JsonViewerController({
      shadowRoot: shadow,
      getEnabled: () => this.getAttribute("json-viewer") !== "false",
      getRaw: () => this.rawDslJson,
      getObjectCode: () => this.objectCode(),
      getHostRect: () => this.getBoundingClientRect(), // panel 改 fixed 后按 host 视口矩形动态定位
      onOpenChange: (open) => {
        this.dispatchEvent(
          new CustomEvent("cjdsl-json-view", {
            bubbles: true,
            composed: true,
            detail: { open, objectCode: this.objectCode() },
          }),
        );
      },
    } satisfies JsonViewerDeps);
  }

  connectedCallback(): void {
    // 排障标记 v9：每次 cjdsl-page 实例被插入 DOM 时打一条 —— 确认新 bundle 加载 + 数清楚一共创建了几个 cjdsl-page
    // （如果灰框是另一个 cjdsl-page，这里会显示 instanceId 不同）
    console.info("[cjdsl-page] v9 instance connected", {
      instanceId: (this as { __id?: number }).__id ??= ++(globalThis as { __cjdsl_n__?: number }).__cjdsl_n__,
      mode: this.getAttribute("mode"),
      dslLen: this.getAttribute("dsl")?.length ?? 0,
    });
    // 内联保底高度双保险：CSS :host min-height 在某些 web component + Chromium 边界下
    // 可能不立即生效（host 默认 inline、shadow root 内样式应用时序），内联样式最稳。
    // 必须放在这里（而非 constructor）：构造期写 style 会新增属性，触发
    // DOM 规范 createElement 第 6 步的 NotSupportedError。
    this.style.minHeight = "48px";
    this.style.display = "block";
    // 重连兜底：若此前 disconnectedCallback 把 root/fallbackEl 置空（元素被宿主移出再移回 DOM），
    // 重建 React root 与 fallback 引用，否则 render() 会 early-return 导致卡片空壳（灰色框）。
    if (!this.root) {
      const mount = this.shadowRoot?.getElementById("cjdsl-mount");
      if (mount) this.root = createRoot(mount);
    }
    if (!this.fallbackEl) {
      this.fallbackEl =
        (this.shadowRoot?.getElementById("cjdsl-fallback") as HTMLDivElement) ?? null;
    }
    this.parseDsl();
    this.parseContext();
    this.restoreSubmitted(); // 挂载时恢复提交态（宿主桥已通过 submitted 属性回填）
    this.jsonViewer.sync(); // 按 json-viewer 属性同步按钮显隐（缺省 true 打开）
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
    // 直接清理监听与定时器（不走 clearRenderWatch 的 renderFailed 守卫，确保一定清掉）
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
    this.fallbackEl = null; // 释放 shadow 内 fallback div 引用，让 GC 回收
  }

  attributeChangedCallback(name: string, _old: string | null, _new: string | null): void {
    // json-viewer 为独立开关，只同步按钮/浮层显隐，不重渲染 DSL（避免干扰表单状态）
    if (name === "json-viewer") {
      this.jsonViewer.sync();
      return;
    }
    // name 已在 observedAttributes；dsl/context 变化都重解析并渲染
    this.parseDsl();
    this.parseContext();
    if (name === "submitted") this.restoreSubmitted(); // submitted 属性变化同步（预填补丁重设 dsl 时不受影响）
    if (name === "values") this.restoreValues(); // values 属性变化：宿主桥回填持久化字段值
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
    // 提交锁收尾：成功保持锁定（按钮置灰+字段只读）；失败解锁允许重试
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
  private restoreSubmitted(): void {
    const submitted = parseSubmittedAttribute(this.getAttribute("submitted"));
    if (submitted === undefined) return;
    this.store.set("__cjdsl_submitted", submitted);
  }

  /** 收集当前表单字段值（供提交时随事件上抛持久化；剔除内部提交态键） */
  private collectValues(): Record<string, unknown> {
    const snap = this.store.snapshot();
    const out: Record<string, unknown> = {};
    for (const [k, v] of Object.entries(snap)) {
      if (k === "__cjdsl_submitted") continue;
      out[k] = v;
    }
    return out;
  }

  /** 从 values 属性回填持久化字段值（延迟到 React layout effect seed 之后 merge，保证已提交值优先于预填） */
  private restoreValues(): void {
    const raw = this.getAttribute("values");
    if (raw == null) return;
    let parsed: unknown = null;
    try {
      parsed = JSON.parse(raw);
    } catch {
      parsed = null;
    }
    if (!parsed || typeof parsed !== "object") return;
    queueMicrotask(() => {
      this.store.merge(parsed as Record<string, unknown>);
    });
  }

  /** 提交态变更通知：上抛 CustomEvent，供宿主桥（CjdslPageBridge）持久化到 PA 端本地 */
  private notifySubmitted(): void {
    this.dispatchEvent(
      new CustomEvent("cjdsl-submitted", {
        bubbles: true,
        composed: true,
        detail: {
          submitted: this.store.get("__cjdsl_submitted") === true,
          values: this.collectValues(),
        },
      }),
    );
  }

  private parseDsl(): void {
    const { parsed, rawSource } = parseDslSource(this.getAttribute("dsl"), this.innerHTML);
    // 保存原始字符串（浮层展示源，方案 §3.3：以原始 dsl 属性字符串为准，避免归一化丢字段）。
    // 关键：解析失败时也保留原始串（不再置空），否则「非法 JSON 导致渲染失败」场景下
    // 用户点开源码只看到「（无 DSL 内容）」，排查线索被抹掉。formatJson 对非 JSON 原样返回，不会报错。
    this.rawDslJson = rawSource;
    this.dslNode = toDslNode(parsed) ?? null;
  }

  private parseContext(): void {
    this.userContext = parseContextJson(this.getAttribute("context"));
  }

  private objectCode(): string {
    return computeObjectCode(this.dslNode?.id, this.userContext);
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

  private render(): void {
    if (!this.root) return;
    if (!this.dslNode) {
      // 解析失败 / 无内容：给 host 打 data-cjdsl-error（CSS 据此让源码按钮常驻 + 警示红），
      // 配合 :host min-height:48px 保证按钮不被裁切，用户可点开源码兜底排查。
      const hasRaw = this.rawDslJson.trim().length > 0;
      // 注意：**不传 onCommit** —— 失败分支的 placeholder 一旦 commit 就会触发
      // cancelHealthCheck（回滚 error 标记 + 隐藏 fallback），反而会把失败态擦掉。
      this.root.render(createEmptyPlaceholder(hasRaw ? "invalid" : "empty"));
      this.markRenderFailed(); // 解析失败是确定的失败，立即显示兜底（error 标记 + fallback）
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
          getMode: () => this.getAttribute("mode") || undefined,
          store: this.store,
          onSubmitted: () => this.notifySubmitted(),
          dispatchAction: (detail) => this.dispatchAction(detail),
          showToast: (message, severity) => this.showToast(message, severity),
        }),
        (err: Error) => this.onRenderError(err),
        () => this.cancelHealthCheck(),
      ),
    );
    this.restoreValues(); // 每次渲染后回填持久化字段值（覆盖 DslRenderer seed，dsl 预填补丁重渲时保持已提交值优先）
    // 启动「渲染监听」：用 MutationObserver 在 React 真正把内容挂进 mount 时立即判定成功，
    // 不依赖不可靠的 useEffect/ErrorBoundary；800ms 宽限内无内容才判定失败，避免慢提交被误伤。
    // 注意：用 console.info 而非 console.debug —— Chrome DevTools 默认日志级别是 Info，
    // debug 级默认被隐藏，导致排障时「只看到一行日志」的假象。
    console.info("[cjdsl-page] render success-branch", {
      dslId: this.dslNode?.id,
      nodeTypes: this.collectNodeTypes(this.dslNode),
    });
    this.startRenderWatch();
    // 排障 v9：render 完立刻打 mount 实际 DOM 子元素——确认 React 真的把内容挂进 #cjdsl-mount 了
    // 用 queueMicrotask 等 React commit 完再读
    queueMicrotask(() => {
      const mount = this.shadowRoot?.getElementById("cjdsl-mount");
      if (!mount) return;
      const childInfo = Array.from(mount.children).map((el) => ({
        tag: el.tagName.toLowerCase(),
        cls: (el as HTMLElement).className?.toString().slice(0, 40) || "",
        text0: (el.textContent || "").slice(0, 30),
      }));
      console.info("[cjdsl-page] v9 mount.children after render", {
        childCount: mount.children.length,
        childInfo,
      });
    });
  }

  /** 递归收集 DSL 树里出现的所有节点类型（排障用：确认到底有没有 chart 节点） */
  private collectNodeTypes(node: unknown, out: string[] = []): string[] {
    if (!node || typeof node !== "object") return out;
    const n = node as { type?: unknown; children?: unknown };
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
  private startRenderWatch(): void {
    this.renderFailed = false;
    this.clearRenderWatch();
    const mount = this.shadowRoot?.getElementById("cjdsl-mount");
    if (!mount) {
      this.markRenderFailed("no-mount");
      return;
    }
    // 已挂载内容（如重渲染保留的内容）→ 直接视为成功
    if (mount.childNodes.length > 0) {
      console.info("[cjdsl-page] mount 已有内容，判定成功");
      return;
    }
    this.renderObserver = new MutationObserver(() => {
      if (mount.childNodes.length > 0) {
        console.info("[cjdsl-page] MutationObserver 捕获到提交，判定成功");
        this.clearRenderWatch();
      }
    });
    this.renderObserver.observe(mount, { childList: true, subtree: false });
    this.healthCheckTimer = window.setTimeout(() => {
      if (mount.childNodes.length === 0) {
        console.error("[cjdsl-page] 渲染监听超时：800ms 内 mount 无内容，判定渲染失败", {
          dslId: this.dslNode?.id,
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
  private clearRenderWatch(): void {
    if (this.renderFailed) return; // 已判定失败，忽略后续成功信号，避免误回滚
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
  private cancelHealthCheck(): void {
    this.clearRenderWatch();
  }

  /**
   * 判定渲染失败：打 error 标记（源码按钮常驻 + 警示红）+ 显示同步兜底 div，并停止渲染监听。
   * reason 仅用于诊断日志，方便在 DevTools 控制台区分失败来源。
   */
  private markRenderFailed(reason = "unknown"): void {
    this.renderFailed = true;
    console.warn("[cjdsl-page] 渲染判定失败", { reason, dslId: this.dslNode?.id });
    this.toggleAttribute("data-cjdsl-error", true);
    if (this.fallbackEl) this.fallbackEl.hidden = false;
    // 已确认失败，停止监听，避免后续提交误回滚
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
  private onRenderError(err: Error): void {
    console.error("[cjdsl-page] DSL 渲染异常，已降级为失败占位（可点源码按钮查看原始 DSL）", err);
    this.markRenderFailed();
  }
}
