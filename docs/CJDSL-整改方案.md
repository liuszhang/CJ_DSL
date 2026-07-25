---
AIGC:
    Label: "1"
    ContentProducer: 001191440300708461136T1XGW3
    ProduceID: 56ab3647c00a3b3fdf8fade204e7a8ee_d5e0956d874411f18766525400f8a581
    ReservedCode1: DxYhSnSw/GLMG5p24R1RQ2K6muX8sBrnkDTGxOqJNf4TZekt3XeaVFuzLwcWp0qNl9mZq6kRT2+fASb5zpQnYTLmLczJdZbLnbtGF3vOsI2t2mXqLRSqlN9P5oiWKvoJx3wk54iHUKtxlCDZT1vC9cU01i+Tb1wD0THWshjYwLrGZGPJ/TnVO7Jhhok=
    ContentPropagator: 001191440300708461136T1XGW3
    PropagateID: 56ab3647c00a3b3fdf8fade204e7a8ee_d5e0956d874411f18766525400f8a581
    ReservedCode2: DxYhSnSw/GLMG5p24R1RQ2K6muX8sBrnkDTGxOqJNf4TZekt3XeaVFuzLwcWp0qNl9mZq6kRT2+fASb5zpQnYTLmLczJdZbLnbtGF3vOsI2t2mXqLRSqlN9P5oiWKvoJx3wk54iHUKtxlCDZT1vC9cU01i+Tb1wD0THWshjYwLrGZGPJ/TnVO7Jhhok=
---

# CJDSL 项目整改方案

> 编制日期：2026-07-24
> 编制依据：[CJDSL-缺口分析与完善建议.md](./CJDSL-缺口分析与完善建议.md) + 源码逐项验证
> 验证范围：D:\Pro\CJ.Plug.Github\CJDSL\src 全部 7 个项目

---

## 1. 缺口总览

| 编号 | 缺口名称 | 严重程度 | 当前状态 | 整改目标 |
|------|----------|----------|----------|----------|
| G01 | LLM 生成链路三重脱节 | 🔴 P0 | 3 个端点均为硬桩/走模板 | 接通 LLM 全链路 |
| G02 | 核心 Handler 占位实现 | 🔴 P0 | submit/validate/confirm/export 等 7 个 Handler 只弹 toast | 实现真实交互逻辑 |
| G03 | 元模型驱动业务 API 缺失 | 🔴 P0 | 无任何 CRUD 业务端点 | 提供通用/生成式业务 API |
| G04 | 表单数据双向绑定未验证 | 🟠 P1 | 独立字段未回写 formState | 验证并修复绑定链路 |
| G05 | 安全机制整体缺失 | 🟠 P1 | 无沙箱/白名单/XSS 清洗 | 实现 DslSecurityValidator |
| G06 | 校验 allowlist 与渲染器不匹配 | 🟠 P1 | 校验通过 56 种，渲染仅 36 种 | 对齐校验与渲染能力 |
| G07 | Dashboard 布局空壳 | 🟠 P1 | 返回空 Components | 实现完整仪表盘生成 |
| G08 | datetime 组件 bug | 🟡 P2 | 使用 MudDatePicker 丢失时间 | 改用 MudDateTimePicker |
| G09 | 调试代码残留 | 🟡 P2 | Debug.WriteLine 生产环境中输出 | 清理或改为 ILogger |
| G10 | 零自动化测试 | 🟡 P2 | 无测试项目 | 建立测试体系 |
| G11 | Confirm 永远返回 true | 🟡 P2 | 仅弹 toast，安全隐患 | 接入 MudMessageBox |
| G12 | M2-M5 元模型未被消费 | 🟡 P2 | 实体类存在但无人使用 | 接入生成链路 |
| G13 | 设计文档模块未落地 | 🟡 P2 | SignalR/版本管理/渐进增强/向量库等缺失 | 分阶段补齐 |
| G14 | NLP 生成为脆弱正则 | 🟠 P1 | 固定句式正则匹配 | 对接 LLM 语义理解 |
| G15 | WPF 渲染器未文档化 | 🟡 P2 | 原型状态无文档 | 明确定位或完善 |
| G16 | export Handler stub | 🟡 P2 | 标注"导出功能开发中" | 实现 CSV/Excel 导出 |

