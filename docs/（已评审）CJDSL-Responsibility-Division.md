# CJDSL 跨产品职责划分总方案

> 状态：已评审（2026-08-23 设计树收敛）· **修订中**（2026-08-24 因两项新决策修正生成形态与鉴权）
> 配套记忆：`D:\Pro\CJ.Plug.Github\CJDSL\.workbuddy\memory\MEMORY.md` 的「CJDSL 跨产品职责划分决策」段

## 0. 修订记录（2026-08-24）

本次修订源于改造计划文档（设计中）推进时敲定的两项决策，原方案（生成集中化到 `CJDSL.Web` HTTP 服务、服务间令牌鉴权）因此与已定事实冲突，现拉回一致：

| 修订项 | 原方案 | 修订后 | 触发决策 |
|---|---|---|---|
| 生成能力物理形态 | `CJDSL.Web` 作独立 HTTP 生成服务，各产品 HTTP 调用 | **`CJDSL.Generation` 库**，各产品直接引用（ProjectReference / npm），进程内本地生成 | 彦祖：生成服务也走"产品直接引用某库"而非独立 HTTP 服务 |
| bundle 托管方 | `CJDSL.Web /wwwroot` 静态托管 | **单独项目产出**（暂名 `CJDSL.WebComponent`），各产品直接引用 | 彦祖：`CJDSL.Web` 是内部测试项目，不应作为对外 bundle 托管源 |
| `CJDSL.Web` 定位 | 集中生成 + 集中渲染引擎（生产） | **回归内部设计/自测器**，不复用为生产服务 | 同上两项推导 |
| 鉴权（决策 6） | 服务间令牌（Service Token + 限流） | **撤销**：无 HTTP 服务则无 Service Token；重新定性为"生成库 LLM 凭证由各产品经 CJCore 配置提供" | 同上推导 |

> 决策 1（生成集中化）、2（渲染集中化）、3（Web Component 化）、4（β 数据归属）、5（CustomEvent 桥接）、7（单活版本）维持不变，仅物理形态与鉴权措辞修订。

---

## 1. 背景与目标

### 1.1 现状痛点（调研结论）
- `CJOntology`：引用 `CJDSL.Blazor`（×5 项目），仅注册渲染器，生成/数据走自有 CJCore。
- `CJOEM`：引用 `CJDSL.Blazor`（×1），`AgentChatPanel` 用 `<DslPageRenderer>` 本地渲染。
- `ABWork`：整包吃 `Domain+Application+Infrastructure+Blazor`，本地 `AddCJDSLInfrastructure()`（生成+业务数据全本地跑）；DSH 侧用 `CJDSL.React` npm 本地打包。
- `CJPlug` / `Liuvis`：完全不引用 CJDSL（游离在版图外）。
- `CJDSL.Web`：独立应用，自带生成 + 业务 API，但**无任何兄弟产品把它当运行中的服务引用**；目标定位回归**内部设计/自测器**，不作为对外生产服务。

核心问题：各产品消费 CJDSL 的方式不统一；生成能力部署形态未定义；渲染重复实现。

### 1.2 目标
把"自然语言进 CJDSL → 转统一 DSL → 各产品渲染"变成一套统一、可复用的契约，消除重复实现，明确 CJDSL 与各产品的边界。

---

## 2. 总原则与对外契约

CJDSL 对外只暴露**两层契约**：

1. **统一语言层（Domain，稳定）**：`DslPage` / `DslComponent` / `DslEvent` / `DslDataSource` 等模型。所有产品与渲染器共享同一份 JSON Schema。
2. **能力层（可分体引用）**：
   - **生成器（集中为库）**：自然语言→DSL（规则/LLM 双路 + 后处理 + 语义验证），封装为 `CJDSL.Generation` 库，各产品直接引用、进程内运行。
   - **渲染器（集中为 bundle）**：框架无关 JS 渲染器，封装为 Web Component，由单独的 bundle 产出项目集中构建一份，各产品直接引用。
   - **验证/后处理（随生成库）**：权限注入、数据源绑定、语义验证，内置于 `CJDSL.Generation` 库。

---

## 3. 目标架构图

