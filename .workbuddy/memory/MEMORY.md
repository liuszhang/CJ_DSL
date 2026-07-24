# CJDSL 项目长期记忆

## 构建环境（重要）

- SDK 为 .NET 10 预览版（10.0.400-preview）。Windows 下标准 `obj` 目录常被**遗留 dotnet 进程**锁定，导致 `rpswa.dswa.cache.json` / `*.dll` / `*.pdb` 写入 Access Denied（CS2012 / UnauthorizedAccessException），同时影响 CJDSL 与兄弟仓库 CJCore。
- 可靠规避：`dotnet build CJDSL.sln -c Debug --disable-build-servers --artifacts-path artifacts`，构建到独立目录绕开锁。
- 运行 Web：`dotnet artifacts/bin/CJDSL.Web/debug/CJDSL.Web.dll --urls http://localhost:5001`（需先把 `src/CJDSL.Web/wwwroot` 拷到 artifacts 输出目录；源码无 appsettings.json）。
- 本环境 `taskkill`/`Stop-Process`/`tasklist` 被安全策略拦截（LOLBin），无法杀遗留 dotnet 进程；彻底修复需用户在外部终止进程或重启。
- Blazor 项目已加 `<UseRazorBuildServer>false</UseRazorBuildServer>`；共享编译属性抽到 `Directory.Build.props`，`Directory.Packages.props` 仅留 CPM 版本。

## 关键设计约定

- 渲染上下文 `DslRenderContext`：每页一个，持有 `Forms`/`ComponentRefs`/`DataStore`/`EventDispatcher`；表单在 `OnParametersSet` 登记，`MudForm` ref 在 `OnAfterRender` 写入 `ComponentRefs`，submit/validate/reset 处理器据此取用。
- DSL 事件分发：`IDslEventDispatcher.DispatchAsync` 返回 false 表示链中断（如用户取消确认）。
- 业务数据端点：`/api/{objectCode}/{save|submit|list|item/{id}}`，默认内存存储（`UseSqlite=false` 时 `InMemoryBusinessDataService`），进程内有效；`objectCode` 走白名单正则且保留 `dsl` 前缀。
- LLM 生成：`IDslGeneratorResolver` 按 `provider` 选 Template/Llm，未配置 LLM 时自动降级模板。