---

## 2. 分模块整改计划

### 2.1 模块 A：LLM 生成链路（对应 G01、G14）

**现状**：
- `DslEndpoints.cs` 中 `/generate-from-nlp` 直接返回固定空壳（:42-51），不调用 MediatR
- `/adapt` 直接回显 `request.BaseDsl`（:54-60），不调用 AdaptDslCommand
- `/generate` 的 `IDslGenerator` 注册为 `TemplateDslGenerator`（`InfrastructureServiceExtensions.cs:45`），`LlmDslGenerator` 仅以具体类型注册（:46），实际不会被注入

**目标**：LLM 生成 DSL 成为默认/可选链路，三个端点全部可用。

**改动清单**：

| 序号 | 改动项 | 文件 | 说明 |
|------|--------|------|------|
| A1 | 修复 `/generate-from-nlp` | `DslEndpoints.cs` | 创建 `GenerateDslFromNlpCommand`（若不存在则新增），传入 `request.Description`、`request.UserContext` 等参数，经 MediatR 转到 `LlmDslGenerator.GenerateFromNlpAsync` |
| A2 | 修复 `/adapt` | `DslEndpoints.cs` | 创建 `AdaptDslCommand`，传入 `request.BaseDsl`、`request.UserContext`、`request.DataContext`，转到 `LlmDslGenerator.AdaptAsync` |
| A3 | 生成器注册策略 | `InfrastructureServiceExtensions.cs` | `IDslGenerator` 可选注册为 `LlmDslGenerator` 或提供 `provider` 参数（`?provider=llm|template`）在 `/generate` 中动态选择 |
| A4 | 新增 `GenerateDslFromNlpCommand` | `CJDSL.Application/Dsl/Commands/` | Command + Handler，注入 `ILLMClientProvider`/`IDslPromptBuilder`/`IDslResponseParser`，调用 LLM 生成 |
| A5 | 新增 `AdaptDslCommand` | `CJDSL.Application/Dsl/Commands/` | Command + Handler，注入 `LlmDslGenerator`，调用 `AdaptAsync` |
| A6 | LLM 配置文档 | `docs/` | 补充 LLM 配置说明（OpenAI/Ollama 的 appsettings 配置项） |

**预估工时**：3 人天

**验收标准**：
- 配置 LLM API Key 后，`POST /api/dsl/generate-from-nlp` 传入自然语言描述，返回由 LLM 生成的 DslPage JSON
- `POST /api/dsl/adapt` 基于用户角色/数据上下文，返回调整后的 DSL
- `POST /api/dsl/generate?provider=llm` 走 LLM 生成链路
- 不配置 LLM 时 `/generate` 回退到模板生成器，给出日志警告

---

### 2.2 模块 B：事件交互闭环（对应 G02、G11、G16）

**现状**（`DslEventDispatcher.cs`）：

| Handler | 当前实现 | 行号 |
|---------|----------|------|
| submit | 找到 form 后仅弹 toast | :128-135 |
| validate | 仅弹 "验证通过" toast | :183-186 |
| refresh | 仅弹 "刷新数据" toast | :153-156 |
| openModal | 仅弹 "打开模态框" toast | :143-147 |
| closeModal | 仅弹 "关闭模态框" toast | :148-151 |
| export | "导出功能开发中" stub | :178-181 |
| Confirm | 永远返回 true，无真实确认框 | :205-210 |

真实可用：apiCall / navigate / setValue / showToast / reset / chain。

**目标**：9 种 Handler 全部实现真实交互逻辑。

**改动清单**：

