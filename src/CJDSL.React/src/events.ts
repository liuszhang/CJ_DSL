// EventDispatcher（v1）：submit/apiCall/setValue/chain/showToast/navigate
//  - confirm：事件级确认框（cancel 中断链）
//  - onSuccess：submit/apiCall 成功后回调（链式继续）
//  - debounceMs：防抖
import { DslStore } from "./store";

export interface DslEvent {
  type: string;
  handler: string;
  params?: Record<string, any>;
  confirm?: { title?: string; message?: string; confirmText?: string; cancelText?: string };
  debounceMs?: number;
}

export interface FormValues {
  [fieldName: string]: unknown;
}

export interface SubmitContext {
  action: string;
  formId?: string;
  values: FormValues;
}

export interface EventCallbacks {
  /** 表单提交：返回 { ok, message }，供调用方（index.tsx）决定 toast */
  onSubmit?: (ctx: SubmitContext) => Promise<{ ok: boolean; message?: string }> | { ok: boolean;	message?: string };
  /** 通用 API 调用：默认走 /api/cjdsl/datasource 或由调用方实现 */
  onApiCall?: (params: Record<string, any>, formValues: FormValues) => Promise<{ ok: boolean; message?: string; data?: unknown }>;
  /** toast 提示 */
  onToast?: (message: string, severity?: string) => void;
  /** 导航 */
  onNavigate?: (path: string) => void;
}

export interface DispatchContext {
  store: DslStore;
  formId?: string;
  values: FormValues;
  callbacks: EventCallbacks;
  /** 表单校验钩子：返回 false 阻止 submit */
  validateForm?: () => boolean;
}

export class EventDispatcher {
  private debounceTimers = new Map<string, ReturnType<typeof setTimeout>>();

  async dispatch(ev: DslEvent, ctx: DispatchContext): Promise<boolean> {
    // debounce：同一事件 id 防抖
    if (ev.debounceMs && ev.debounceMs > 0) {
      const key = ev.type + ":" + (ev.params?.action ?? "");
      const existing = this.debounceTimers.get(key);
      if (existing) clearTimeout(existing);
      await new Promise<void>((resolve) => {
        this.debounceTimers.set(
          key,
          setTimeout(() => {
            this.debounceTimers.delete(key);
            resolve();
          }, ev.debounceMs ?? 0),
        );
      });
    }

    // confirm：取消则中断
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

  private async handleSubmit(ev: DslEvent, ctx: DispatchContext): Promise<boolean> {
    if (ctx.validateForm && !ctx.validateForm()) {
      ctx.callbacks.onToast?.("表单校验未通过，请检查必填项", "error");
      return false;
    }
    const params = ev.params ?? {};
    const action = String(params.action ?? "");
    if (!action) {
      ctx.callbacks.onToast?.("submit 事件缺少 params.action", "error");
      return false;
    }
    if (!ctx.callbacks.onSubmit) return false;
    const result = await ctx.callbacks.onSubmit({ action, formId: ctx.formId, values: ctx.values });
    if (result.ok && ev.params?.onSuccess) {
      await this.dispatchChainItems(ev.params.onSuccess, ctx);
    }
    return result.ok;
  }

  private async handleApiCall(ev: DslEvent, ctx: DispatchContext): Promise<boolean> {
    const params = ev.params ?? {};
    if (!ctx.callbacks.onApiCall) return false;
    const result = await ctx.callbacks.onApiCall(params, ctx.values);
    if (result.ok && ev.params?.onSuccess) {
      await this.dispatchChainItems(ev.params.onSuccess, ctx);
    }
    return result.ok;
  }

  private handleSetValue(ev: DslEvent, ctx: DispatchContext): boolean {
    const params = ev.params ?? {};
    const field = String(params.field ?? "");
    if (!field) return false;
    ctx.store.set(`data.${field}`, params.value);
    return true;
  }

  private async handleChain(ev: DslEvent, ctx: DispatchContext): Promise<boolean> {
    const chain = ev.params?.chain;
    if (!Array.isArray(chain)) return false;
    for (const item of chain) {
      const sub: DslEvent = {
        type: item?.type ?? "click",
        handler: String(item?.handler ?? ""),
        params: item?.params,
        confirm: item?.confirm,
        debounceMs: item?.debounceMs,
      };
      const ok = await this.dispatch(sub, ctx);
      if (!ok) return false; // 失败/取消中断
    }
    return true;
  }

  private async dispatchChainItems(items: unknown, ctx: DispatchContext): Promise<void> {
    if (!Array.isArray(items)) return;
    for (const item of items) {
      if (!item || typeof item !== "object") continue;
      const rec = item as Record<string, any>;
      const sub: DslEvent = {
        type: rec.type ?? "click",
        handler: String(rec.handler ?? ""),
        params: rec.params,
        confirm: rec.confirm,
        debounceMs: rec.debounceMs,
      };
      await this.dispatch(sub, ctx);
    }
  }

  private handleShowToast(ev: DslEvent, ctx: DispatchContext): boolean {
    const params = ev.params ?? {};
    ctx.callbacks.onToast?.(String(params.message ?? ""), String(params.severity ?? "info"));
    return true;
  }

  private handleNavigate(ev: DslEvent, ctx: DispatchContext): boolean {
    const params = ev.params ?? {};
    const path = String(params.path ?? "");
    if (!path) return false;
    ctx.callbacks.onNavigate?.(path);
    return true;
  }
}
