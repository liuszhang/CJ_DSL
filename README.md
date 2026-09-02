# CJDSL — DSL 驱动的新一代 Web 应用系统

> **核心理念**：界面不是写死的代码，而是大模型根据用户上下文与系统数据实时生成 DSL（JSON），再由统一渲染引擎动态呈现为 MudBlazor 组件树。

---

## 目录

- [系统概述](#系统概述)
- [核心原理](#核心原理)
- [架构设计](#架构设计)
- [技术栈](#技术栈)
- [项目结构](#项目结构)
- [运行流程](#运行流程)
- [DSL 规范](#dsl-规范)
- [渲染引擎](#渲染引擎)
- [LLM 集成](#llm-集成)
- [元模型体系](#元模型体系)
- [事件与状态管理](#事件与状态管理)
- [已支持的组件类型](#已支持的组件类型)
- [快速开始](#快速开始)

---

## 系统概述

CJDSL（**CJ DSL**）是一套基于 **声明式 DSL** 驱动的 Web 应用系统。与传统前端开发不同，CJDSL 不需要手写 UI 组件代码，而是：

1. **定义元模型**（业务实体、属性、状态、规则）
2. **LLM 自动生成 DSL**（JSON 格式的界面描述）
3. **渲染引擎动态呈现**（将 DSL 映射为 MudBlazor 组件）

当业务需求变更时，只需调整元模型或自然语言描述，LLM 即可重新生成 DSL，**无需修改前端代码、无需重新编译部署**。

---

## 核心原理

```
┌─────────────────────────────────────────────────────────────────┐
│                        传统前端开发                               │
│  需求变更 → 人工改代码 → 重新编译 → 发版上线                       │
└─────────────────────────────────────────────────────────────────┘

┌─────────────────────────────────────────────────────────────────┐
│                        CJDSL 开发模式                            │
│  需求变更 → LLM 重生成 DSL JSON → 渲染引擎即时呈现 → 无需发版      │
└─────────────────────────────────────────────────────────────────┘
```

### 三层抽象

| 层级 | 名称 | 职责 | 受众 |
|------|------|------|------|
| L3 | **DSL 声明层** | 描述"界面长什么样"（JSON） | 大模型 / 开发者 |
| L2 | **渲染引擎层** | 将 DSL 映射到 MudBlazor 组件 | 框架开发者 |
| L1 | **元模型层** | 描述"业务是什么" | 业务分析师 / 大模型 |

---

## 架构设计

```
┌──────────────────────────────────────────────────────────────┐
│                    客户端 (Blazor Server)                      │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────────────┐  │
│  │  DSL 渲染引擎  │  │  DslDataStore │  │  DslEventDispatcher│  │
│  │ (组件递归渲染) │  │  (状态管理)   │  │  (事件分发)        │  │
│  └──────┬───────┘  └──────┬───────┘  └─────────┬──────────┘  │
│         └─────────────────┴─────────────────────┘             │
│                           ▼                                   │
│              MudBlazor 组件树（动态构建）                       │
└──────────────────────────┬───────────────────────────────────┘
                           │ HTTP
                           ▼
┌──────────────────────────────────────────────────────────────┐
│                    服务端 (.NET 10)                            │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────────────┐  │
│  │  DSL 生成服务  │  │  元模型服务   │  │   业务 API 层      │  │
│  │ (LLM/模板)    │  │ (M0-M5)     │  │  (CQRS/MediatR)   │  │
│  └──────────────┘  └──────────────┘  └────────────────────┘  │
│  ┌──────────────┐  ┌──────────────┐  ┌────────────────────┐  │
│  │  LLM 适配器   │  │  表达式引擎   │  │   缓存层           │  │
│  │ (OpenAI/     │  │  (Jint)      │  │  (Memory)         │  │
│  │  Ollama)     │  │              │  │                   │  │
│  └──────────────┘  └──────────────┘  └────────────────────┘  │
└──────────────────────────────────────────────────────────────┘
```

### Clean Architecture 分层

| 项目 | 层级 | 职责 |
|------|------|------|
| `CJDSL.Domain` | 领域层 | DSL 实体、元模型、接口定义、值对象 |
| `CJDSL.Application` | 应用层 | CQRS 命令/查询、DTO、AutoMapper 映射 |
| `CJDSL.Infrastructure` | 基础设施层 | LLM 客户端、仓储实现、缓存、表达式引擎 |
| `CJDSL.Api` | 接口层 | Minimal API 端点 |
| `CJDSL.Blazor` | Blazor 共享层 | 渲染引擎组件、事件分发器、数据存储 |
| `CJDSL.Web` | Web 入口 | Blazor Server 主机、页面、布局 |

---

## 技术栈

| 技术 | 版本 | 用途 |
|------|------|------|
| .NET | 10.0 | 运行时 |
| Blazor Server | - | UI 渲染模式（InteractiveServer） |
| MudBlazor | 9.5.0 | UI 组件库 |
| MediatR | 14.1.0 | CQRS 命令/查询总线 |
| AutoMapper | 16.1.1 | 对象映射 |
| FluentValidation | 12.1.1 | 请求验证 |
| Jint | 4.10.0 | JavaScript 表达式求值引擎 |
| OpenAI API | - | LLM DSL 生成 |
| Ollama | - | 本地 LLM 适配 |

---

## 项目结构

```
CJDSL/
├── src/
│   ├── CJDSL.Domain/                    # 领域层
│   │   ├── Entities/
│   │   │   ├── Dsl/                     # DSL 核心实体
│   │   │   │   ├── DslPage.cs           # 页面根节点
│   │   │   │   ├── DslComponent.cs      # 组件节点（递归树）
│   │   │   │   ├── DslEvent.cs          # 事件定义
│   │   │   │   ├── DslDataSource.cs     # 数据源配置
│   │   │   │   ├── DslValidationRule.cs # 验证规则
│   │   │   │   ├── DslPermission.cs     # 权限控制
│   │   │   │   ├── DslResponsive.cs     # 响应式配置
│   │   │   │   └── DslStyle.cs          # 样式配置
│   │   │   └── MetaModel/               # 七维元模型
│   │   │       ├── M0_BasicData.cs      # M0: 枚举、数据字典
│   │   │       ├── M1_ObjectModel.cs    # M1: 对象、属性、状态
│   │   │       └── M2_M5_MetaModels.cs  # M2-M5: 行为、规则、场景、参与者
│   │   ├── Interfaces/                  # 仓储与服务接口
│   │   ├── ValueObjects/                # 值对象
│   │   └── Shared/                      # Result 模式
│   │
│   ├── CJDSL.Application/               # 应用层
│   │   └── Dsl/
│   │       ├── Commands/                # CQRS 命令
│   │       │   ├── GenerateDslCommand.cs
│   │       │   ├── GenerateDslFromNlpCommand.cs
│   │       │   └── AdaptDslCommand.cs
│   │       ├── Queries/                 # CQRS 查询
│   │       │   └── GetDslQuery.cs
│   │       ├── DslDto.cs                # 请求/响应 DTO
│   │       └── Mapping/                 # AutoMapper Profile
│   │
│   ├── CJDSL.Infrastructure/            # 基础设施层
│   │   ├── LLM/
│   │   │   ├── OpenAIClient.cs          # OpenAI 兼容 API 客户端
│   │   │   ├── OllamaClient.cs          # Ollama 本地 LLM 客户端
│   │   │   ├── DslPromptBuilder.cs      # LLM 提示词构建器
│   │   │   └── DslResponseParser.cs     # LLM JSON 响应解析器
│   │   ├── Services/
│   │   │   ├── TemplateDslGenerator.cs  # 模板 DSL 生成器
│   │   │   ├── LlmDslGenerator.cs       # LLM DSL 生成器
│   │   │   ├── DslSemanticValidator.cs  # DSL 语义验证器
│   │   │   ├── JintExpressionEvaluator.cs # 表达式求值引擎
│   │   │   ├── InMemoryMetaModelRepository.cs
│   │   │   └── InMemoryDslRepository.cs
│   │   └── Caching/
│   │       └── InMemoryDslCache.cs
│   │
│   ├── CJDSL.Blazor/                    # Blazor 共享层
│   │   ├── Components/Renderers/
│   │   │   ├── DslPageRenderer.razor    # 页面级根渲染器
│   │   │   ├── DslComponentRenderer.razor # 递归组件渲染器
│   │   │   ├── DslFileUploadRenderer.razor # 文件上传
│   │   │   ├── DslRichTextRenderer.razor # 富文本编辑器
│   │   │   └── DslChartRenderer.razor   # 图表
│   │   ├── Events/
│   │   │   └── DslEventDispatcher.cs    # 事件分发器
│   │   ├── Expressions/
│   │   │   └── ClientJintEvaluator.cs   # 客户端表达式引擎
│   │   ├── Models/
│   │   │   └── DslModels.cs             # 客户端 DSL 模型
│   │   └── wwwroot/js/
│   │       └── cjdsl-richtext.js        # 富文本 JS 互操作
│   │
│   ├── CJDSL.Api/                       # API 层
│   │   └── Endpoints/
│   │       └── DslEndpoints.cs          # Minimal API 路由
│   │
│   └── CJDSL.Web/                       # Web 入口
│       └── Components/
│           ├── App.razor
│           ├── Layout/
│           │   ├── MainLayout.razor
│           │   └── NavMenu.razor
│           └── Pages/
│               ├── Home.razor           # 首页
│               ├── DslTest.razor        # DSL 在线测试
│               ├── DslPreviewDialog.razor # DSL 预览弹窗
│               └── DslPage.razor        # 动态 DSL 页面
│
├── CJDSL.sln
└── Directory.Packages.props              # 中央包管理
```

---

## 运行流程

### 1. DSL 页面加载流程

```
用户访问 /dsl/{pageCode}
        │
        ▼
DslPage.razor 组件加载
        │
        ├──► 调用 GET /api/dsl/page/{pageCode}
        │         │
        │         ▼
        │    GetDslQuery Handler
        │         │
        │         ├──► 检查缓存 (InMemoryDslCache)
        │         │      命中 → 返回缓存 DSL
        │         │      未命中 ↓
        │         ├──► 从仓储加载 (InMemoryDslRepository)
        │         ├──► 写入缓存
        │         └──► 返回 DslPage JSON
        │
        ▼
反序列化为 DslPage 对象
        │
        ▼
DslPageRenderer 渲染
        │
        ├──► 创建 DslRenderContext (DataStore + User + ExpressionEvaluator)
        │
        ├──► 遍历 Components 列表
        │         │
        │         ▼
        │    DslComponentRenderer (递归)
        │         │
        │         ├──► 检查 VisibleIf → 表达式求值决定是否渲染
        │         ├──► 检查 DisabledIf → 决定是否禁用
        │         ├──► 根据 Type 匹配 MudBlazor 组件
        │         ├──► 解析 Props → 设置组件属性
        │         └──► 递归渲染 Children
        │
        ▼
    MudBlazor 组件树呈现
```

### 2. LLM DSL 生成流程

```
调用 POST /api/dsl/generate
        │
        ▼
GenerateDslCommand Handler
        │
        ├──► 构建缓存键 (metaObjectCode + layout + roles + device)
        ├──► 检查缓存
        │
        ├──► 加载元模型 (M1_Object)
        │
        ├──► 构建 LLM 提示词 (DslPromptBuilder)
        │    - 系统提示词：CJDSL Schema 规范
        │    - 用户提示词：业务对象属性、状态、用户角色
        │
        ├──► 调用 LLM (OpenAI / Ollama)
        │    - 温度: 0.3 (确定性输出)
        │    - 强制 JSON 输出
        │
        ├──► 解析响应 (DslResponseParser)
        │    - 清理 Markdown 代码块标记
        │    - 反序列化为 DslPage
        │
        ├──► 后处理
        │    - 注入数据绑定 (dataBind)
        │    - 注入验证规则 (validationRules)
        │    - 注入权限控制 (visibleIf / disabledIf)
        │    - 注入数据源 (dataSource)
        │
        ├──► 验证 DSL (DslSemanticValidator)
        │    - 组件类型合法性
        │    - Handler 合法性
        │    - 表达式语法
        │
        ├──► 写入缓存 (10分钟 TTL)
        │
        └──► 返回 DslPage JSON
```

### 3. 用户交互流程

```
用户点击按钮
        │
        ▼
DslComponentRenderer.HandleClick()
        │
        ▼
DslEventDispatcher.DispatchAsync()
        │
        ├──► 检查 DebounceMs (防抖)
        ├──► 检查 Confirm (确认对话框)
        │
        ├──► 根据 Handler 类型分发:
        │    ├── submit      → 表单提交
        │    ├── apiCall     → HTTP API 调用
        │    ├── navigate    → 页面跳转
        │    ├── showToast   → 消息提示
        │    ├── setValue    → 设置字段值
        │    ├── reset       → 重置表单
        │    ├── refresh     → 刷新数据
        │    ├── validate    → 执行验证
        │    └── chain       → 链式调用多个 handler
        │
        ▼
    更新 DslDataStore → 触发 StateHasChanged → UI 自动更新
```

---

## DSL 规范

### 页面结构

```json
{
  "id": "page_repair_form",
  "title": "设备报修单",
  "layout": "form",
  "permission": {
    "requiredRoles": ["operator", "admin"]
  },
  "components": [ ... ]
}
```

### 组件结构

```json
{
  "id": "field_name",
  "type": "text",
  "label": "字段标签",
  "fieldName": "fieldName",
  "dataBind": "@data.fieldName",
  "span": 6,
  "visibleIf": "user.roles.includes('admin')",
  "disabledIf": "data.status == 'archived'",
  "props": { "Required": true, "Variant": "Filled" },
  "children": [ ... ],
  "events": [
    { "type": "onClick", "handler": "apiCall", "params": { "endpoint": "/api/xxx" } }
  ],
  "validationRules": [
    { "type": "required", "message": "必填" }
  ],
  "dataSource": {
    "type": "dictionary",
    "code": "equipment_type"
  }
}
```

### 表达式引擎

DSL 中的 `visibleIf`、`disabledIf` 支持 JavaScript 语法，由 Jint 引擎在客户端执行：

```json
"visibleIf": "user.roles.includes('admin')"
"disabledIf": "data.status == 'completed' || data.status == 'cancelled'"
```

内置变量：
- `user` — 当前用户（roles, permissions）
- `data` — 表单数据
- `row` — 表格行数据
- `today` — 当前日期

---

## 渲染引擎

渲染引擎是 CJDSL 的核心，负责将 DSL JSON 动态转换为 MudBlazor 组件树。

### 核心组件

| 组件 | 文件 | 职责 |
|------|------|------|
| `DslPageRenderer` | `DslPageRenderer.razor` | 页面级根组件，创建渲染上下文 |
| `DslComponentRenderer` | `DslComponentRenderer.razor` | 递归组件渲染器，根据 `type` 分发 |
| `DslEventDispatcher` | `DslEventDispatcher.cs` | 事件分发器，处理所有 handler |
| `DslDataStore` | `DslModels.cs` | 客户端状态存储（类 Redux） |
| `ClientJintEvaluator` | `ClientJintEvaluator.cs` | 客户端表达式求值（Jint） |

### 渲染上下文

```csharp
DslRenderContext {
    Page: DslPage,              // 当前页面 DSL
    DataStore: DslDataStore,    // 数据存储
    User: UserContext,          // 用户上下文
    ExpressionEvaluator,        // 表达式引擎
    Forms: Dictionary,          // 表单状态
    RowData: object?,           // 当前行数据
    ComponentRefs: Dictionary   // 组件引用
}
```

---

## LLM 集成

### 多提供商适配

```
ILLMClient (接口)
    ├── OpenAIClient    → OpenAI / 兼容 API (DeepSeek, Moonshot 等)
    └── OllamaClient    → 本地 Ollama 服务
```

### 提示词工程

`DslPromptBuilder` 构建结构化提示词：
- **系统提示词**：CJDSL Schema 规范、组件类型映射、生成规则
- **用户提示词**：业务对象元模型、属性列表、生命周期状态、用户上下文

### 后处理流水线

LLM 生成的原始 DSL 经过：
1. JSON 解析与清理
2. 权限注入（基于用户角色设置 `visibleIf`）
3. 数据源绑定（字典/枚举字段自动配置 `dataSource`）
4. 验证规则注入（必填字段添加 `validationRules`）
5. 语义验证（组件类型、Handler 合法性检查）

---

## 元模型体系

七维元模型是业务知识的底座，驱动 UI、API、数据库、权限、流程的全栈一致性：

| 层级 | 名称 | 职责 |
|------|------|------|
| M0 | 基础数据模型 | 枚举、数据字典、量纲 |
| M1 | 对象模型 | 业务实体、属性、生命周期状态 |
| M1.5 | 关系模型 | 对象关联、继承、组合 |
| M2 | 行为模型 | 业务动作、前置条件、后置状态 |
| M3 | 规则模型 | 验证规则、计算规则、风控规则 |
| M4 | 场景模型 | 业务流程、用例、场景时间线 |
| M5 | 主体模型 | 参与者、角色、权限 |

---

## 事件与状态管理

### 事件处理器

| Handler | 说明 | 参数 |
|---------|------|------|
| `submit` | 表单提交 | formId, endpoint |
| `apiCall` | HTTP API 调用 | endpoint, method, formId, onSuccess |
| `navigate` | 页面跳转 | path |
| `showToast` | 消息提示 | message, severity |
| `setValue` | 设置字段值 | field, value |
| `reset` | 重置表单 | formId |
| `refresh` | 刷新数据 | targetId |
| `validate` | 执行验证 | formId |
| `chain` | 链式调用 | chain[] |

### 链式调用示例

```json
{
  "handler": "chain",
  "params": {
    "chain": [
      { "handler": "validate", "params": { "formId": "form1" } },
      { "handler": "apiCall", "params": { "endpoint": "/api/submit", "method": "POST" } },
      { "handler": "showToast", "params": { "message": "提交成功" } },
      { "handler": "navigate", "params": { "path": "/list" } }
    ]
  }
}
```

---

## 已支持的组件类型

### 表单组件
`text` · `number` · `select` · `autocomplete` · `textarea` · `date` · `datetime` · `time` · `switch` · `checkbox` · `radio` · `slider` · `rating` · `file` · `richText`

### 布局组件
`card` · `form` · `grid` · `stack` · `paper` · `divider` · `tabs` · `stepper` · `expansion` · `expansionPanel`

### 展示组件
`textDisplay` · `table` · `chip` · `badge` · `avatar` · `progress` · `skeleton` · `tooltip` · `pagination` · `alert` · `list` · `listItem` · `chart` · `flow`

### 溯源路径 flow（三端）

`type="flow"` 将溯源路径（PathJson）渲染为结构化有向链图：nodes/edges 逐跳展示、eliminated 已排除候选灰化虚线分组、节点卡片含 type 徽标 / note（截断 ≤30 字）/ 证据强度与路径置信度百分比色阶（≥0.7 绿 / 0.4-0.7 橙 / <0.4 红）。

- **Blazor**：`Models/FlowModels.cs`（FlowNode/FlowEdge/FlowEliminatedBranch）+ `Components/Renderers/FlowRenderer.cs`（ComponentType="flow"）
- **React**：`src/flow.ts`（FlowNode/FlowEdge/FlowEliminatedBranch/FlowProps + FLOW_LAYOUTS）+ `src/flow.tsx`（FlowView，DslRenderer.tsx switch 已注册 case "flow"）
- **WebComponent**：复用 React FlowView，无需独立实现；`highlightOnClick=true` 且未配置 navigate 类事件时，节点点击上抛 `cjdsl-action` CustomEvent：

```json
{
  "type": "flowNodeClick",
  "action": "flowNodeClick",
  "nodeId": "hop-0",
  "hop": 0,
  "instanceId": "a1b2c3d4-...",
  "relation": "evidence_of"
}
```

宿主监听 `cjdsl-action`（type=flowNodeClick）可接管跳转 / 图谱联动；`applyResult({ setValues })` 可回填联动字段。

### 交互组件
`button` · `iconButton`

---

## 快速开始

### 环境要求

- .NET 10 SDK
- Node.js (可选，用于前端工具链)

### 运行

```bash
# 克隆项目
git clone <repo-url>
cd CJDSL

# 还原依赖
dotnet restore

# 构建
dotnet build

# 运行 Web 项目
dotnet run --project src/CJDSL.Web
```

访问 `https://localhost:5001` 或 `http://localhost:5000`

### 页面说明

| 路由 | 页面 | 说明 |
|------|------|------|
| `/` | 首页 | 系统介绍、快速体验入口 |
| `/dsl-test` | DSL 测试 | 手动输入 DSL JSON 并实时预览 |
| `/dsl/{pageCode}` | DSL 页面 | 加载并渲染指定 DSL 页面 |
| `/swagger` | API 文档 | Swagger UI |

### DSL 测试

访问 `/dsl-test`，在左侧编辑区输入 DSL JSON，右侧实时渲染预览。点击"加载示例"可快速体验。

---

## 设计愿景

> **让大模型成为"界面架构师"**，让人类专注于业务逻辑与元模型设计，让机器处理繁琐的界面细节。

CJDSL 的终极目标是实现 **零前端代码** 的应用开发模式：业务人员描述需求，大模型生成 DSL，渲染引擎即时呈现，彻底消除前端开发的瓶颈。