| 序号 | 改动项 | 文件 | 说明 |
|------|--------|------|------|
| B1 | 实现 `submit` | `DslEventDispatcher.cs` | 找到 form → 调用 `MudForm.Validate()` → 收集数据 → 触发链中后续 apiCall/navigate |
| B2 | 实现 `validate` | `DslEventDispatcher.cs` | 找到 form → 调用 `MudForm.Validate()` → 返回校验结果而非 toast |
| B3 | 实现 `openModal` | `DslEventDispatcher.cs` | 通过 `IDialogService.ShowAsync<DslDialog>` 打开弹窗，传入 DSL 内容 |
| B4 | 实现 `closeModal` | `DslEventDispatcher.cs` | 通过 `MudDialogInstance.Close()` 关闭当前弹窗 |
| B5 | 实现 `refresh` | `DslEventDispatcher.cs` | 触发 `StateHasChanged` 或调用数据源重新加载 |
| B6 | 实现 `export` | `DslEventDispatcher.cs` | 调用 `IJSRuntime` 触发浏览器下载（CSV/Excel），或生成 Blob 下载 |
| B7 | 实现真实 `Confirm` | `DslEventDispatcher.cs` | 替换 toast 为 `IDialogService.ShowMessageBox`，返回用户真实选择 |
| B8 | 新增 `DslDialog.razor` | `CJDSL.Blazor/Components/` | 模态框渲染组件，接收 DSL 片段并渲染 |

**预估工时**：4 人天

**验收标准**：
- 在报修单页面点击"提交"：弹出确认框 → 用户确认 → 调用 API → 成功后导航
- 点击"取消"按钮调用 `closeModal`：模态框关闭
- `export` 能导出表格数据为 CSV 文件
- `Confirm` 弹出 MudBlazor 原生确认对话框，用户点取消则中断后续链

---

### 2.3 模块 C：业务 API 闭环（对应 G03）

**现状**：`DslEndpoints.cs` 仅暴露 DSL 生成/查询/校验端点，无任何业务 CRUD API。模板生成器的 `apiCall` 指向 `/api/{code}/save` 等路径，但无对应端点。

**目标**：提供元模型驱动的通用 CRUD 端点或代码生成端点。

**改动清单**：

| 序号 | 改动项 | 文件 | 说明 |
|------|--------|------|------|
| C1 | 新增 `BusinessApiEndpoints.cs` | `CJDSL.Api/Endpoints/` | 通用端点：`POST /api/{code}/save`、`POST /api/{code}/submit`、`GET /api/{code}/list` |
| C2 | 新增 `IBusinessDataService` | `CJDSL.Domain/Interfaces/` | 接口：SaveAsync / QueryAsync / SubmitAsync |
| C3 | 实现 `InMemoryBusinessDataService` | `CJDSL.Infrastructure/Services/` | 内存实现（开发/演示用） |
| C4 | 实现 `SqliteBusinessDataService` | `CJDSL.Infrastructure/Persistence/` | SQLite 实现，与现有 `CJDSLDbContext` 配合 |
| C5 | 注册服务 | `InfrastructureServiceExtensions.cs` | DI 注册，根据配置选择 InMemory/SQLite |
| C6 | 数据 Schema 动态适配 | `BusinessApiEndpoints.cs` | 端点根据元模型动态接受/返回 JSON，不要求强类型 |

**预估工时**：5 人天

**验收标准**：
- 在报修单页面填写数据提交后，数据能持久化到 SQLite
- `GET /api/equipment/list` 返回设备列表
- 查询/保存端点根据元模型自动适配字段，无需手写代码

---

### 2.4 模块 D：表单数据绑定（对应 G04）

**现状**：`DslComponentRenderer.razor` 中每个输入组件的值存储在独立局部字段（`_stringValue`/`_decimalValue`/`_dateValue`/`_boolValue`），未见回写 `formState` 的逻辑。

**目标**：确保表单提交时 `apiCall` 能拿到用户真实输入。

**改动清单**：

| 序号 | 改动项 | 文件 | 说明 |
|------|--------|------|------|
| D1 | 核查绑定链路 | `DslComponentRenderer.razor` | 确认 `MudForm.EditContext` 是否自动收集字段值 |
| D2 | 添加 formState 回写 | `DslComponentRenderer.razor` | 若 MudForm 不自动回写，在 OnParametersSet 或变更事件中将值写入 `Context.Forms[formId]` |
| D3 | 端到端集成测试 | 手工/脚本 | 打开报修单 → 填写所有字段 → 点击提交 → 检查 API 请求体是否包含所有字段值 |

**预估工时**：2 人天

**验收标准**：
- 使用浏览器 DevTools 查看提交请求，payload 包含用户填写的所有字段及值
- 切换字段值后再次提交，payload 反映最新值

---

### 2.5 模块 E：安全机制（对应 G05）

