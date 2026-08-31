// 渲染挂载辅助（自 cjdsl-page.ts 拆分）
//   渲染元素构建与 RendererCallbacks 工厂；react-dom 挂载点（root）与生命周期由 CjdslPage 主类保留。
import React, { useEffect } from "react";
import {
  DslRenderer,
  type DslStore,
  type DslNode,
  type RendererCallbacks,
  type SubmitContext,
  type FormValues,
} from "@cj/cjdsl-react";

/**
 * 占位元素。
 *   "empty"   —— dsl 完全为空（未设置/空字符串），灰字提示。
 *   "invalid" —— dsl 有内容但解析失败 / 渲染异常，红色警示 + 引导点击源码按钮兜底排查。
 * 两种情况卡片均保留最小高度（:host min-height:48px），保证源码按钮不被裁切。
 *
 * onCommit 参数：commit 成功后调一次（用于主类隐藏同步 fallback div）。由嵌入的
 * CommitSuccessNotifier 用 useEffect 触发 —— 仅在 commit 真正成功时跑。
 */
export function createEmptyPlaceholder(
  reason: "empty" | "invalid" = "empty",
  onCommit?: () => void,
): React.ReactElement {
  const placeholder = React.createElement(
    "div",
    {
      style:
        reason === "invalid"
          ? {
              color: "#c62828",
              fontSize: 12,
              padding: "8px 10px",
              border: "1px dashed #ef9a9a",
              borderRadius: 4,
              background: "#ffebee",
            }
          : { color: "#888", fontSize: 13, padding: 8 },
    },
    reason === "invalid"
      ? "DSL 解析失败或渲染异常，点击右上角按钮查看源码排查"
      : "（无 DSL 内容）",
  );
  if (!onCommit) return placeholder;
  return React.createElement(
    React.Fragment,
    null,
    React.createElement(CommitSuccessNotifier, { onCommit }),
    placeholder,
  );
}

/**
 * 渲染错误边界：捕获 DslRenderer 内部运行时抛错（不支持的节点结构、表达式求值异常等），
 * 转为失败占位而非让 React 卸载整棵树（否则卡片高度归零，源码按钮也不可见 → 排查盲区）。
 * 同时回调通知主类给 host 打 data-cjdsl-error，触发按钮常驻 + 警示红样式。
 */
export class DslRenderErrorBoundary extends React.Component<
  { children: React.ReactNode; onError: (err: Error) => void },
  { failed: boolean }
> {
  constructor(props: { children: React.ReactNode; onError: (err: Error) => void }) {
    super(props);
    this.state = { failed: false };
  }

  static getDerivedStateFromError(): { failed: boolean } {
    return { failed: true };
  }

  componentDidCatch(error: Error): void {
    this.props.onError(error);
  }

  render(): React.ReactNode {
    if (this.state.failed) return createEmptyPlaceholder("invalid");
    return this.props.children;
  }
}

/** 构建 DslRenderer 渲染元素 */
export function createDslRendererElement(
  root: DslNode,
  store: DslStore,
  callbacks: RendererCallbacks,
): React.ReactElement {
  return React.createElement(DslRenderer, { root, store, callbacks });
}

/**
 * 用 ErrorBoundary 包裹 DslRenderer 元素。
 * 单独提供是为了让主类（cjdsl-page.ts）不必直接依赖 React.createElement —— 主类目前不 import React。
 *
 * 关键细节：内嵌一个 CommitSuccessNotifier 组件，它的 useEffect 会在 commit 成功后调 onCommit
 * 通知主类「React 渲染成功，可以隐藏同步 fallback div 了」。
 * - commit 失败被 ErrorBoundary 兜住时，CommitSuccessNotifier 不会被挂载（被占位组件替换），useEffect 不跑
 *   → onCommit 不调 → 主类 fallback div 保留显示（红框红字）
 * - commit 失败且 ErrorBoundary 也没兜住（web component shadow 内 React 边界），整树 unmount，
 *   onCommit 也不调 → fallback div 仍显示（默认隐藏属性 = false）
 * - commit 成功 → onCommit 调 → fallback div 隐藏，正常内容显示
 * 靠这个机制彻底规避「render() 同步调用 hideFallback 会把 fallback 也藏掉」的陷阱。
 */
export function createErrorBoundedRendererElement(
  root: DslNode,
  store: DslStore,
  callbacks: RendererCallbacks,
  onError: (err: Error) => void,
  onCommit: () => void,
): React.ReactElement {
  return React.createElement(
    DslRenderErrorBoundary,
    { onError },
    React.createElement(CommitSuccessNotifier, { onCommit }),
    createDslRendererElement(root, store, callbacks),
  );
}

/**
 * 空渲染 + 仅在 commit 成功时调一次 onCommit 的哨兵组件。
 * 用 useEffect 触发（commit 后异步执行），避免 render 阶段同步调用干扰 React 调度。
 */
function CommitSuccessNotifier({ onCommit }: { onCommit: () => void }): null {
  useEffect(() => {
    onCommit();
  }, [onCommit]);
  return null;
}

/** 构建 RendererCallbacks 所需的宿主依赖（主类注入自身实现，避免循环依赖） */
export interface RendererCallbackDeps {
  getMode: () => string | undefined;
  store: DslStore;
  onSubmitted: () => void;
  dispatchAction: (detail: Record<string, unknown>) => void;
  showToast: (message: string, severity?: string) => void;
}

/** 构建 RendererCallbacks（事件 → 宿主动作转发，语义与拆分前一致） */
export function createRendererCallbacks(deps: RendererCallbackDeps): RendererCallbacks {
  return {
    mode: deps.getMode(),
    onSubmit: (ctx: SubmitContext) => {
      // 乐观锁：点击即置灰表单、禁用按钮，防连点重复提交（失败由 applyResult 解锁）
      deps.store.set("__cjdsl_submitted", true);
      deps.onSubmitted();
      deps.dispatchAction({
        type: "submit",
        action: ctx.action,
        data: ctx.values,
      });
      return { ok: true, message: "已提交，等待宿主处理" };
    },
    onApiCall: (params: Record<string, any>, formValues: FormValues) => {
      deps.dispatchAction({
        type: "apiCall",
        action: String(params?.action ?? ""),
        data: formValues,
        apiParams: params,
      });
      return { ok: true, message: "已发起 API 调用，等待宿主处理" };
    },
    onToast: (msg: string, sev?: string) => deps.showToast(msg, sev || "info"),
    onNavigate: (path: string) => {
      deps.dispatchAction({ type: "navigate", action: "navigate", data: { path } });
    },
  };
}