```
┌─────────────────────────────────────────────────────────────────┐
│  各产品（CJOntology / CJOEM / ABWork / 未来纳入者）= 瘦客户端      │
│  · 持有 <cjdsl-page> 容器（直接引用集中产出的 Web Component bundle）│
│  · 直接引用 CJDSL.Generation 库，进程内本地生成 DSL（LLM 经 CJCore）│
│  · 监听 cjdsl-action，把业务动作路由到「自己的」后端 API 落库      │
└───────────┬───────────────────────────────┬─────────────────────┘
            │ ① 本地引用生成库（进程内）       │ ② 直接引用 Web Component bundle
            ▼                                 ▼
┌────────────────────────────────┐   ┌──────────────────────────────┐
│  CJDSL.Generation（生成库）      │   │  CJDSL.WebComponent（bundle 产出）│
│  · 自然语言→DSL（模板+LLM 双路） │   │  · 框架无关 JS 渲染器          │
│  · 后处理 / 语义验证             │   │  · <cjdsl-page> Custom Element │
│  · 委托 CJCore 做 LLM 传输/结构化│   │  · 集中构建一份，各产品引用    │
└──────────────┬─────────────────┘   └──────────────────────────────┘
               │ 委托（库内调用）
         ┌─────▼──────┐
         │   CJCore   │  LLM 传输 / 结构化输出 / 数据层 / 账号权限
         └────────────┘

注：业务数据写在各产品自己的后端（β），CJDSL 不背业务数据。
    CJDSL.Web 仅作内部设计/自测器，不对外提供生成服务或 bundle 托管。
    CJDSL.Blazor 退居 CJDSL.Web 内部渲染/自测，对外统一走 Web Component。
```

---

## 4. 职责划分

### 4.1 CJDSL.Web（内部设计 / 自测器，非生产服务）
- **定位修正**：仅用于 CJDSL 团队内部设计 DSL、本地自测渲染/生成链路；**不作为对外生产生成服务，也不托管 Web Component bundle**。
- **内部使用**：保留 `CJDSL.Blazor` 仅作内部渲染/自测；可本地跑生成链路验证 `CJDSL.Generation` 库行为。
- **不负责**：各产品的业务数据持久化、各产品的用户登录体系、对外 bundle 分发。

### 4.2 集中能力产出方（对外）
- **`CJDSL.Generation`（生成库）**：
  - 提供自然语言→DSL（规则 `TemplateDslGenerator` / LLM `LlmDslGenerator`，未配置自动降级）；后处理流水线（权限注入、数据源绑定、验证规则注入、语义验证）。
  - 各产品以 ProjectReference（.NET）或 npm（前端）直接引用，在自身进程内调用。
  - LLM 传输委托 CJCore（`IStructuredLLMClient`）；LLM 凭证由各产品经 CJCore 配置提供。
- **`CJDSL.WebComponent`（bundle 产出项目，名称待最终敲定）**：
  - 集中构建框架无关 JS 渲染器，封装为 `<cjdsl-page>` Custom Element，产出单一 bundle。
  - 各产品直接引用该 bundle（npm 包 / 静态资源），加载同一份渲染资产。

### 4.3 各产品（宿主 / 瘦客户端）
- 嵌入 `<cjdsl-page dsl="…">` 或直接引用 bundle 后运行时 set DSL。
- **直接引用 `CJDSL.Generation` 库本地生成**（LLM 凭证经 CJCore 配置），不再 HTTP 调外部服务。
- 监听 `cjdsl-action`，按 `action` 路由到自身后端 API；把结果经 `el.applyResult(...)` 回传 Web Component。
- 提供用户上下文（UserContext：当前用户 / 权限 / 主题）注入 Web Component。

### 4.4 CJCore（共享内核）
- LLM 传输（`ILLMClient`）、结构化输出（`IStructuredLLMClient`）、数据层、账号权限。
- 被 `CJDSL.Generation` 库委托使用；各产品仍各自直接引用 CJCore 做自身业务。

---

## 5. 已敲定的 7 个设计决策（逐条）