**现状**：无安全检查。Jint 执行任意 JS 无沙箱，`apiCall` 无 endpoint 白名单，富文本无清洗。

**目标**：实现三层安全防护。

**改动清单**：

| 序号 | 改动项 | 文件 | 说明 |
|------|--------|------|------|
| E1 | 新增 `DslSecurityValidator` | `CJDSL.Infrastructure/Services/` | 实现 `IDslSecurityValidator` 接口 |
| E2 | Jint 沙箱配置 | `DslSecurityValidator.cs` | 设置执行超时（3s）、禁用 `System`/`IO`/`Net`/`Reflection` 等命名空间 |
| E3 | apiCall 白名单 | `DslSecurityValidator.cs` | 通过 `CJDSL:Security:AllowedEndpoints` 配置可调用的 endpoint 前缀 |
| E4 | 富文本 XSS 清洗 | `DslSecurityValidator.cs` | 使用 HtmlSanitizer 清洗 richText 内容 |
| E5 | 校验集成 | `DslEndpoints.cs` | 在 `/validate` 和 `/generate` 返回前调用安全校验 |
| E6 | 新增 `IDslSecurityValidator` | `CJDSL.Domain/Interfaces/` | 接口定义 |

**预估工时**：3 人天

**验收标准**：
- 包含 `while(true){}` 的 `visibleIf` 表达式被 Jint 超时拒绝
- DSL 中 `endpoint: "https://evil.com/steal"` 被白名单拦截
- 富文本中的 `<script>alert(1)</script>` 被清洗

---

### 2.6 模块 F：校验与渲染对齐（对应 G06）

**现状**：`DslSemanticValidator` 接受 56 种类型，渲染器 switch 约 32 种 + registry 8 种 = 约 36 种可用。20 种类型校验通过但渲染失败。

**缺失组件**：dataGrid、dialog、snackbar、markdown、appBar、drawer、breadcrumb、tree、timeline、carousel、colorPicker、jsonEditor、codeBlock、kanban、calendar、map、iframe、custom、fab

**目标**：两步走：先收紧校验，再逐步实现组件。

**改动清单**：

| 序号 | 改动项 | 文件 | 说明 |
|------|--------|------|------|
| F1 | 收紧 allowlist | `DslSemanticValidator.cs` | 将 `_validComponentTypes` 缩减为实际可渲染的 36 种，其余移入实验性列表并归类为 Warning |
| F2 | 补充 dialog 渲染 | `DslComponentRenderer.razor` | 基于 MudDialog 实现（与 B3 联动） |
| F3 | 补充 snackbar 渲染 | `DslComponentRenderer.razor` | 映射到 MudBlazor Snackbar 全局通知 |
| F4 | 补充 markdown 渲染 | `DslComponentRenderer.razor` | 使用 Markdown 渲染库或 MudBlazor Markdown 组件 |
| F5 | 其余组件分期 | — | dataGrid/tree/carousel 等列为 Phase 3 后续迭代 |

**预估工时**：3 人天（含 dialog/snackbar/markdown）

**验收标准**：
- 校验通过的所有类型在渲染器中均可渲染，不出现"未识别的组件类型"
- dialog 可在点击按钮后弹出
- markdown 文本正常渲染

---

### 2.7 模块 G：Dashboard 与组件修复（对应 G07、G08、G09）

**现状**：
- `TemplateDslGenerator.GenerateDashboardAsync` 返回空 Components
- `LlmDslGenerator.GenerateDashboardAsync` 返回硬编码占位
- `datetime` 用 `MudDatePicker` 丢失时间部分
- chart 分支含 `Debug.WriteLine`

**改动清单**：

| 序号 | 改动项 | 文件 | 说明 |
|------|--------|------|------|
| G1 | 实现 Dashboard 模板生成 | `TemplateDslGenerator.cs` | 基于 M4 场景/元模型统计生成卡片式仪表盘（统计卡片 + 图表 + 列表） |
| G2 | 实现 Dashboard LLM 生成 | `LlmDslGenerator.cs` | 传入 M4 场景结构，由 LLM 生成 Dashboard DSL |
| G3 | datetime 用 MudDatePicker+MudTimePicker 组合（MudBlazor 9.6.0 无 MudDateTimePicker 组件，组合已实现日期+时间取值，无需改） | `DslComponentRenderer.razor:83-95` | 保持组合控件，支持日期+时间选择 |
| G4 | 清理 Debug.WriteLine | `DslComponentRenderer.razor` | 替换为 `ILogger.LogDebug` 或删除 |

