# CJDSL 改造实施计划（以 DA.DSH.PA 落地验证为目标）

> 配套方案：`D:\Pro\CJ.Plug.Github\CJDSL\docs\（已评审·修订中）CJDSL-Responsibility-Division.md`（7 项决策已敲定并修订生成形态，本文不再重议）
> 范围限定：CJDSL 核心改造 + ABWork `DA.DSH.PA` 端到端落地验证
> 纪律：本文为实施计划，不涉及代码改动（编码/构建由彦祖逐阶段触发）

---

## 0. 结论先行（TL;DR）

- **目标**：用 `DA.DSH.PA` 跑通最小闭环——**集中生成库（`CJDSL.Generation` 直接引用）+ 集中渲染（Web Component `<cjdsl-page>` bundle 单独项目产出）+ β 数据归属（`cjdsl-action` 回宿主落库）+ 标准化 CustomEvent**。
- **分期**：A 核心改造（CJDSL 侧）→ B 落地验证（DA.DSH.PA 侧）→ C 端到端验收。
- **关键判断**：`CJDSL.React` 的 `DslRenderer` 是 React 绑定，但其 `dsl/expr/store/events/validate/svg/payload/api` 已是**框架无关 vanilla TS**，可直接作为 Web Component 底座——**MVP 走"包裹 DslRenderer 进 Custom Element"（路线 A），后续择机去 React（路线 B）**，无需从零重写。
- **§8 全部决策已定稿**（2026-08-24），计划可进入立项执行。

---

## 1. 目标与范围

### 1.1 目标
验证方案文档的 7 项决策在真实产品（ABWork 的 DSH 插件链路）上端到端可行，暴露落地风险，形成可复用的改造范式供 CJOEM/CJOntology 推广。

### 1.2 在范围
- CJDSL 侧：Web Component 渲染器产出（单独 bundle 项目）、`CJDSL.Generation` 生成库抽离、CustomEvent 契约固化。
- DA.DSH.PA 侧：切 Web Component 渲染、接 `cjdsl-action` 桥接、β 落库、引用 `CJDSL.Generation` 库本地生成。

### 1.3 不在范围（本次）
- CJOEM / CJOntology 改造（阶段 2 推广，本文仅沉淀范式）。
- DSL 契约 SemVer 版本化（方案 5.7 单活版本已定）。
- 生产级生成库 LLM 凭证分发/轮换体系（MVP 各产品经 CJCore 配置提供，沿用既有体系）。

---

## 2. 现状基线（精简，均来自代码调研）

### 2.1 CJDSL.React（`src/CJDSL.React`）
- `DslRenderer.tsx`：React 专属，唯一真正产 DOM 的渲染器。
- `dsl/expr/store/events/validate/svg/payload/api.ts`：**框架无关 vanilla TS**，可直接复用为 Web Component 底座。
- `build.cjs` 双产物：`lib/cjs/index.js`（ESM）+ `lib/client.js`（CJS，包进 `window.__ModuleLoader__`）。
- 现状：生成目前走 `CJDSL.Web /api/dsl/generate-from-nlp` 端点（未加鉴权）；改造后改为 `CJDSL.Generation` 库直接引用，端点仅留作内部自测。

### 2.2 DA.DSH.PA（`ABWork/.../Clients/DSH/DA.DSH.PA`）
- MAUI Windows 外壳，拉起 DSH Web（node + `@deepseek-ai/dsh`，WebView2 承载）。
- 间接经 cordis 插件 `DA.DSHPlug.CJDSL`（Node/TS）使用 `@cj/cjdsl-react`（alias 到 CJDSL.React 源码）。
- `DA.DSHPlug.CJDSL/src/api.ts` 已有 `/api/cjdsl/{validate,submit,datasource,action}`，**submit/action 当前为 echo 占位**，带 `registerActionHandler` 扩展点。
- `index.ts` 的 `generateDslFromIntent` 已 POST `CJDSL_GENERATE_URL/api/dsl/generate-from-nlp`，但 **`CJDSL_GENERATE_URL` 当前未配置**（降级为模型直出 DSL）。
- **缺口**：`cjdsl-action` CustomEvent 不存在（events.ts 直接同域 fetch）；渲染分散在 React slot；生成端点无 Token。

---

## 3. 总体改造路线

