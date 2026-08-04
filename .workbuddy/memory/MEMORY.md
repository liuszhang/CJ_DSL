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