**预估工时**：2 人天

**验收标准**：
- Dashboard 生成返回含统计卡片和图表占位的完整 DSL
- datetime 组件可选择日期和时间

---

### 2.8 模块 H：测试体系（对应 G10）

**现状**：零测试。全仓无 `*.Tests` 项目。

**目标**：建立分层测试体系。

**改动清单**：

| 序号 | 改动项 | 说明 |
|------|--------|------|
| H1 | 新建 `CJDSL.Tests` 项目 | xUnit + Moq + FluentAssertions |
| H2 | 单元测试 - 模板生成器 | 覆盖 form/list/detail 生成，快照比对 DSL JSON |
| H3 | 单元测试 - 语义校验器 | 覆盖合法/非法组件、表达式语法、Handler 合法性 |
| H4 | 单元测试 - DslResponseParser | 覆盖 LLM 响应解析的边界情况 |
| H5 | 单元测试 - 表达式引擎 | 覆盖 visibleIf/disabledIf 各类表达式 |
| H6 | 集成测试 - 事件分发器 | Mock HTTP/MudBlazor 依赖，验证各 Handler 行为 |
| H7 | 端到端测试 | Playwright/Selenium 测试报修单完整流程 |

**预估工时**：5 人天

**验收标准**：
- 核心模块（生成器/校验器/解析器）单测覆盖率 > 70%
- CI 中集成测试通过

---

### 2.9 模块 J：LLM 模块收敛到 CJCore（新增，2026-07-25）

**现状**：CJDSL 在 `CJDSL.Infrastructure/LLM/` 自建了一整套 LLM 客户端（`ILLMClient`/`LLMClientProvider`/`OpenAIClient`/`OllamaClient`/`DslResponseParser`），与 CJCore 共享 LLM 模块（`src/Modules/LLM/` 9 个项目）完全平行——重复造轮子且能力更弱：无结构化输出、无自动重试、无流式/工具调用抽象；配置存 `system-config.json`，自建配置页 `/config/llm`。

**决策记录**（2026-07-25 用户拍板）：
- 收敛深度：**B 全套收敛**（客户端栈 + DB 配置 + CJCore 配置管理 UI），而非仅换客户端。
- 旧配置页处置：**删除** `ConfigLlm.razor` 与"LLM 源配置"菜单，全面换用 CJCore 的 `/llm-config` 配置页；`system-config.json` 中已激活 Provider 首次启动自动迁入 DB（幂等）。
- 关键事实：CJCore Data 模块是自包含独立 SQLite 库（`DataDbContext` + `EnsureDataDbCreatedAsync` + `ISeedDataProvider`），**不影响** CJDSL 现有 `UseSqlite=false` 内存业务存储。

**改动清单**：

| 序号 | 改动项 | 说明 |
|------|--------|------|
| J1 | 删除自建客户端栈 | 删 `ILLMClient.cs`（Domain）、`OpenAIClient`/`OllamaClient`/`LLMClientProvider`/`DslResponseParser`（Infra） |
| J2 | 引入 CJCore LLM 引用 | Infrastructure 引 `CJCore.LLM.Abstractions/LLMClient/Structured`；Web 引 `CJCore.Modules.LLM`（传递带 Model/Api/UI/Data） |
| J3 | DB 配置客户端适配器 | 新写 `DbConfiguredLLMClient : ILLMClient`：经 `ILLMConfigReader`(DB) 取 Endpoint/ApiKey/Model 填入 `ChatRequest`，委托 CJCore 通用 `LLMClient` |
| J4 | 改写 `LlmDslGenerator` | 用 `IStructuredLLMClient.SendStructuredAsync<DslPage>()` 取代"裸文本 + 手工解析"，失败仍走 fallback 页 |
| J5 | 改写 `DslGeneratorResolver` | 可用性判断从 `SystemConfig.GetActive()` 改为 DB 默认模型是否配置 |
| J6 | Web 宿主接入模块 | `AddCJCoreLLM(...)` + `MapCJCoreLLM()` + `EnsureDataDbCreatedAsync()` + `RunSeedDataAsync()`；UI 路由/菜单经 `IModule` 自动发现 |
| J7 | 旧配置页下线 + 配置迁库 | 删 `ConfigLlm.razor` 与菜单项；`ISeedDataProvider` 实现一次性幂等迁移 json→DB 并设默认模型 |
| J8 | 单测更新 | `SystemConfig.Llm` 保留类型但不再被消费；补 J3/J4 相关单测 |