```
阶段 A（CJDSL 核心改造）
  A1 Web Component 渲染器（路线A包裹 / 路线B去React演进）
  A2 抽离 CJDSL.Generation 生成库（规则+LLM+后处理+验证）
  A3 bundle 单独项目产出（CJDSL.WebComponent）
  A4 标准化 CustomEvent 契约固化
        │
阶段 B（DA.DSH.PA 落地验证）
  B1 切 Web Component 渲染（注入 <cjdsl-page> + 集中 bundle）
  B2 接 cjdsl-action 桥接（window 监听 → /api/cjdsl/action）
  B3 β 落库（action echo → registerActionHandler 真实持久化）
  B4 引用 CJDSL.Generation 库本地生成（替代原 HTTP 调 CJDSL.Web）
        │
阶段 C（端到端验收）
  C1 跑通 MVP 闭环 + 验收标准逐项核对
```

---

## 4. 阶段 A — CJDSL 核心改造

### A1 Web Component 渲染器（已被 A3.1「单独项目」取代，本节仅保留演进路线记录）
> 说明：按 §8.2「单独项目产出 bundle」决策，A1.1/A1.2 原「在 `CJDSL.React` 内加 `web-component.ts` + 第三产物 `lib/web-component.js`」的做法**不再单独实施**，改由 A3.1 新建独立项目 `CJDSL.WebComponent` 统一产出 `<cjdsl-page>` bundle。本节仅保留演进路线 B 记录。
- **A1.3（演进 路线 B，本期不做）** 未来在 `CJDSL.WebComponent` 内新增 `WebComponentRenderer.ts`（vanilla DOM：createElement + addEventListener），复用 vanilla 核心，**去掉 React 依赖**，产出真正框架无关渲染器（bundle 体积进一步优化）。

### A2 抽离 CJDSL.Generation 生成库
- **A2.1** 从 `CJDSL.Web` / `ABWork` 本地 Infrastructure 抽离生成逻辑为独立库 `CJDSL.Generation`（规则 `TemplateDslGenerator` / LLM `LlmDslGenerator`，未配置自动降级；后处理流水线；语义验证）。
- **A2.2** LLM 传输委托 CJCore（`IStructuredLLMClient`）；LLM 凭证由各产品经自身 CJCore 配置提供（无 HTTP 服务，无 Service Token）。
- **【实施记录 2026-08-23】A2.1/A2.2 已完成**：新建 `src/CJDSL.Generation`（net10 类库），迁入全部生成能力——`TemplateDslGenerator`/`LlmDslGenerator`/`DslGeneratorResolver`/`DslPromptBuilder`/`DbConfiguredLLMClient`（规则+LLM）、`DslSemanticValidator`/`DslSecurityValidator`/`JintExpressionEvaluator`/`InMemoryDslCache`/`SystemConfigService`（后处理+验证+缓存）、MediatR 三命令（`GenerateDslFromNlpCommand`/`GenerateDslCommand`/`AdaptDslCommand`）；并暴露 `AddCJDSLGeneration()` 注册扩展 + 高层门面 `IDslGenerationService.GenerateFromNlpAsync(...)`（各产品直接调用，不碰 MediatR）。`CJDSL.Infrastructure` 收敛为纯持久化（`AddCJDSLPersistence`），`CJDSL.Application` 仅留 Query/DTO/Mapping，`CJDSL.Web` 改为 `AddCJDSLGeneration()+AddCJDSLPersistence()`。生成接口（`IDslGenerator` 等）留 `CJDSL.Domain.Interfaces`，最小破坏。
  - **跨产品影响（下一步必做）**：凡曾 `AddCJDSLInfrastructure()` 的产品（已知 **ABWork** 的 `DA.DSH.PA` 等）须改为 `AddCJDSLGeneration()+AddCJDSLPersistence()`，且生成相关 `using CJDSL.Infrastructure.Services/LLM/Configuration` 改为 `CJDSL.Generation.*`；否则无法编译（属计划阶段 B 范围，本步仅完成 CJDSL 核心侧）。

