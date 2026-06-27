# CJDSL 项目概要

## 核心定位

CJDSL（CJ DSL）是一套基于 **声明式 DSL** 驱动的 Web 应用系统。与传统前端开发不同，CJDSL 不需要手写 UI 组件代码，而是：

1. **定义元模型**（业务实体、属性、状态、规则）
2. **LLM 自动生成 DSL**（JSON 格式的界面描述）
3. **渲染引擎动态呈现**（将 DSL 映射为 MudBlazor 组件树）

当业务需求变更时，只需调整元模型或自然语言描述，LLM 即可重新生成 DSL，**无需修改前端代码、无需重新编译部署**。

---

## 核心功能

| 功能 | 说明 |
|------|------|
| **三层抽象架构** | 元模型层（描述业务）→ DSL 声明层（JSON 描述界面）→ 渲染引擎层（映射 MudBlazor 组件） |
| **LLM 自动生成 DSL** | 基于元模型 + 用户上下文，由大模型生成界面 JSON，支持 OpenAI/Ollama 多提供商 |
| **递归渲染引擎** | `DslComponentRenderer` 递归解析 DSL 树，动态构建 MudBlazor 组件树，支持 40+ 组件类型 |
| **表达式引擎** | Jint 驱动的 `visibleIf`/`disabledIf` 条件渲染，支持 JavaScript 语法 |
| **事件分发系统** | 9 种预定义 Handler（submit/apiCall/navigate/setValue/chain 等），支持链式调用和防抖 |
| **七维元模型体系** | M0-M5 层级（基础数据→对象→关系→行为→规则→场景→主体），驱动 UI/API/DB 全栈一致性 |
| **状态管理** | `DslDataStore`（类 Redux）+ `DslEventDispatcher` 实现客户端状态流 |

---

## 项目亮点

- **零前端代码开发**：需求变更只需调整元模型或自然语言描述，LLM 重新生成 DSL，无需编译发版
- **Clean Architecture**：Domain/Application/Infrastructure/Api/Blazor/Web 六层分离，职责清晰
- **DSL 后处理流水线**：权限注入、数据源绑定、验证规则注入、语义验证，确保生成质量
- **多布局支持**：表单/列表/详情/仪表盘/自定义等布局模式
- **在线测试**：`/dsl-test` 页面支持实时编辑 DSL JSON 并预览渲染效果

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

## 架构分层

| 项目 | 层级 | 职责 |
|------|------|------|
| `CJDSL.Domain` | 领域层 | DSL 实体、元模型、接口定义、值对象 |
| `CJDSL.Application` | 应用层 | CQRS 命令/查询、DTO、AutoMapper 映射 |
| `CJDSL.Infrastructure` | 基础设施层 | LLM 客户端、仓储实现、缓存、表达式引擎 |
| `CJDSL.Api` | 接口层 | Minimal API 端点 |
| `CJDSL.Blazor` | Blazor 共享层 | 渲染引擎组件、事件分发器、数据存储 |
| `CJDSL.Web` | Web 入口 | Blazor Server 主机、页面、布局 |

---

## 设计愿景

> **让大模型成为"界面架构师"**，让人类专注于业务逻辑与元模型设计，让机器处理繁琐的界面细节。

CJDSL 的终极目标是实现 **零前端代码** 的应用开发模式：业务人员描述需求，大模型生成 DSL，渲染引擎即时呈现，彻底消除前端开发的瓶颈。