**预估工时**：1.5 人天

**验收标准**：
- 全解构建 0 错误；既有单测全绿
- `/llm-config` 页面可管理 Provider/模型/MCP，设置的默认模型被 DSL 生成链路实际消费
- `provider=llm` 生成请求：DB 有默认模型走 LLM（含结构化解析），无则降级模板
- 旧 `/config/llm` 路由不存在；`system-config.json` 旧配置首启自动入库

---

### 2.10 模块 I：文档与规范（对应 G15、G13 部分）

**现状**：docs 仅含一份缺口分析文档。WPF 渲染器无文档，元模型使用指南缺失。

**改动清单**：

| 序号 | 改动项 | 说明 |
|------|--------|------|
| I1 | WPF 渲染器文档 | 明确原型定位、运行方式、与 Blazor 版差异 |
| I2 | LLM 配置指南 | appsettings 中 OpenAI/Ollama 的完整配置示例 |
| I3 | 元模型扩展指南 | 如何新增业务对象、枚举、属性的 step-by-step |
| I4 | API 参考文档 | 所有端点参数/响应/错误码 |

**预估工时**：2 人天

---

## 3. 实施路线图

### Phase 1：核心主张成立（1-2 周）

> **目标**：让"LLM 生成 DSL → 渲染 → 提交 → 持久化"的完整链路可用。

| 阶段 | 任务 | 工日 | 交付物 |
|------|------|------|--------|
| 1.1 | A1-A6：接通 LLM 生成链路 | 3 | `/generate-from-nlp` 和 `/adapt` 真实可用 |
| 1.2 | B1-B2, B7：实现 submit/validate/Confirm | 2 | 表单提交流程闭环 |
| 1.3 | C1-C6：业务 API 端点 | 5 | 报修单提交数据可持久化 |
| 1.4 | D1-D3：验证表单绑定 | 2 | 确认 apiCall 能拿到真实输入 |
| 1.5 | G3-G4：修复 datetime + 清理 debug | 0.5 | 代码整洁 |

**Phase 1 合计：12.5 人天**

**验收标准**：在报修单页面，用自然语言描述生成表单 → 渲染表单 → 填写数据 → 点击提交 → 数据持久化到 SQLite → 可查询。

---

### Phase 2：安全与可靠性（2-3 周）

> **目标**：系统从"可运行原型"升级为"可对外试用"。

| 阶段 | 任务 | 工日 | 交付物 |
|------|------|------|--------|
| 2.1 | E1-E6：安全机制 | 3 | DslSecurityValidator 上线 |
| 2.2 | F1-F4：校验/渲染对齐 + dialog/snackbar/markdown | 3 | 校验通过即渲染通过 |
| 2.3 | B3-B6, B8：openModal/closeModal/refresh/export | 2 | 全部 Handler 可用 |
| 2.4 | H1-H2：测试项目 + 模板生成器单测 | 2 | 基础测试框架就绪 |
| 2.5 | G1-G2：Dashboard 生成 | 1.5 | 仪表盘基础生成可用 |

**Phase 2 合计：11.5 人天**

**验收标准**：安全校验拦截恶意 DSL，所有 12 种 Handler 真实可用，Dashboard 生成返回有效内容。

---

### Phase 3：工程化与完善（3-4 周）

> **目标**：具备生产级质量，文档完善。

| 阶段 | 任务 | 工日 | 交付物 |
|------|------|------|--------|
| 3.0 | J1-J8：LLM 模块收敛到 CJCore | 1.5 | 客户端栈/配置/UI 全面复用 CJCore，删自建实现 |
| 3.1 | H3-H7：完整测试体系 | 3 | 单测覆盖率 > 70%，E2E 可跑 |
| 3.2 | I1-I4：文档补齐 | 2 | 完整的使用/配置/扩展文档 |
| 3.3 | F5：dataGrid/tree/carousel 等组件 | 5 | 常用组件补齐 |
| 3.4 | M2-M5 元模型消费（G12） | 3 | 规则引擎接入生成器 |
| 3.5 | 性能优化与压力测试 | 2 | 大表单/批量 DSL 生成性能可接受 |