### A3 bundle 单独项目产出
- **A3.1** 新建 `CJDSL.WebComponent`（npm `@cj/cjdsl-web-component`）独立项目，集中构建 `<cjdsl-page>` bundle；各产品直接引用（`<script src=".../cjdsl-page.js">` 或 npm import），作为单一权威源。
  - **【实施记录 2026-08-24】A3.1 已完成**：项目位于 `src/CJDSL.WebComponent`。`build.cjs` 产出双 bundle：
    - `dist/cjdsl-page.js`：**IIFE 全局脚本**，react/react-dom/client/@cj/cjdsl-react 全部打进 bundle，`<script>` 直接加载即用，**零 React 依赖**（各产品无需装 React）—— 主交付、单一权威源。
    - `dist/cjdsl-page.esm.js`：ESM 版，external react（供打包器 import）。
  - 核心源码：
    - `src/cjdsl-page.ts`：`class CjdslPage extends HTMLElement`，shadow DOM 内用 `react-dom/client` 挂载 `DslRenderer`；观测 `dsl`/`context` 属性变化重渲染；桥接 `onSubmit/onApiCall/onToast/onNavigate` → 对外 `cjdsl-action` CustomEvent；挂载完成 dispatch `cjdsl-ready`；暴露 `applyResult()` 供宿主回传。
    - `src/types.ts`：`CjdslActionDetail`/`CjdslContext`/`CjdslReadyDetail`/`CjdslApplyResult` 契约类型。
    - `src/index.ts`：`customElements.define('cjdsl-page', CjdslPage)` 自动注册 + `defineCjdslPage()` 幂等注册函数 + 类型导出 + `window.defineCjdslPage`。
  - 对外契约（与 A4 合并实现）：
    - 事件名：`cjdsl-action`（业务动作）/ `cjdsl-ready`（挂载完成）。
    - `cjdsl-action` detail（`CjdslActionDetail`）：`{ type, action, objectCode, data, apiParams?, context? }`，`type` 复用 DSL 事件 handler 语义（submit/apiCall/navigate/toast/...）。
    - 宿主回传：`<cjdsl-page>.applyResult({ ok, message, severity, setValues, refresh })`。

### A4 标准化 CustomEvent 契约固化（已由 A3.1 实现）
- **A4.1** Web Component 内部把 `onSubmit`/`onApiCall`/`onToast`/`onNavigate` 统一桥接为 `dispatchEvent(new CustomEvent('cjdsl-action', { detail }))`，不再直接后端 fetch；detail 见 `CjdslActionDetail`（`src/CJDSL.WebComponent/src/types.ts`）。
- **A4.2** 桥接契约 TS 类型已随 A3.1 落地于 `src/CJDSL.WebComponent/src/types.ts`，复用 DSL 事件 handler 语义。各宿主只需 `addEventListener('cjdsl-action', ...)` 即可接。

---

## 5. 阶段 B — DA.DSH.PA 落地验证

### B1 切 Web Component 渲染
- **B1.1** `DA.DSHPlug.CJDSL/src/client/index.tsx`：保留 cordis 插件身份，新增在 DSH 页面注入 `<script src="集中bundle URL">` + `<cjdsl-page dsl="…">`；**直接移除**旧 `CjdslToolCard`/`ChatDslNode` React slot，全切 Web Component（§8.7 已定直接替换）。

### B2 接 cjdsl-action 桥接
- **B2.1** `client/index.tsx` 内 `window.addEventListener('cjdsl-action', e => fetch('/api/cjdsl/action',{method:'POST',headers:{...},body:JSON.stringify(e.detail)}))`。**监听必须落在我们的 client bundle 内**（DSH 第三方 host 页面不可改，见 §8.4）。

### B3 β 落库
- **B3.1** `api.ts` 的 `/api/cjdsl/action` 从 echo 改为 `registerActionHandler` 真实持久化（MVP 可先用本地文件/内存占位，证明 β 闭环）；落库归属见 §8.1。

### B4 引用 CJDSL.Generation 库本地生成
> **2026-08-24 决策变更**：原 B4.1「MAUI 内嵌 Kestrel 承载 CJDSL.Generation」已撤销（用户要求不要内嵌 Kestrel、不要做成服务）；
> 改为在 **CJDSL 项目内新建 `CJDSL.Generation.TS`**（`src/CJDSL.Generation.TS`，npm 包 `@cj/cjdsl-generation-ts`），
> 作为**静态工具库**形态：LLM 凭证由调用方显式传入（`generateFromNlp(text, {apiKey, baseUrl, model})`），
> 零常驻服务、不内嵌 Kestrel、不 HTTP 调 CJDSL.Web。DA.DSHPlug.CJDSL 通过 build.mjs alias 引用其源码。
- **B4.1** `DA.DSHPlug.CJDSL` 引用 CJDSL 官方静态生成库 `@cj/cjdsl-generation-ts`（位于 `CJDSL/src/CJDSL.Generation.TS`），进程内直接 `generateFromNlp(intent, creds)`，不再 HTTP 调 `CJDSL.Web`、不再内嵌 Kestrel。
- **B4.2** 去掉模型直出兜底，统一走 `@cj/cjdsl-generation-ts` 静态库生成（§8.5 已定仅库生成）；生成库凭证缺失时直接报错，不引导模型直出 DSL。

---

## 6. 阶段 C — 端到端验收（MVP 闭环）

