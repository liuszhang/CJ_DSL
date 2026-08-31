// CJDSL.WebComponent 对外桥接契约类型（对齐改造计划 §「cjdsl-action」标准化 CustomEvent）
//   事件名：cjdsl-action（业务动作）/ cjdsl-ready（挂载完成）
//   payload：{ action, objectCode, data, context }

/** 宿主透传给 Web Component 的用户上下文（UserContext），由各产品背书 */
export interface CjdslContext {
  userId?: string;
  tenantId?: string;
  locale?: string;
  /** 表单/业务对象编码，缺省时取 DSL 根节点 id */
  objectCode?: string;
  [k: string]: unknown;
}

/**
 * cjdsl-action CustomEvent 的 detail 结构。
 * type 复用 DSL 事件 9 种 handler 语义（submit/apiCall/navigate/refresh/setvalue/export/toast/showToast/...）。
 */
export interface CjdslActionDetail {
  /** 事件类型：submit | apiCall | navigate | toast | refresh | setValue | export | ... */
  type: string;
  /** 动作名（取自 DSL 事件的 params.action 或 submit 的 action） */
  action: string;
  /** 业务对象编码 */
  objectCode: string;
  /** 表单值 / 业务数据 */
  data?: unknown;
  /** apiCall 时的原始 DSL 参数（含 endpoint/target 等） */
  apiParams?: Record<string, unknown>;
  /** 宿主透传的用户上下文 */
  context?: CjdslContext;
}

/** cjdsl-ready CustomEvent 的 detail 结构 */
export interface CjdslReadyDetail {
  /** 元素 id（若有） */
  id?: string;
}

/** 宿主调用 <cjdsl-page>.applyResult(...) 的回传结构（方案：宿主回传经 Web Component 暴露的方法） */
export interface CjdslApplyResult {
  /** 业务操作是否成功（决定 toast 默认色） */
  ok?: boolean;
  /** 回显给用户的提示文案 */
  message?: string;
  /** 提示级别 */
  severity?: "info" | "success" | "warning" | "error";
  /** 回填到表单字段的值（key=value） */
  setValues?: Record<string, unknown>;
  /** 是否强制重新渲染（刷新关联数据源等） */
  refresh?: boolean;
}

/** cjdsl-json-view 浮层开关 CustomEvent 的 detail 结构（源 JSON 查看按钮，方案 §3.5） */
export interface CjdslJsonViewerDetail {
  /** 浮层是否打开 */
  open: boolean;
  /** 业务对象编码（缺省取 DSL 根节点 id / 用户上下文 objectCode） */
  objectCode?: string;
}
