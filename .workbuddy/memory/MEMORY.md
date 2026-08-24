# CJDSL 项目长期记忆

## 构建环境（重要）

- SDK 为 .NET 10 预览版（10.0.400-preview）。Windows 下标准 `obj` 目录常被**遗留 dotnet 进程**锁定，导致 `rpswa.dswa.cache.json` / `*.dll` / `*.pdb` 写入 Access Denied（CS2012 / UnauthorizedAccessException），同时影响 CJDSL 与兄弟仓库 CJCore。
- 可靠规避：`dotnet build CJDSL.sln -c Debug --disable-build-servers --artifacts-path artifacts`，构建到独立目录绕开锁。
- 运行 Web：`dotnet artifacts/bin/CJDSL.Web/debug/CJDSL.Web.dll`（**Program.cs `UseUrls` 默认绑 http://localhost:5000 + https://localhost:5001**，http 与 https 必须分占不同端口，同端口不可同时开 http+https 监听；`--urls` 实测不覆盖代码内 `UseUrls`）。需先把 `src/CJDSL.Web/wwwroot` 拷到 artifacts 输出目录（自定义 artifacts 路径构建不会自动拷）；源码无 appsettings.json。**https 证书由 `ConfigureKestrel` 从本机受信任开发证书（CN=localhost，`dotnet dev-certs https --trust` 已信任）显式加载，因为 Production 环境下 Kestrel 不自动选开发证书。** 浏览器访问 **https://localhost:5001**（HSTS 会强制 https）；http://localhost:5000 为同应用 http 端口。内嵌 CJCore「LLM 配置」页的供应商/模型/MCP 接口服务端回连走 `http://localhost:5000/api/llm/*`（`ApiBaseUrl` 设 5000），必须保证应用监听 5000/5001，否则报「加载供应商失败: 连接被拒」。
- 本环境 `taskkill`/`Stop-Process`/`tasklist` 被安全策略拦截（LOLBin），无法杀遗留 dotnet 进程；彻底修复需用户在外部终止进程或重启。
- Blazor 项目已加 `<UseRazorBuildServer>false</UseRazorBuildServer>`；共享编译属性抽到 `Directory.Build.props`，`Directory.Packages.props` 仅留 CPM 版本。

## 关键设计约定

- 渲染上下文 `DslRenderContext`：每页一个，持有 `Forms`/`ComponentRefs`/`DataStore`/`EventDispatcher`；表单在 `OnParametersSet` 登记，`MudForm` ref 在 `OnAfterRender` 写入 `ComponentRefs`，submit/validate/reset 处理器据此取用。
- DSL 事件分发：`IDslEventDispatcher.DispatchAsync` 返回 false 表示链中断（如用户取消确认）。
- 业务数据端点：`/api/{objectCode}/{save|submit|list|item/{id}}`，默认内存存储（`UseSqlite=false` 时 `InMemoryBusinessDataService`），进程内有效；`objectCode` 走白名单正则且保留 `dsl` 前缀。
- LLM 生成：`IDslGeneratorResolver` 按 `provider` 选 Template/Llm，未配置 LLM 时自动降级模板。**模块 J 已完成全套收敛到 CJCore**——自建 LLM 客户端/解析器已删除，改用 `DbConfiguredLLMClient`（从 CJCore 数据层读默认模型）+ `IStructuredLLMClient`；配置页换用 CJCore `/llm-config`（旧 `ConfigLlm.razor` 已删，激活提供商经一次性幂等迁移进入 CJCore 数据层）。

## 组件库事实（MudBlazor 9.6.0）

- **无 `MudDateTimePicker` 组件**：可用 picker 仅 `MudDatePicker`/`MudTimePicker`/`MudDateRangePicker`/`MudColorPicker`。datetime 用 `MudDatePicker`+`MudTimePicker` 组合（代码里 `_dateValue.Date.Add(_timeValue)` 合并），已能完整取日期+时间。整改方案 G3 原写"改用 MudDateTimePicker"在该版本不可行，已改为保持组合控件。改 Razor 前先用反射/文档确认组件存在。

