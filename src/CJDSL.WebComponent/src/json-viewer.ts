// 源 JSON 查看器模块（自 cjdsl-page.ts 拆分，方案 §3.2/§3.3/§3.4/§3.5）
//   JsonViewerController 封装按钮/浮层的惰性创建、开关同步、浮层切换、复制、Esc/外点关闭；
//   通过 deps 依赖注入与 CjdslPage 主类解耦（不反向依赖主类，避免循环依赖）。
import { formatJson } from "./dsl-utils";

/** 主类注入的宿主依赖（全部为最小接口，主类私有成员不外泄） */
export interface JsonViewerDeps {
  shadowRoot: ShadowRoot;
  /** 渲染级开关是否开启（json-viewer 属性缺省 true，显式 "false" 关闭） */
  getEnabled: () => boolean;
  /** 取 dsl 属性原始 JSON 字符串（浮层展示源） */
  getRaw: () => string;
  /** 业务对象编码（事件 detail 用） */
  getObjectCode: () => string;
  /** host 元素视口矩形（panel 改 fixed 后按此计算 top/right，跟随 host 移动） */
  getHostRect: () => DOMRect;
  /** 浮层开/关状态变化回调（上抛 cjdsl-json-view 事件） */
  onOpenChange: (open: boolean) => void;
}

export class JsonViewerController {
  private button: HTMLButtonElement | null = null;
  private panel: HTMLDivElement | null = null;
  private open = false;
  // 跟随 host 用的 passive 监听 + ResizeObserver（仅 open 时挂上，close/dispose 移除）
  private readonly reposition = (): void => {
    if (this.open) this.syncPosition();
  };
  private hostResizeObs: ResizeObserver | null = null;

  constructor(private readonly deps: JsonViewerDeps) {
    // Esc 关闭浮层（方案 §3.2）；window 级监听，浮层打开期间任意焦点可关闭
    window.addEventListener("keydown", this.onKeyDown);
    // 监听 host 尺寸变化（如聊天区布局调整导致 cjdsl-page 高度改变），触发重新定位
    if (typeof ResizeObserver !== "undefined") {
      const host = deps.shadowRoot.host; // ShadowRoot.host 即 cjdsl-page 元素本身
      if (host) {
        this.hostResizeObs = new ResizeObserver(() => this.reposition());
        this.hostResizeObs.observe(host);
      }
    }
  }

  private onKeyDown = (e: KeyboardEvent): void => {
    if (e.key === "Escape" && this.open) this.toggle(false);
  };

  /** 元素卸载时移除 window 级监听（由主类 disconnectedCallback 调用） */
  dispose(): void {
    window.removeEventListener("keydown", this.onKeyDown);
    this.detachFollowListeners();
    this.hostResizeObs?.disconnect();
    this.hostResizeObs = null;
  }

