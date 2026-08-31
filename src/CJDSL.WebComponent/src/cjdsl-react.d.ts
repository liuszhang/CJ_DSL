// @cj/cjdsl-react 模块类型声明（补丁：CJDSL.React 仅产出 JS，未产出 lib/index.d.ts，tsc 无法解析类型）
//   仅影响类型检查，不参与 esbuild 构建（构建经 reactAliasPlugin 解析到真实源码）。
//   签名对齐 CJDSL.React/src 源码：store.ts / events.ts / DslRenderer.tsx / dsl.ts。
declare module "@cj/cjdsl-react" {
  import type { ReactElement } from "react";

  export interface FormValues {
    [fieldName: string]: unknown;
  }

  export interface SubmitContext {
    action: string;
    formId?: string;
    values: FormValues;
  }

  export interface RendererCallbacks {
    mode?: string;
    onSubmit?: (
      ctx: SubmitContext,
    ) => Promise<{ ok: boolean; message?: string }> | { ok: boolean; message?: string };
    onApiCall?: (
      params: Record<string, any>,
      formValues: FormValues,
    ) => Promise<{ ok: boolean; message?: string; data?: unknown }> | { ok: boolean; message?: string; data?: unknown };
    onToast?: (message: string, severity?: string) => void;
    onNavigate?: (path: string) => void;
  }

  export interface DslNode {
    id?: string;
    [key: string]: any;
  }

  export class DslStore {
    constructor(initial?: Record<string, unknown>);
    get(key: string): unknown;
    set(key: string, value: unknown): void;
    merge(obj: Record<string, unknown>): void;
    snapshot(): Record<string, unknown>;
    subscribe(fn: () => void): () => void;
  }

  export function toDslNode(raw: unknown): DslNode | null;

  export function DslRenderer(props: {
    root: DslNode;
    store: DslStore;
    callbacks: RendererCallbacks;
  }): ReactElement | null;
}
