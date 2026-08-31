// <cjdsl-page> 样式与常量（自 cjdsl-page.ts 拆分）
//   BASE_STYLE：shadow DOM 内注入的全部样式（含源 JSON 查看按钮/浮层，方案 §3.2/§3.3）
//   TOAST_COLORS：内置 toast 兜底颜色表（severity → 背景色）

export const BASE_STYLE = `
  /* min-height:48px —— 保底高度：DSL 解析失败/为空/渲染异常时卡片仍留有空间容纳
     源码按钮（top:8 + 28px 高 = 36px），避免按钮被裁切导致无法兜底查看源码排查。
     正常渲染时内容高于 48px，min-height 不产生任何视觉影响。 */
  :host { position: relative; display: block; min-height: 48px; box-sizing: border-box; font-family: -apple-system, BlinkMacSystemFont, "Segoe UI", Roboto, "Helvetica Neue", Arial, "PingFang SC", "Microsoft YaHei", sans-serif; color: rgba(0,0,0,0.87); }
  * { box-sizing: border-box; }
  /* mount div 自身也设 min-height —— 双保险：即便 React ErrorBoundary 没兜住（web component +
     React 18 commit 边界的已知陷阱），shadow 内容仍至少 48px 高，bridge 容器不塌成 1-2px 灰线 */
  #cjdsl-mount { min-height: 48px; display: block; }
  /* 同步 fallback div（独立于 React 树）：React 首次 commit 成功前显示，commit 成功后再隐藏。
     position:relative + z-index:1 让 React 内容若挂载上去会盖在它上面（不冲突）。 */
  #cjdsl-fallback {
    display: flex; align-items: center; justify-content: center;
    min-height: 48px; padding: 8px 10px;
    color: #c62828; font-size: 12px; line-height: 1.5; text-align: center;
    border: 1px dashed #ef9a9a; border-radius: 8px; background: #fff8f8;
    position: relative; z-index: 1;
  }
  #cjdsl-fallback[hidden] { display: none; }
  #cjdsl-toast { position: absolute; top: 8px; left: 8px; right: 8px; padding: 8px 12px; border-radius: 6px; font-size: 13px; z-index: 999; display: none; box-shadow: 0 2px 8px rgba(0,0,0, 0.18); }
  /* 源 JSON 查看按钮：默认隐藏，鼠标划入卡片（或键盘聚焦）后显示（方案 §3.2） */
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
  /* 渲染失败态（host 带 data-cjdsl-error）：源码按钮常驻可见 + 警示红，
     让失败卡片一眼可辨，且无需 hover 即可点开源码兜底排查（正常渲染仍走 hover 逻辑）。 */
  :host([data-cjdsl-error]) .cjdsl-json-viewer-btn {
    opacity: 1; pointer-events: auto; background: #c62828;
  }
  :host([data-cjdsl-error]) .cjdsl-json-viewer-btn:hover { background: #b71c1c; }
  /* 失败态视觉（红色虚线 + 浅红底）由 #cjdsl-fallback div 承担（同步、不依赖 React）。
     这里不再在 :host 上加 border/background，避免与 fallback div 双层描边。 */
  /* 源 JSON 浮层（卡片跟随式 Popover，方案 §3.3 修订）
     position: fixed：escapes CjdslPageBridge 的 overflow:hidden 裁切（bridge 渲染容器
     borderRadius:10 + overflow:hidden 会切断 absolute 子元素的下沿），由 JsonViewerController
     按 host.getBoundingClientRect() 动态计算 top/right + flip。
     max-height 收紧到 400px：DA.DSH.PA 是 MAUI 混合应用，WebView2 视口底部被 MAUI 原生输入框
     覆盖约 100px（具体值用户态变化），100vh 包含被遮的部分会导致浮层底部进 MAUI 输入框层；
     JS 在 syncPosition 里按「host 下方可用空间 vs 上方可用空间」选大的一侧放置 + 必要时再
     收缩 max-height 到实际可用值，保证浮层完整可见。
     布局用普通 block（非 flex）：避免 flex-basis 覆盖 height 导致滚动容器高度失效，
     body 高度由 JsonViewerController 打开时 JS 显式计算设置（panel.clientHeight - headH） */
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
  /* body 为唯一滚动容器：CSS overflow:auto 覆盖 X+Y，JS 显式设 height/maxHeight 约束 Y；
     内容超出即可滚动，不依赖 flex 收缩。原先 pre 上也有 overflow:auto 形成双滚动条，
     视觉上只有最内层生效，外层 body 反而看不出在滚——故移除 pre 的 overflow。 */
  .cjdsl-json-viewer-body { overflow: auto; }
  .cjdsl-json-viewer-body pre {
    margin: 0; padding: 10px 12px; font-size: 12px; line-height: 1.55;
    font-family: Consolas, "SF Mono", Menlo, "Courier New", monospace; color: #24292f;
    white-space: pre;
  }
  /* 滚动条增强可见性（WebKit / Firefox） */
  .cjdsl-json-viewer-body::-webkit-scrollbar { width: 10px; height: 10px; }
  .cjdsl-json-viewer-body::-webkit-scrollbar-thumb { background: rgba(0,0,0,0.25); border-radius: 5px; border: 2px solid transparent; background-clip: content-box; }
  .cjdsl-json-viewer-body::-webkit-scrollbar-thumb:hover { background: rgba(0,0,0,0.4); border: 2px solid transparent; background-clip: content-box; }
  .cjdsl-json-viewer-body { scrollbar-width: thin; scrollbar-color: rgba(0,0,0,0.25) transparent; }
`;

export const TOAST_COLORS: Record<string, string> = {
  info: "#0277BD",
  success: "#2E7D32",
  warning: "#F57C00",
  error: "#C62828",
};