## CJDSL 跨产品职责划分决策（2026-08-23，与彦祖敲定）

- **总原则**：自然语言 → CJDSL（转 DSL 统一语言）→ 各产品渲染呈现。CJDSL 对外只暴露两层契约：①统一语言层（Domain 模型，稳定、需版本化治理）；②能力层（渲染器 + 生成器 + 验证）。
- **生成集中化（已定）**：生成（自然语言→DSL，规则/LLM 双路 + 后处理 + 语义验证）收敛为各产品**直接引用**的独立库 `CJDSL.Generation`（进程内本地生成，抽离自原 Infrastructure/Application）；**无独立 HTTP 生成服务、无 `CJDSL.Generation.Client`、无 Service Token**；各产品不再各自 `AddCJDSLInfrastructure()`，改为 `AddCJDSLGeneration()+AddCJDSLPersistence()`。
- **渲染集中化（已定）**：渲染 Web Component 化（框架无关 JS Custom Element `<cjdsl-page>`），bundle 由单独项目 `CJDSL.WebComponent` 集中构建、各产品直接引用；`CJDSL.Web` 退居内部设计/自测器（不托管 bundle）；各产品为瘦客户端持容器，**不再各自引用/编译 `CJDSL.Blazor` / `CJDSL.React` 本地渲染**。
- **ABWork 需跟随改造**：它当前 `AddCJDSLInfrastructure()` + 本地 `CJDSL.Blazor`/`CJDSL.React`，集中化后改为调集中生成服务 + 集中渲染，去掉本地 Infrastructure 与本地渲染包。
- **待决分叉点（设计树尚未敲定）**：
  1. 集中渲染的交付形态（已定）：**Web Component 化**（Custom Element，框架无关 JS 渲染器，集中构建一份 bundle，各产品加载同一份嵌入；解决 Liuvis/CJPlug 非 MudBlazor 绑定问题）。现有 `CJDSL.Blazor` 退居 CJDSL.Web 内部自测/服务内渲染，对外统一走 Web Component。
  2. 交互与业务数据归属（已定 β）：数据归属各产品。Web Component 把业务动作通过 `CustomEvent`（如 `cjdsl-action` 带 payload）抛回宿主，宿主调自己的后端 API 落库；CJDSL.Web 不背业务数据，保持纯生成+渲染引擎。
  3. Web Component ↔ 宿主桥接契约（已定）：标准化 CustomEvent 契约——固定事件 `cjdsl-action`/`cjdsl-ready` + payload schema `{action,objectCode,data,context}`；`action` 复用现有 `DslEventDispatcher` 的 9 种 handler 语义（apiCall/submit/validate/navigate/refresh/setvalue/export…）；宿主回传经 Web Component 暴露的 property/method（如 `el.applyResult(...)`）。CJDSL 提供 TS 类型 + 文档。
  4. 集中服务的鉴权与多调用方识别：因生成改为库直接引用（无 HTTP 服务），原「服务间令牌」决策**已撤销**；生成库的 LLM 凭证由各产品经**自身 CJCore 配置**提供，CJDSL.Web 不鉴权调用方。
  5. DSL 契约版本治理（已定）：**单活版本（永远最新）**——DSL 模型（Domain）与 Web Component bundle 不分版本，始终拉最新；CJDSL.Web 也永远最新。代价：breaking change 会全产品同时受影响，须以"全产品 CI 回归 + DSL 变更评审"纪律兜底（无版本隔离/灰度）。
- **游离在版图外的产品**：CJPlug / Liuvis 当前不引用 CJDSL；Liuvis 非 MudBlazor 技术栈，若纳入需解决渲染器技术栈绑定（集中渲染后此矛盾缓解，因各产品只持容器）。