### 5.1 生成集中化
- **决策**：生成（自然语言→DSL）逻辑**集中收敛为 `CJDSL.Generation` 库**，各产品直接引用（ProjectReference / npm），进程内本地运行，不再各自 `AddCJDSLInfrastructure()`，也不调独立 HTTP 服务。
- **理由**：避免每个产品重复接 CJCore LLM + 后处理 + 验证；DSL 统一语言的后处理/语义验证集中治理。库直接引用比 HTTP 服务部署更轻、无运行单点。
- **影响**：将现有生成逻辑从 `CJDSL.Web` / `ABWork` 本地 Infrastructure 抽离为独立库 `CJDSL.Generation`；各产品改为引用该库。

### 5.2 渲染集中化
- **决策**：渲染逻辑只维护一份（在集中的 Web Component bundle 产出项目），各产品不各自引用/编译 `CJDSL.Blazor` / `CJDSL.React`。
- **理由**：消除重复实现；与"统一语言"目标一致。
- **影响**：`CJDSL.Blazor` 退居 CJDSL.Web 内部自测；对外统一 Web Component。

### 5.3 集中渲染交付形态 = Web Component 化
- **决策**：渲染器做成框架无关 JS / Custom Element（`<cjdsl-page>`），由单独项目集中构建一份 bundle，各产品直接引用同一份嵌入。
- **理由**：真正"只维护一份"；技术栈无关，**顺带解决 Liuvis/CJPlug 因非 MudBlazor 进不来的老矛盾**。
- **代价**：现有 Blazor 渲染器要重构为框架无关 JS 渲染器（可借鉴 `CJDSL.React` 思路；其 `dsl/expr/store/events` 等已为 vanilla TS，可作底座）。

### 5.4 业务数据归属 = β
- **决策**：数据归属各产品。Web Component 把业务动作经 `CustomEvent` 抛回宿主，宿主调自己后端落库；CJDSL 不背业务数据。
- **理由**：CJOEM 已有 SQLite+Nginx、CJOntology 跑 CJCore 数据层，业务数据不可能迁进 CJDSL；α 会让 CJDSL 变成所有产品的数据库，耦合爆炸。

### 5.5 桥接契约 = 标准化 CustomEvent
- **决策**：固定事件 `cjdsl-action` / `cjdsl-ready` + payload `{action, objectCode, data, context}`；`action` 复用 `DslEventDispatcher` 的 9 种 handler 语义（apiCall/submit/validate/navigate/refresh/setvalue/export…）；宿主回传经 Web Component 暴露的 property/method（如 `el.applyResult(...)`）。
- **理由**：DSL 事件模型与对外契约统一成一套语言，各产品只实现一份事件路由。

### 5.6 生成库凭证提供方式（原"服务间令牌"决策已撤销）
- **背景**：原决策 6 的"服务间令牌（Service Token + 限流）"是为「独立 HTTP 生成服务」设计的。因生成形态改为**库直接引用（无 HTTP 服务、无跨进程调用）**，Service Token 前提不复存在，该决策撤销。
- **新定性**：`CJDSL.Generation` 库在**各产品进程内**运行，LLM 调用经 CJCore 的 `IStructuredLLMClient` 发出；**LLM 凭证由各产品自身的 CJCore 配置提供**（与产品既有 CJCore 用量一致），不存在跨服务调用方识别问题。
- **余留**：若未来某产品确实无法本地持有 LLM 凭证、必须远程生成，再单独评估"远程生成服务 + 鉴权"子方案（不在本方案默认形态内）。

### 5.7 DSL 契约版本 = 单活版本（永远最新）
- **决策**：DSL 模型（Domain）、`CJDSL.Generation` 库、Web Component bundle 不分版本，始终拉最新。
- **代价（已识别）**：breaking change 会全产品同时受影响，无版本隔离/灰度。
- **兜底纪律（必须遵守）**：
  1. 任何 DSL 模型 / 渲染器 / 桥接契约变更，须经**全产品 CI 回归**（各产品跑 DSL 渲染冒烟测试）。
  2. DSL 变更须走**变更评审**（影响面评估 + 各产品 owner 确认）。
  3. `CJDSL.Generation` 库 / bundle 发布前在预发环境跑全产品契约校验。

---

## 6. 各产品改造清单

