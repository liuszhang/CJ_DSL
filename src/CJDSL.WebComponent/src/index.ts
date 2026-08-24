// CJDSL.WebComponent 入口：自动注册 <cjdsl-page> Custom Element，并暴露编程式 API。
//   IIFE 产物（dist/cjdsl-page.js）加载即注册，各产品无需手动调用；
//   ESM 产物（dist/cjdsl-page.esm.js）同理，export 见下。
import { CjdslPage } from "./cjdsl-page";
import type {
  CjdslActionDetail,
  CjdslContext,
  CjdslReadyDetail,
  CjdslApplyResult,
} from "./types";

export const TAG_NAME = "cjdsl-page";

/** 注册 <cjdsl-page>（幂等，重复注册自动跳过），可指定自定义 tag */
export function defineCjdslPage(tag: string = TAG_NAME): void {
  if (typeof customElements === "undefined") return;
  if (!customElements.get(tag)) {
    customElements.define(tag, CjdslPage as CustomElementConstructor);
  }
}

// IIFE / ESM 加载即自动注册（符合「集中构建、直接引用」单一权威源定位）
defineCjdslPage();

export { CjdslPage };
export type {
  CjdslActionDetail,
  CjdslContext,
  CjdslReadyDetail,
  CjdslApplyResult,
};

declare global {
  interface Window {
    defineCjdslPage?: (tag?: string) => void;
  }
  interface HTMLElementTagNameMap {
    "cjdsl-page": CjdslPage;
  }
}

if (typeof window !== "undefined") {
  window.defineCjdslPage = defineCjdslPage;
}
