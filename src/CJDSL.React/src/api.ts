// CJDSL.React 数据后端抽象层
// 原 DSH 插件的 api.ts 直接 fetch `${location.origin}/api/cjdsl/*`，这把渲染器与具体宿主端口耦合。
// 这里抽出 CjdslApiClient 接口，默认提供一个基于同源 HTTP 的实现；DSH 等宿主可注入自己的实现。

export interface SubmitPayload {
  action: string;
  formId?: string;
  values: Record<string, unknown>;
}

export interface DatasourcePayload {
  type: "static" | "api" | "dictionary" | "enum";
  [key: string]: unknown;
}

export interface CjdslApiClient {
  /** 校验 DSL（POST JSON），返回是否通过及错误信息 */
  validateDsl(dsl: unknown): Promise<{ ok: boolean; errors?: string[] }>;
  /** 表单提交：{ action, formId, values } → { ok, message? } */
  submit(payload: SubmitPayload): Promise<{ ok: boolean; message?: string; result?: unknown }>;
  /** 数据源代理：{ type, ... } → { ok, data? } */
  datasource(source: DatasourcePayload): Promise<{ ok: boolean; data?: unknown; error?: string }>;
  /** 通用动作分发 */
  action(payload: { action: string; formId?: string; values?: Record<string, unknown> }): Promise<{ ok: boolean; message?: string; result?: unknown }>;
}

/** 默认实现：同源 /api/cjdsl/* */
export class HttpCjdslApiClient implements CjdslApiClient {
  private base: string;

  constructor(opts: { baseUrl?: string } = {}) {
    this.base = (opts.baseUrl ?? (typeof location !== "undefined" ? location.origin : "")).replace(/\/+$/, "");
  }

  private async handle<T>(res: Response): Promise<T> {
    if (!res.ok) {
      let detail = "";
      try {
        const body = await res.json();
        detail = (body as any)?.error || (body as any)?.message || JSON.stringify(body);
      } catch {
        detail = res.statusText;
      }
      throw new Error(`HTTP ${res.status}: ${detail}`);
    }
    return (await res.json()) as T;
  }

  validateDsl(dsl: unknown): Promise<{ ok: boolean; errors?: string[] }> {
    return fetch(`${this.base}/api/cjdsl/validate`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ dsl }),
    }).then((r) => this.handle(r));
  }

  submit(payload: SubmitPayload): Promise<{ ok: boolean; message?: string; result?: unknown }> {
    return fetch(`${this.base}/api/cjdsl/submit`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    }).then((r) => this.handle(r));
  }

  datasource(source: DatasourcePayload): Promise<{ ok: boolean; data?: unknown; error?: string }> {
    return fetch(`${this.base}/api/cjdsl/datasource`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify({ source }),
    }).then((r) => this.handle(r));
  }

  action(payload: { action: string; formId?: string; values?: Record<string, unknown> }): Promise<{ ok: boolean; message?: string; result?: unknown }> {
    return fetch(`${this.base}/api/cjdsl/action`, {
      method: "POST",
      headers: { "Content-Type": "application/json" },
      body: JSON.stringify(payload),
    }).then((r) => this.handle(r));
  }
}

/** 默认可用实例（同源）；宿主可覆盖为自定义实现 */
export const defaultApiClient: CjdslApiClient = new HttpCjdslApiClient();