### 6.1 验证步骤
1. 启 `CJDSL.WebComponent` 构建产出 bundle + `CJDSL.Generation` 库就绪（供 DA.DSH.PA 引用）。
2. 启 `DA.DSH.PA`（MAUI → DSH Web → 加载 `DA.DSHPlug.CJDSL`）。
3. DSH 触发 `cjdsl_render` → 看到 `<cjdsl-page>` 渲染的卡片。
4. 点提交按钮 → 宿主收到 `cjdsl-action` → `/api/cjdsl/action` 落库成功回执。
5. 触发生成 → `DA.DSHPlug.CJDSL` 进程内调用 `@cj/cjdsl-generation-ts` 静态库（凭证由 pluginConfig/环境变量注入）产出 DSL（查本地日志确认）。

### 6.2 验收标准（逐条核对）
- [ ] `<cjdsl-page>` 以 Web Component 形态正常渲染（非 React slot 直出）。
- [ ] 业务动作经 `cjdsl-action` 回宿主，而非 Web Component 内直接同域 fetch。
- [ ] `/api/cjdsl/action` 真实落库（非 echo）。
- [ ] `DA.DSHPlug.CJDSL` 通过 `@cj/cjdsl-generation-ts` 静态库本地生成 DSL（非 HTTP 调 CJDSL.Web、非 MAUI 内嵌 Kestrel）。
- [ ] 生成统一走 `@cj/cjdsl-generation-ts` 静态库，已去除模型直出兜底通道（§8.5 已定仅库生成）。

---

## 7. 任务清单（执行用）

| 编号 | 归属 | 任务 | 关键文件 | 状态 |
|---|---|---|---|---|
| A1.1 | CJDSL | 新增 `web-component.ts`（于 CJDSL.React 内包裹 DslRenderer） | → **已并入 A3.1**（改在独立项目 CJDSL.WebComponent 实现，符合 §8.2） | — | 已并入 A3.1 |
| A1.2 | CJDSL | `build.cjs` 增第三产物（于 CJDSL.React 内） | → **已并入 A3.1**（改由 CJDSL.WebComponent/build.cjs 双产物） | — | 已并入 A3.1 |
| A1.3 | CJDSL | （演进 路线 B）vanilla `WebComponentRenderer.ts` 去 React | `src/CJDSL.WebComponent/src/WebComponentRenderer.ts` | 待办（本期不做） |
| A2.1 | CJDSL | 抽离 `CJDSL.Generation` 生成库 | `src/CJDSL.Generation`（新建） | 已完成 |
| A2.2 | CJDSL | 库内 LLM 委托 CJCore + 凭证经各产品 CJCore 配置 | `src/CJDSL.Generation` | 已完成 |
| A3.1 | CJDSL | 新建 bundle 产出项目 `CJDSL.WebComponent` | `src/CJDSL.WebComponent`（新建） | 已完成 |
| A4.1 | CJDSL | Web Component 桥接 `cjdsl-action` | `src/CJDSL.WebComponent/src/cjdsl-page.ts` | 已完成（由 A3.1） |
| A4.2 | CJDSL | 桥接契约 TS 类型 | `src/CJDSL.WebComponent/src/types.ts` | 已完成 |
| B1.1 | ABWork | 注入 `<cjdsl-page>` + 集中 bundle（旧 slot 共存） | `DA.DSHPlug.CJDSL/src/client/index.tsx` | 待办 |
| B2.1 | ABWork | `window.addEventListener('cjdsl-action')` → `/api/cjdsl/action` | `DA.DSHPlug.CJDSL/src/client/index.tsx` | 待办 |
| B3.1 | ABWork | `/api/cjdsl/action` echo → `registerActionHandler` 落库 | `DA.DSHPlug.CJDSL/src/api.ts` | 待办 |
| B4.1 | ABWork | 引用 `CJDSL.Generation` 库本地生成（替代 HTTP） | `DA.DSHPlug.CJDSL/src/index.ts` | 待办 |
| B4.2 | ABWork | 保留模型直出 DSL 兜底 | `DA.DSHPlug.CJDSL/src/index.ts` | 待办 |
| C1 | 验证 | 跑通 MVP 闭环 + 验收标准核对 | — | 待办 |

---

## 8. 风险与待决策项（需彦祖拍板）