**Phase 3 合计：15 人天**

---

### Phase 4：进阶特性（远期规划）

| 特性 | 说明 | 优先级 |
|------|------|--------|
| SignalR 热重载 | DSL 变更后无需刷新浏览器即时生效 | 中 |
| DSL 版本管理 | 页面 DSL 的版本历史与回滚 | 中 |
| 渐进增强引擎 | 根据设备能力动态降级组件 | 低 |
| Blazor WASM 支持 | 纯客户端渲染 | 低 |
| RAG 元模型推断 | 从自然语言描述推断业务模型 | 低 |
| AB 测试 | DSL 级别的 A/B 分流 | 低 |

---

## 4. 风险与注意事项

### 4.1 技术风险

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| Jint 沙箱不彻底 | 仍可绕过执行恶意代码 | 结合 CSP + 服务端表达式预校验 |
| LLM 生成 DSL 不稳定 | 返回格式不符合预期 | `DslResponseParser` 增加容错 + 回退到模板生成器 |
| SQLite 高并发瓶颈 | 多用户场景性能差 | 架构预留接口后续切换 PostgreSQL/MySQL |
| MudBlazor 组件限制 | 部分复杂组件无对应封装 | 自定义渲染器或接受功能裁剪 |
| Confirm 安全依赖前端 | 可被浏览器 DevTools 绕过 | 关键操作在服务端再次校验 |

### 4.2 执行风险

| 风险 | 影响 | 缓解措施 |
|------|------|----------|
| 表单绑定问题隐藏深 | D1-D3 可能发现需要大量重构 | 预留 2 天缓冲，必要时接受 formState 显式回写方案 |
| LLM API 不可用 | A1-A6 无法验证 | 保留模板生成器作为降级方案，LLM 功能通过 feature flag 控制 |
| 测试编写耗时超预期 | Phase 3 延期 | 优先覆盖 P0 链路（生成→渲染→提交），其余渐进补充 |
| 业务 API 动态 Schema 复杂度高 | 通用端点实现困难 | Phase 1 可先为报修单/设备写固定端点，通用的后续迭代 |

### 4.3 架构约束

- **不改分层架构**：现有 Clean Architecture 六层结构保持不动，所有改动在现有层次内完成
- **不改 DSL Schema**：现有 DslPage / DslComponent 的 JSON Schema 不破坏兼容性
- **优先补缺不重构**：已工作的模块（渲染器注册表、LLM 客户端、模板生成器）不做大规模重构
- **WPF 渲染器维持原型定位**：不在 Phase 1-3 中完善，仅补充文档说明

### 4.4 依赖清单

| 依赖 | 用途 | 引入时机 |
|------|------|----------|
| HtmlSanitizer | 富文本 XSS 清洗 | Phase 2 |
| xUnit / Moq / FluentAssertions | 测试框架 | Phase 2 |
| Playwright / Selenium | E2E 测试 | Phase 3 |
| Markdown 渲染库 | markdown 组件 | Phase 2 |

---

## 附录 A：组件类型对照表（校验 vs 渲染）

