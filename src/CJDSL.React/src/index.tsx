// @cj/cjdsl-react 公共入口（client 侧）
//   导出 DSL 渲染器、store、事件、表达式、校验、SVG、载荷检测与后端抽象，
//   供宿主（如 DA.DSH.PA 的 DSH 插件）直接引用，避免本地重复维护渲染器副本。
export { DslRenderer, type DslNode, type RendererCallbacks } from "./DslRenderer";
export { DslStore } from "./store";
export { EventDispatcher, type DslEvent, type EventCallbacks, type FormValues, type SubmitContext } from "./events";
export { evalDslExpr } from "./expr";
export { validateField, type ValidationRule, type FieldValidationResult } from "./validate";
export { buildDonutSvg, type PieDatum } from "./svg";
export {
  detectDslPayload,
  detectDslPayloadInText,
  toDslNode,
  inferMode,
  extractJsonSubstring,
  extractJsonSpan,
  PAYLOAD_PREFIX,
  type DslPayloadDetect,
} from "./dslPayload";
export {
  HttpCjdslApiClient,
  defaultApiClient,
  type CjdslApiClient,
  type SubmitPayload,
  type DatasourcePayload,
} from "./api";
export { ChatDslNode } from "./ChatDslNode";
export { CjdslToolCard } from "./ToolCard";
export {
  validateDsl,
  parseDslText,
  V1_COMPONENT_TYPES,
  V1_EVENT_HANDLERS,
  V1_VALIDATION_RULES,
  V1_DATA_SOURCE_TYPES,
  type DslValidationResult,
  type DslComponent,
  type DslPage,
  type DslDataSource,
} from "./dsl";