| 产品 | 当前状态 | 改造动作 |
|---|---|---|
| **CJOEM** | 本地 `CJDSL.Blazor` + `AgentChatPanel` 用 `<DslPageRenderer>` | 移除本地 `CJDSL.Blazor` 引用与 `AddDslRenderers()`；`AgentChatPanel` 改嵌 `<cjdsl-page>`；直接引用 `CJDSL.Generation` 库本地生成；监听 `cjdsl-action` 路由到自身后端。**建议作首个试点** |
| **CJOntology** | 本地 `CJDSL.Blazor`（×5）+ 自有 CJCore 数据/LLM | 同上；`DynamicDimensionPage` / `DataDrawer` / `*DslService` 改消费 Web Component + 本地生成库；取消本地渲染器依赖 |
| **ABWork** | 本地 `AddCJDSLInfrastructure()` + 本地 Blazor/React | 去掉本地 Infrastructure 与本地渲染包；MAUI 用 WebView 嵌 `cjdsl-page`，DSH 侧直接引用集中 Web Component bundle；生成改引用 `CJDSL.Generation` 库 |
| **CJPlug** | 不引用 CJDSL | 评估是否纳入（若需 DSL 驱动的界面，直接嵌 Web Component + 引生成库，无需 MudBlazor） |
| **Liuvis** | 不引用 CJDSL（非 MudBlazor） | 集中渲染后技术栈绑定矛盾消除；评估是否纳入（GLB 设计工作室若需表单类 DSL 界面，可嵌 Web Component） |

**ABWork 是改造对象而非终态模板**：它当前本地整包吃 Infrastructure，集中化后必须改为引用 `CJDSL.Generation` 库，否则集中化被破坏。

---

## 7. 实施路线

- **阶段 0 — 集中能力剥离**
  - 从 `CJDSL.Web` / `ABWork` 抽离生成逻辑为独立库 `CJDSL.Generation`（规则+LLM 双路、后处理、语义验证、委托 CJCore）。
  - 新建 Web Component bundle 产出项目（暂名 `CJDSL.WebComponent`），封装框架无关 JS 渲染器为 `<cjdsl-page>`。
  - `CJDSL.Web` 回归内部设计/自测器，不再对外提供生成服务或 bundle。
- **阶段 1 — 客户端资产**
  - 发布 `CJDSL.Generation` 库（.NET 类库 + 前端 npm 封装）+ Web Component bundle + TS 类型 + 桥接文档（`cjdsl-action` 事件表）。
- **阶段 2 — 各产品改造（先试点后推广）**
  - CJOEM 试点 → CJOntology → ABWork。
- **阶段 3 — 版图扩展**
  - 评估 CJPlug / Liuvis 纳入。
- **配套纪律**：单活版本下的全产品 CI 回归 + DSL 变更评审（见 5.7）。

---

## 8. 风险与余留事项

| 风险 | 说明 | 缓解 |
|---|---|---|
| 单活版本无灰度 | breaking change 全产品同时炸 | 全产品 CI 回归 + 变更评审 + 预发契约校验（5.7） |
| Web Component 重构成本 | Blazor→JS 渲染器重写 | 借鉴 `CJDSL.React` 的 vanilla TS 底座；先覆盖高频组件，长尾渐进 |
| 集中资产单点 | 生成库 / bundle 版本统一，breaking change 影响全产品（非运行单点，因库直接引用） | 单活版本纪律兜底；bundle 走静态资源/CDN 缓存降级 |
| 业务数据回传延迟 | CustomEvent→宿主→自身后端→回传 Web Component 链路 | Web Component 内做 loading/乐观更新；宿主回传走 property/method 同步 |
| 生成库凭证 | 库进程内运行，LLM 凭证由各产品 CJCore 配置提供 | 沿用各产品既有 CJCore 凭证体系，无跨服务信任问题 |
| 契约（Domain）演进 | 模型变更需全产品同步 | 单活版本纪律兜底；长尾考虑未来迁移到 SemVer |

---

## 9. 行动项（建议）

1. 立项：剥离 `CJDSL.Generation` 库 + Web Component bundle 产出项目（阶段 0）。
2. 立项：发布 `CJDSL.Generation` 库 + Web Component bundle + 桥接文档（阶段 1）。
3. CJOEM 试点改造（阶段 2 起点）。
4. 建立单活版本兜底纪律（CI 回归流水线 + 变更评审流程）。

> 本方案为设计结论，不涉及代码改动（遵循"只编码不构建不运行"纪律，落地由彦祖逐阶段触发）。