  /** 渲染级开关同步（json-viewer 缺省 true；显式 false 时隐藏按钮并关闭浮层） */
  sync(): void {
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
  toggle(force?: boolean): void {
    if (!this.deps.getEnabled()) return;
    this.ensureUi();
    if (!this.panel) return;
    const open = force ?? !this.open;
    // panel 为 fixed 布局：top/right 由 syncPosition 按 host.getBoundingClientRect() 算
    this.panel.style.display = open ? "block" : "none";
    this.open = open;
    if (open) {
      const code = this.panel.querySelector("code");
      if (code) {
        // textContent 渲染，禁止 innerHTML（方案 §3.3/§3.6 XSS 纪律）
        const raw = this.deps.getRaw();
        const formatted = formatJson(raw);
        if (raw.length > 500 * 1024) {
          code.textContent = "内容过大，请从控制台查看。\n\n" + formatted.slice(0, 20000) + "\n…（已截断）";
        } else {
          code.textContent = formatted;
        }
      }
      // RAF 等首帧 layout 完成：定位 + body 高度同时算；挂载跟随监听
      requestAnimationFrame(() => {
        this.syncPosition();
        this.attachFollowListeners();
      });
    } else {
      this.detachFollowListeners();
      // 关闭时清掉 inline top/right/maxHeight，避免下次打开位置/尺寸错乱
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
  private syncPosition(): void {
    if (!this.panel) return;
    const rect = this.deps.getHostRect();
    if (rect.width <= 0 && rect.height <= 0) return;
    const vw = window.innerWidth;
    const vh = window.innerHeight;
    // CSS 默认 max-height 的 JS 等价值（与 styles.ts 保持一致）
    const cssMax = Math.min(vh - 16, Math.min(vh * 0.6, 400));
    const MIN_PANEL_H = 160; // 浮层最小可读高度，低于此认为空间不够强行压扁

    // 下方可用空间 = (host.top + 44) 到 vh-16 的距离
    const belowTop = rect.top + 44;
    const belowAvail = Math.max(0, vh - belowTop - 16);
    // 上方可用空间 = 8 (顶部留白) 到 (host.top - 8)（按钮上方 8px）的距离
    const aboveBottom = rect.top - 8;
    const aboveAvail = Math.max(0, aboveBottom - 8);

    // 选择空间更大的一侧（默认下方，保留视觉「在卡片右上方」的感觉）
    const placeBelow = belowAvail >= aboveAvail;
    const avail = placeBelow ? belowAvail : aboveAvail;
    const targetH = Math.max(MIN_PANEL_H, Math.min(cssMax, avail));

    if (placeBelow) {
      this.panel.style.top = `${belowTop}px`;
      this.panel.style.bottom = "auto";
    } else {
      // flip：浮层底部锚定在 (host.top - 8)，即 bottom = vh - (host.top - 8)
      this.panel.style.bottom = `${vh - aboveBottom}px`;
      this.panel.style.top = "auto";
    }
    // 必要时覆盖 CSS 默认 max-height（防止实际可用空间 < cssMax）
    this.panel.style.maxHeight = `${targetH}px`;

    const right = Math.max(8, vw - rect.right + 8);
    this.panel.style.right = `${right}px`;
    // 同步刷新 body 高度（panel clientHeight 受 max-height 约束）
    this.syncBodyHeight();
  }

  /** open 期间挂跟随监听：滚动（capture 截获嵌套滚动容器）+ 视口 resize + host 尺寸变化（已由 ResizeObserver 覆盖） */
  private attachFollowListeners(): void {
    window.addEventListener("scroll", this.reposition, { passive: true, capture: true });
    window.addEventListener("resize", this.reposition, { passive: true });
  }

  /** close/dispose 时清理跟随监听，避免泄漏 */
  private detachFollowListeners(): void {
    window.removeEventListener("scroll", this.reposition, { capture: true });
    window.removeEventListener("resize", this.reposition);
  }

  /**
   * 显式约束滚动容器（body）高度 = 面板可用高度 - 头部高度，确保内容超出时 overflow 滚动生效。
   * panel 为 block 布局，clientHeight 受 max-height:min(70vh,520px) 限制；
   * body 为独立滚动容器（非 flex item，height 内联生效），内容超出即可滚动。
   */
  private syncBodyHeight(): void {
    if (!this.panel) return;
    const head = this.panel.querySelector<HTMLElement>(".cjdsl-json-viewer-panel-head");
    const body = this.panel.querySelector<HTMLElement>(".cjdsl-json-viewer-body");
    if (!head || !body) return;
    const panelH = this.panel.clientHeight;
    const headH = head.offsetHeight;
    const bodyH = Math.max(80, panelH - headH);
    body.style.height = bodyH + "px";
    body.style.maxHeight = bodyH + "px";
    body.style.overflowY = "auto";
  }

  /** 复制源 JSON（navigator.clipboard 优先，失败降级 execCommand） */
  copy(): void {
    const text = formatJson(this.deps.getRaw());
    const done = () => {
      if (this.button) {
        const old = this.button.title;
        this.button.title = "已复制";
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
        /* 复制失败静默，不影响其它交互 */
      }
    };
    if (navigator.clipboard?.writeText) {
      navigator.clipboard.writeText(text).then(done).catch(fallback);
    } else {
      fallback();
    }
  }

  /** 惰性创建按钮与浮层骨架（关闭时 DOM 不创建，零运行时开销） */
  private ensureUi(): void {
    const shadow = this.deps.shadowRoot;
    if (!shadow || this.button) return;

    const btn = document.createElement("button");
    btn.type = "button";
    btn.className = "cjdsl-json-viewer-btn";
    btn.setAttribute("aria-label", "查看 CJDSL 源 JSON");
    btn.title = "查看源 JSON";
    // 内联 SVG 大括号（JSON 语义），不引入图标库，保持 bundle 零依赖（方案 §3.2）
    btn.innerHTML =
      '<svg width="14" height="14" viewBox="0 0 24 24" fill="none" stroke="currentColor" stroke-width="2.2" stroke-linecap="round" stroke-linejoin="round" aria-hidden="true">' +
      '<path d="M8 3H7a2 2 0 0 0-2 2v5a2 2 0 0 1-2 2 2 2 0 0 1 2 2v5c0 1.1.9 2 2 2h1"/><path d="M16 21h1a2 2 0 0 0 2-2v-5c0-1.1.9-2 2-2a2 2 0 0 1-2-2V5a2 2 0 0 0-2-2h-1"/>' +
      "</svg>";
    btn.addEventListener("click", (e) => {
      e.stopPropagation(); // 不冒泡触发 cjdsl-action / 聊天行事件（方案 §3.2/§3.6）
      this.toggle();
    });
    shadow.appendChild(btn);
    this.button = btn;

    const panel = document.createElement("div");
    panel.className = "cjdsl-json-viewer-panel";
    panel.innerHTML =
      '<div class="cjdsl-json-viewer-panel-head"><span>CJDSL 源 JSON</span>' +
      '<span class="cjdsl-json-viewer-actions">' +
      '<button type="button" data-copy>复制</button>' +
      '<button type="button" data-close>关闭</button>' +
      "</span></div>" +
      '<div class="cjdsl-json-viewer-body"><pre><code></code></pre></div>';
    panel.querySelector<HTMLButtonElement>("[data-copy]")!.addEventListener("click", (e) => {
      e.stopPropagation();
      this.copy();
    });
    panel.querySelector<HTMLButtonElement>("[data-close]")!.addEventListener("click", (e) => {
      e.stopPropagation();
      this.toggle(false);
    });
    // 浮层内部点击一律拦截，避免冒泡关闭自身；浮层外 shadow 内点击由下方监听关闭（方案 §3.2）
    panel.addEventListener("click", (e) => e.stopPropagation());
    shadow.appendChild(panel);
    this.panel = panel;

    // 点击浮层外区域（shadow DOM 内）关闭浮层
    shadow.addEventListener("click", (e) => {
      if (!this.open) return;
      const t = e.target as Node;
      if (t === this.button) return; // 按钮已 stopPropagation，不会到达此处，防御性判断
      if (this.panel && !this.panel.contains(t)) this.toggle(false);
    });
  }
}