| 类型 | 校验器允许 | 渲染器实现 | 状态 |
|------|-----------|-----------|------|
| page | ✅ | ❌ | 去掉（框架级） |
| card | ✅ | ✅ (Registry) | OK |
| form | ✅ | ✅ | OK |
| text | ✅ | ✅ (Registry) | OK |
| number | ✅ | ✅ | OK |
| select | ✅ | ✅ (Registry) | OK |
| autocomplete | ✅ | ✅ | OK |
| textarea | ✅ | ✅ | OK |
| date | ✅ | ✅ | OK |
| datetime | ✅ | ✅ (bug) | 需修复 |
| time | ✅ | ✅ | OK |
| checkbox | ✅ | ✅ | OK |
| switch | ✅ | ✅ | OK |
| radio | ✅ | ✅ | OK |
| slider | ✅ | ✅ | OK |
| rating | ✅ | ✅ | OK |
| file | ✅ | ✅ | OK |
| button | ✅ | ✅ (Registry) | OK |
| iconButton | ✅ | ✅ | OK |
| fab | ✅ | ❌ | 缺 |
| table | ✅ | ✅ | OK |
| dataGrid | ✅ | ❌ | 缺 |
| list | ✅ | ✅ | OK |
| tabs | ✅ | ✅ | OK |
| stepper | ✅ | ✅ | OK |
| expansion | ✅ | ✅ | OK |
| dialog | ✅ | ❌ | 缺 |
| snackbar | ✅ | ❌ | 缺 |
| progress | ✅ | ✅ | OK |
| chart | ✅ | ✅ | OK |
| markdown | ✅ | ❌ | 缺 |
| grid | ✅ | ✅ (Registry) | OK |
| stack | ✅ | ✅ (Registry) | OK |
| paper | ✅ | ✅ | OK |
| divider | ✅ | ✅ (Registry) | OK |
| textDisplay | ✅ | ✅ (Registry) | OK |
| avatar | ✅ | ✅ | OK |
| chip | ✅ | ✅ | OK |
| badge | ✅ | ✅ | OK |
| tooltip | ✅ | ✅ | OK |
| skeleton | ✅ | ✅ | OK |
| appBar | ✅ | ❌ | 缺 |
| drawer | ✅ | ❌ | 缺 |
| breadcrumb | ✅ | ❌ | 缺 |
| pagination | ✅ | ✅ | OK |
| tree | ✅ | ❌ | 缺 |
| timeline | ✅ | ❌ | 缺 |
| carousel | ✅ | ❌ | 缺 |
| colorPicker | ✅ | ❌ | 缺 |
| richText | ✅ | ✅ | OK |
| jsonEditor | ✅ | ❌ | 缺 |
| codeBlock | ✅ | ❌ | 缺 |
| kanban | ✅ | ❌ | 缺 |
| calendar | ✅ | ❌ | 缺 |
| map | ✅ | ❌ | 缺 |
| iframe | ✅ | ❌ | 缺 |
| custom | ✅ | ❌ | 缺 |

**统计**：校验 56 种，渲染 36 种，缺口 20 种。Phase 2 补齐 dialog/snackbar/markdown（3 种），其余按需分期。

---

## 附录 B：Handler 实现状态

| Handler | 当前实现 | Phase 1 | Phase 2 | 最终 |
|---------|----------|---------|---------|------|
| apiCall | ✅ 完整 | — | — | ✅ |
| navigate | ✅ 完整 | — | — | ✅ |
| setValue | ✅ 完整 | — | — | ✅ |
| showToast | ✅ 完整 | — | — | ✅ |
| reset | ✅ 完整 | — | — | ✅ |
| chain | ✅ 完整 | — | — | ✅ |
| submit | ❌ 占位 | ✅ | — | ✅ |
| validate | ❌ 占位 | ✅ | — | ✅ |
| Confirm | ❌ 永远 true | ✅ | — | ✅ |
| openModal | ❌ 占位 | — | ✅ | ✅ |
| closeModal | ❌ 占位 | — | ✅ | ✅ |
| refresh | ❌ 占位 | — | ✅ | ✅ |
| export | ❌ stub | — | ✅ | ✅ |

---

## 附录 C：端点实现状态

| 端点 | 当前 | 目标 |
|------|------|------|
| `POST /api/dsl/generate` | 走模板 | 支持 `?provider=llm` 走 LLM |
| `POST /api/dsl/generate-from-nlp` | 硬桩返回空壳 | 调用 LLM 生成 |
| `POST /api/dsl/adapt` | 直接回显 | 调用 LLM 适配 |
| `GET /api/dsl/page/{code}` | ✅ | — |
| `POST /api/dsl/validate` | ✅ | 增加安全校验 |
| `POST /api/{code}/save` | ❌ 不存在 | 通用 CRUD |
| `POST /api/{code}/submit` | ❌ 不存在 | 通用 CRUD |
| `GET /api/{code}/list` | ❌ 不存在 | 通用 CRUD |
*（内容由AI生成，仅供参考）*