| # | 项 | 优先级 | 推荐 | 说明 |
|---|---|---|---|---|
| 8.1 | β「宿主自身后端」落库归属 | 高 | **(a) DSH node `/api/cjdsl/action` 接真实持久化**（零新增进程）**【已定 a】** | 备选 (b) DA.DSH.PA 新增轻量 .NET Kestrel 经 `window.dshpaNative`/postMessage 转发。DA.DSH.PA 无 .NET 后端，现仅有 DSH node 与知识大脑 :3002 |
| 8.2 | bundle 托管方 | 中 | **单独项目产出 Web Component bundle，各产品直接引用（npm `@cj/cjdsl-web-component` / 包引用），不走 CJDSL.Web 托管**【已定】 | CJDSL.Web 为内部测试项目，不作对外载体；原 (a) CJDSL.Web/wwwroot 与 (b) CDN 均弃用 |
| 8.3 | 生成库 LLM 凭证提供方式（原 Service Token 已撤销） | 高 | **各产品经自身 CJCore 配置提供 LLM 凭证**（无 HTTP 服务，无 Service Token）**【已定，见方案 §5.6】** | 生成改为库直接引用后，跨服务令牌鉴权前提消失；凭证沿用各产品既有 CJCore 体系 |
| 8.4 | DSH 第三方 host 不可改 | 中（已规避） | `cjdsl-action` 监听落 `DA.DSHPlug.CJDSL` client bundle 内 | 已确认无法改 `@deepseek-ai/dsh` host 页 |
| 8.5 | 生成双通道是否并存 | 低 | **仅库生成**（去掉模型直出兜底，统一走 CJDSL.Generation 库）**【已定：仅库生成】** | 更纯净单一；库/LLM 故障则 DSH 失能，依赖全产品 CI + 单活版本兜底纪律 |
| 8.6 | `DA.DSHPlug.CJDSL/src/dsl.ts` 重复副本 | 低 | **删除副本，统一 import `@cj/cjdsl-react` 的 `validateDsl`**【已定：删除副本】 | alias 已能在构建期把 CJDSL.React 源码打进 bundle，离线可用 |
| 8.7 | Web Component 是否替代旧 slot | 中 | **直接替换**（移除旧 React slot，全切 `<cjdsl-page>`）**【已定：直接替换】** | 一步到位更干净；改动面大但验证通过即终态，无共存维护成本 |
| 8.8 | 集中生成服务的物理形态（由 8.2 引申） | 高 | **各产品直接引用 `CJDSL.Generation` 库（进程内跑），不走独立 HTTP 服务**【已定】 | CJDSL.Web 为内部测试项目，不作对外生产服务；生成能力封装为库，各产品 ProjectReference/npm 引用，本地跑规则+LLM+后处理+验证 |

### 方案级修正（由 8.2 / 8.8 引申，已定；方案文档已同步修订为「已评审·修订中」）
- 推翻已评审方案文档「生成集中化到 CJDSL.Web（独立 HTTP 服务）」设定。改为：**生成 = 各产品直接引用的 `CJDSL.Generation` 库（运行分散在各产品进程），渲染 = 各产品直接引用的 Web Component 包**。CJDSL.Web 退居纯内部设计/自测器。
- 方案文档已修订：`§3 架构图`、`§4.1 CJDSL.Web 职责`、`§5.1 生成集中化`、`§5.6 鉴权（撤销 Service Token，改为生成库凭证）`、`§7 实施路线` 均与新形态一致。
- `§8.3 Service Token` 已撤销并重定性为「生成库的 LLM 凭证如何提供给各产品」（方案 §5.6 已答：各产品经 CJCore 配置）。

---

---

## 9. 配套纪律（引用方案 5.7）

- 单活版本（永远最新）下，任何 DSL 模型 / 渲染器 / 桥接契约变更，须经**全产品 CI 回归** + **DSL 变更评审** + **预发环境契约校验**。
- 本验证仅动 DA.DSH.PA 与 CJDSL 两仓，回归时须覆盖 CJOEM/CJOntology 现有 DSL 渲染冒烟测试，防止单活版本连带破坏。

---

## 附录：关键文件索引

- CJDSL：`src/CJDSL.React/src/{DslRenderer.tsx,events.ts,store.ts,api.ts,dsl.ts,build.cjs}`、`src/CJDSL.Web`（内部设计/自测器）、`src/CJDSL.Generation`（新建生成库）、`src/CJDSL.WebComponent`（新建 bundle 产出项目）
- ABWork：`decentralized-agent/Clients/DSH/DA.DSH.PA/DA.DSH.PA.csproj`、`DA.DSH.PA/Services/DshHostService.cs`、`DA.DSH.PA/MainPage.xaml.cs`
- ABWork 插件：`decentralized-agent/Clients/DSH/DA.DSHPlug.CJDSL/src/{index.ts,api.ts,client/index.tsx,build.mjs}`、`decentralized-agent/Clients/DSH/dsh-profile-cj/profile/cordis.patch.yml`
