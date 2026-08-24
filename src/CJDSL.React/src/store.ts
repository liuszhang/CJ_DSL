// 轻量 DslStore：Set/Get/Merge + 订阅（对齐 CJDSL DslDataStore 语义子集）
export type StoreListener = () => void;

function getPath(obj: Record<string, unknown>, path: string): unknown {
  if (path === "") return obj;
  const parts = path.split(".");
  let cur: unknown = obj;
  for (const p of parts) {
    if (cur === null || cur === undefined) return undefined;
    if (typeof cur !== "object") return undefined;
    cur = (cur as Record<string, unknown>)[p];
  }
  return cur;
}

export class DslStore {
  private data: Record<string, unknown> = {};
  private listeners = new Set<StoreListener>();

  get(key: string): unknown {
    if (key.startsWith("data.")) return getPath(this.data, key.slice(5));
    return getPath(this.data, key);
  }

  set(key: string, value: unknown): void {
    const normalized = key.startsWith("data.") ? key.slice(5)

 : key;
    const parts = normalized.split(".");
    let cur: Record<string, unknown> = this.data;
    for (let i = 0; i < parts.length - 1; i++) {
      const p = parts[i];
      if (typeof cur[p] !== "object" || cur[p] === null) cur[p] = {};
      cur = cur[p] as Record<string, unknown>;
    }
    cur[parts[parts.length - 1]] = value;
    this.emit();
  }

  merge(obj: Record<string, unknown>): void {
    for (const [k, v] of Object.entries(obj)) {
      if (k.startsWith("data.")) this.set(k, v);
      else this.set(`data.${k}`, v);
    }
  }

  snapshot(): Record<string, unknown> {
    return JSON.parse(JSON.stringify(this.data));
  }

  subscribe(fn: StoreListener): () => void {
    this.listeners.add(fn);
    return () => this.listeners.delete(fn);
  }

  private emit(): void {
    for (const fn of [...this.listeners]) {
      try {
        fn();
      } catch {
        /* 订阅者异常不影响状态更新 */
      }
    }
  }
}
