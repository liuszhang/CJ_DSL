---
AIGC:
    Label: "1"
    ContentProducer: 001191440300708461136T1XGW3
    ProduceID: 56ab3647c00a3b3fdf8fade204e7a8ee_0984f68ea38e11f193c6525400f8a581
    ReservedCode1: PpMEdXjEdmbbj8EKUIQL+5bkwflKUQLIjgHOyZUGhIMhslexK5SRfLCM823AR5m4d1jVhSutBa/B55LxoDbnVpCeWbXeZpI+rp1kvbG4CBTBBZbZyklubpqG4yWd9IaWCpkeqJF/tKf1b5vfYX7FTYCWIIlEYhoQ8YpcYLnujR8vt7CtzjfaUv00n/w=
    ContentPropagator: 001191440300708461136T1XGW3
    PropagateID: 56ab3647c00a3b3fdf8fade204e7a8ee_0984f68ea38e11f193c6525400f8a581
    ReservedCode2: PpMEdXjEdmbbj8EKUIQL+5bkwflKUQLIjgHOyZUGhIMhslexK5SRfLCM823AR5m4d1jVhSutBa/B55LxoDbnVpCeWbXeZpI+rp1kvbG4CBTBBZbZyklubpqG4yWd9IaWCpkeqJF/tKf1b5vfYX7FTYCWIIlEYhoQ8YpcYLnujR8vt7CtzjfaUv00n/w=
---



# CJDSL 渲染界面源 JSON 查看按钮方案

> 版本：v1.0（方案评审稿）  
> 日期：2026-08-29  
> 范围：DSH 聊天界面中 CJDSL 动态界面渲染的统一源 JSON 查看入口  
> 关联项目：`DA.DSH.PA`（MAUI 宿主壳）、`DA.DSHPlug.CJDSL`（DSH 运行时插件）、`CJDSL.WebComponent`（集中式 Web Component 渲染器）、`CJDSL.React`（DslRenderer 递归渲染器）

---

## 1. 背景与目标

### 1.1 背景

CJDSL 动态界面已在 DSH 聊天流中稳定渲染（`conversation.chat.node` 的 `cjdsl` 节点与 `tool.call.toolview` 的 `cjdsl_render` 节点）。当前存在两个与"源码可追溯性"相关的现状：

1. **源码块被隐藏**：DSL 载荷以 ` ```dsl ` 代码块出现在 assistant 回复文本中，`dedupe.ts`（`hideDuplicateDslSource`）与 `ChatDslNode.tsx`（`hideAdjacentDslSources`）会把与渲染卡片内容等价的源码块隐藏，避免图表/表单与源码双显。用户因此无法直接看到该界面背后的 CJDSL 源 JSON。
2. **无统一查看入口**：除开发态手动打开控制台外，聊天界面缺少面向最终用户的"查看该界面源 JSON"能力，不利于界面排错、DSL 学习与插件调试。

### 1.2 目标

1. 在聊天界面中所有 CJDSL 渲染的界面**统一**在**右上角**增加一个图标按钮。
2. 按钮**鼠标悬停（划过）后显示**，点击后弹出/展示该界面对应的 **CJDSL 源 JSON** 内容。
3. 该按钮支持**通过参数快速关闭**（默认打开）。

### 1.3 非目标

- 不改变现有 CJDSL 渲染链路与 DSL 语法（v1 白名单不变）。
- 不恢复已被 `dedupe` 隐藏的 ` ```dsl ` 源码块（源码查看统一走新按钮，避免双显回退）。
- 不做 DSL 在线编辑/回放（仅查看）。

---

## 2. 现状调研结论

### 2.1 项目与代码位置总览

| 模块 | 绝对路径 | 职责 |
|---|---|---|
| DA.DSH.PA（MAUI 壳） | `D:\Pro\CJ.Plug.Github\ABWork\decentralized-agent\Clients\DSH\DA.DSH.PA` | WebView2 宿主壳：`MainPage.xaml` 承载 WebView；`MauiProgram.cs` 拉起 `KbHostService`/`DshHostService`；`DshNativeBridge.cs` 注入 `window.dshpaNative`（KB 预填、表单置灰持久化、文件选择）。聊天界面本身是 DSH 运行时 Web 前端，渲染在 WebView2 中，**不直接参与 CJDSL 渲染** |
| DA.DSHPlug.CJDSL（DSH 插件） | `D:\Pro\CJ.Plug.Github\ABWork\decentralized-agent\Clients\DSH\DA.DSHPlug.CJDSL` | **聊天界面 CJDSL 渲染的实际实现方**：宿主入口 `src/index.ts`（REST /api/cjdsl/*、cjdsl_render 工具、systemPrompt、配置）；客户端 `src/client/*`（slot 注册、载荷检测、桥接渲染、源码去重、表单预填/持久化） |
| CJDSL.WebComponent | `D:\Pro\CJ.Plug.Github\CJDSL\src\CJDSL.WebComponent` | **统一渲染层**：`<cjdsl-page>` Custom Element（`src/cjdsl-page.ts`），shadow DOM 内挂载 React `DslRenderer`；`build.cjs` 产出 `dist/cjdsl-page.js`（IIFE 全内置）与 `dist/cjdsl-page.esm.js`，产物拷贝至插件 `lib/cjdsl-page.js` 经 `/api/cjdsl/cjdsl-page.js` 提供 |
| CJDSL.React | `D:\Pro\CJ.Plug.Github\CJDSL\src\CJDSL.React` | `DslRenderer.tsx` 递归渲染 DSL 组件树；`ChatDslNode.tsx` 为独立的 React slot 渲染器（PersonalPA 等宿主使用，DSH 当前走 WebComponent 桥） |

### 2.2 聊天界面 CJDSL 渲染链路（流程）

```
LLM 回复含 ```dsl 载荷 / dsh-agent-network 插件注入 user/message
  │
  ▼
① conversationEvents 检测（DA.DSHPlug.CJDSL/src/client/payload.ts）
   cjdslPayloadDefinition.match：assistant/message 或 plugin 注入的 user/message，
   detectDslPayload 提取 { payload, dsl, mode, rawText }，按 id 缓存（防 start 阶段取不到）
  │
  ▼
② 节点构建：buildViewNode → kind=cjdsl 的 chat 节点，data={ payload, dsl, mode, rawText }
  │
  ▼
③ Slot 渲染（DA.DSHPlug.CJDSL/src/client/index.tsx）
   slots.inject("conversation.chat.node", key=cjdsl) 与
   slots.inject("tool.call.toolview", key=cjdsl_render) 均注册为 CjdslPageBridge
  │
  ▼
④ 桥接渲染（DA.DSHPlug.CJDSL/src/client/bridge.tsx）★ 本次方案的主要挂载点之一
   - 外层容器 div[data-cjdsl-chat-node]（白底、1px 边框、圆角 10、overflow:hidden）
   - hideDuplicateDslSource 隐藏相邻 assistant 行中等价的 ```dsl 源码块
   - 创建 <cjdsl-page> 元素并 setAttribute("dsl", JSON.stringify(dslObj)) / mode
   - 分支：loading 占位、无 DSL 文本回执透传、表单 KB 预填（patch 后重设 dsl 属性）、
     表单置灰持久化（cjdsl-submitted → dshpaNative.SaveFormStateAsync）、来源可信面板
  │
  ▼
⑤ 统一渲染层（CJDSL.WebComponent/src/cjdsl-page.ts）★ 本次方案的核心改动层
   <cjdsl-page> 构造时 attachShadow，内部 #cjdsl-mount 用 react-dom/client 挂载 DslRenderer；
   parseDsl → toDslNode → DslRenderer(root, store, callbacks) 渲染
  │
  ▼
⑥ 事件闭环：表单提交/API 调用/navigate/toast 统一 dispatch 为 cjdsl-action CustomEvent，
   客户端 index.tsx 在 window 监听并转发 /api/cjdsl/action，服务端回执经 applyResult 回灌
```

### 2.3 组件结构

| 组件 | 位置 | 说明 |
|---|---|---|
| `CjdslPageBridge` | `DA.DSHPlug.CJDSL/src/client/bridge.tsx` | React 函数组件；用 ref + effect 直接操作 DOM 创建 `<cjdsl-page>`；外层 `div[data-cjdsl-chat-node]` 是聊天流中可见的卡片容器 |
| `<cjdsl-page>` | `CJDSL.WebComponent/src/cjdsl-page.ts` | Custom Element（shadow DOM open）；observedAttributes = `dsl/context/submitted/values`；对外事件 `cjdsl-action`/`cjdsl-ready`/`cjdsl-submitted`；宿主回传 `applyResult` |
| `DslRenderer` | `CJDSL.React/src/DslRenderer.tsx` | 递归渲染 DSL 组件树；`DslStore` 承载状态；`EventDispatcher` 分发事件；白名单校验由 `dsl.ts`/`validate.ts` 提供 |
| 其它 client 模块 | `payload.ts / dedupe.ts / prefill.ts / persist.ts / sourcePanel.ts / actions.ts / bundle.ts` | 载荷检测、源码去重、KB 预填、置灰持久化、来源面板、回执应用、bundle 注入 |

### 2.4 现有源码块隐藏机制（与本次方案的直接关系）

- `dedupe.ts`：DSH 插件侧，卡片所在行前后各 5 行内隐藏与卡片 DSL 内容等价的 `div.md-code-block`。
- `ChatDslNode.tsx`：CJDSL.React 侧，`hideAdjacentDslSources` 对 `data-chat-flow-kind` 行的 assistant-step 做类似隐藏。
- **影响**：源码双显已被刻意消除，用户无法直接看到源 JSON。本方案提供的按钮正是面向该缺口设计的"受控源码查看入口"——点击按钮展示的 JSON 与 `dedupe` 判定等价时所用的 DSL 对象同源（`dsl` 属性值），语义一致，不构成双显回退。

### 2.5 可扩展点分析

| 扩展点 | 位置 | 与本方案的契合度 |
|---|---|---|
| `<cjdsl-page>` shadow DOM 内部（`#cjdsl-mount` 同级） | `cjdsl-page.ts` | **最高**：这是所有宿主共享的"统一渲染层"，在此加按钮可对所有使用 WebComponent 的产品（DSH、PersonalPA 路线 A 等）一次生效，满足"统一"要求 |
| `CjdslPageBridge` 外层容器 `div[data-cjdsl-chat-node]` | `bridge.tsx` | 中：仅 DSH 生效；但承担"读取插件配置/DSL meta 决定是否开启"的透传职责 |
| `DslRenderer` 内部（React 树） | `DslRenderer.tsx` | 低：会污染组件树、需为每个 DSL 节点注入，且 PersonalPA 的 DslMessageRenderer 等自有渲染器不共享 |
| 宿主 `MainPage.xaml` / `DshNativeBridge.cs` | DA.DSH.PA | 低：WebView 外层无 DOM 操作能力，仅当需要原生持久化（如"关闭状态记忆"）时才涉及 |

**结论**：核心实现放在 `CJDSL.WebComponent/src/cjdsl-page.ts`（统一、零宿主依赖）；`bridge.tsx` 与 `index.ts` 只做"配置透传 + 开关路由"。

---

## 3. 方案设计

### 3.1 总体设计

三层结构，职责分离：

```
┌─ ① 全局配置层（host 端 / DA.DSHPlug.CJDSL/src/index.ts）
│     CjdslConfig.jsonViewerEnabled = true（默认开，可经 pluginConfig 关闭）
│     启动时写入 window.__cjdslConfig = { jsonViewerEnabled }
│
├─ ② 渲染级属性层（CjdslPageBridge → <cjdsl-page>）
│     bridge.tsx 组合判定：全局开关 ∧ DSL meta.jsonViewer ≠ false
│     → 决定 setAttribute("json-viewer", "true"|"false")
│
└─ ③ 统一渲染层（CJDSL.WebComponent/src/cjdsl-page.ts）
      <cjdsl-page json-viewer="true|false">  shadow DOM 内渲染右上角按钮 + JSON 浮层
```

- **默认值**：`json-viewer` 缺省为 `"true"`（按钮打开）。任何一层显式关闭即关闭。
- **统一性**：按钮、浮层、交互全部实现在 `<cjdsl-page>` shadow DOM 内，不依赖 DSH 特有 DOM 结构；任何通过 `<cjdsl-page>` 渲染 CJDSL 的宿主自动获得该能力。
- **非 CJDSL 渲染不误伤**：`loading` 占位与"无 DSL 文本回执"分支不走 `<cjdsl-page>`，天然无按钮，符合"仅 CJDSL 渲染界面"的语义。

### 3.2 按钮交互

**按钮形态**
- 位置：卡片（`:host`）右上角，`position:absolute; top:8px; right:8px; z-index: 10`。
- 图标：内联 SVG 大括号 `{ }`（JSON 语义）或代码 `</>`，**不引入图标库**，保持 bundle 零依赖；备选 Unicode `{ }` 文本按钮。
- 尺寸：28×28px，半透明圆角，hover 时加深背景；`aria-label="查看 CJDSL 源 JSON"`，`title` 提示"查看源 JSON"。

**悬停显示（鼠标划过后显示）**
- CSS 实现：按钮默认 `opacity:0; pointer-events:none`；`:host(:hover)` 或 `:host(:focus-within)` 时 `opacity:1; pointer-events:auto`，过渡 `opacity .18s ease`。
- 效果：鼠标划入渲染界面（卡片任意区域）即显示右上角按钮，移出后淡出，不影响界面本体与表单交互（按钮仅在 hover 期间可点击，避免遮挡）。
- 键盘可达：`focus-within` 同步显示，Tab 聚焦按钮时可操作。

**点击弹出**
- 点击按钮 → 在卡片内部展开 JSON 浮层（`position:absolute; top:44px; right:8px; left:8px`），不阻塞聊天流滚动。
- 浮层关闭：再次点击按钮、浮层右上角关闭按钮、点击浮层外区域（shadow DOM 内监听 `click` 命中判断）、按 Esc。
- 按钮 `click` 事件需 `stopPropagation()`，避免冒泡触发外层 cjdsl-action 或聊天行事件。

### 3.3 JSON 展示方式

采用**卡片内嵌浮层（Popover）**为主，兼顾简单与不打断聊天流：

| 项 | 设计 |
|---|---|
| 内容 | 该卡片实际渲染所依据的完整 CJDSL 源 JSON，即 `<cjdsl-page>` 的 `dsl` 属性原始字符串（含预填 patch 后的值），`JSON.stringify(JSON.parse(raw), null, 2)` 格式化 |
| 原始性 | `parseDsl()` 时同步保存 `this.rawDslJson`（原始属性字符串），浮层展示以它为准，避免 `toDslNode` 归一化丢字段（如 props 大小写、多余键） |
| 渲染 | `<pre><code>` + `textContent` 赋值，**禁止 innerHTML 注入**，杜绝 XSS |
| 高亮 | 不做第三方高亮（bundle 零依赖）；可选轻量自研：仅对键/字符串/数字做简单着色（正则替换需谨慎，默认关闭，v1 不做） |
| 辅助操作 | 标题栏"**CJDSL 源 JSON**" +「复制」按钮（`navigator.clipboard.writeText(rawDslJson)`，失败降级 `document.execCommand('copy')`）+「关闭」按钮 |
| 容量 | 浮层 `max-height: 60vh; overflow:auto; white-space:pre;`，超长可滚动；超大 JSON（>500KB）提示"内容过大，请从控制台查看"并截断展示 |
| 可扩展事件 | 浮层打开/关闭时可选 dispatch `cjdsl-json-view`（detail: `{open, objectCode}`，bubbles+composed），供宿主做统计或拦截；v1 可先不接 |

### 3.4 参数开关设计及默认值

开关采用**三级联控，任一显式关闭即关闭，默认全部打开**：

| 层级 | 参数 | 默认值 | 语义 |
|---|---|---|---|
| 全局（宿主配置） | `CjdslConfig.jsonViewerEnabled` | `true` | 插件整体是否启用该功能；`false` 时 bridge 一律不设 `json-viewer` 属性（等于关闭） |
| 渲染级（WebComponent 属性） | `<cjdsl-page json-viewer="false">` | `"true"`（缺省） | 单卡片/单宿主关闭；由 bridge 依据全局开关与 DSL meta 决定是否写入 `"false"` |
| 卡片级（DSL meta） | `dsl.meta.jsonViewer = false` | 未设置（跟随上层） | 单条 DSL 内容显式关闭按钮，适合"该界面不需要展示源码"的场景（如敏感表单） |

- 关闭时行为：不渲染按钮（DOM 不创建），浮层自动收起，零运行时开销。
- 打开时行为：按 3.2/3.3 渲染按钮与浮层。

### 3.5 各参数命名建议

| 命名 | 位置/类型 | 说明 |
|---|---|---|
| `jsonViewerEnabled` | `CjdslConfig`（`src/index.ts`，camelCase） | 全局开关，默认 `true`；经 `pluginConfig` 注入，与既有 `enabled` 并列 |
| `__cjdslConfig.jsonViewerEnabled` | `window`（`src/client/index.tsx` 或 host 端写） | 全局开关到客户端的传递通道（沿用 `__dshpaCurrentSessionId` 的 window 全局先例）；host 端 `apply(ctx, pluginConfig)` 写入，client 端读取 |
| `json-viewer` | `<cjdsl-page>` DOM 属性（kebab-case） | WebComponent 渲染级开关；加入 `observedAttributes`；取值 `"true"/"false"`，缺省 `"true"` |
| `jsonViewer` | DSL `meta` 字段（`bridge.tsx` 读取） | 卡片级开关；`boolean`；`false` 关闭该卡片按钮 |
| `rawDslJson` | `CjdslPage` 私有字段（`cjdsl-page.ts`） | 保存 `dsl` 属性原始 JSON 字符串，浮层展示源 |
| `cjdsl-json-view` | CustomEvent（可选） | 浮层开关事件，detail `{ open, objectCode }`；对齐 `cjdsl-ready` 命名风格 |
| `.cjdsl-json-viewer-btn` / `.cjdsl-json-viewer-panel` | shadow DOM CSS 类 | 按钮与浮层样式类，前缀 `cjdsl-json-viewer` 避免污染 |

### 3.6 事件与安全

- 按钮/浮层事件全部在 shadow DOM 内处理；`click` 一律 `stopPropagation`，不触发 `cjdsl-action`、不干扰表单提交。
- JSON 展示使用 `textContent`，杜绝 `dangerouslySetInnerHTML`/`innerHTML` 注入路径（与 `DslRenderer` 的 DISABLED_PROP_KEYS 纪律一致）。
- 复制功能仅读取本地 `rawDslJson`，不产生网络请求。
- 不新增对 `window.dshpaNative` 的依赖；如未来需要"关闭状态记忆"，可仿照 `SaveFormStateAsync` 增加持久化键（v1 不做）。

---

## 4. 涉及文件清单与改动点

| # | 文件 | 改动类型 | 改动点 |
|---|---|---|---|
| 1 | `D:\Pro\CJ.Plug.Github\CJDSL\src\CJDSL.WebComponent\src\cjdsl-page.ts` | **核心改动** | ① `observedAttributes` 增加 `json-viewer`；② 构造 shadow DOM 时创建按钮元素与浮层骨架（或惰性创建）；③ 新增 `syncJsonViewer()`：读取 `json-viewer` 属性，控制按钮显隐；④ `parseDsl()` 保存 `rawDslJson`；⑤ 新增 `toggleJsonPanel(open)` 与浮层渲染（`<pre><code>` textContent + 复制/关闭按钮）；⑥ 样式表 `BASE_STYLE` 增加按钮/浮层规则（hover 显示、absolute 定位、max-height 滚动）；⑦ `attributeChangedCallback` 对 `json-viewer` 做同步 |
| 2 | `D:\Pro\CJ.Plug.Github\CJDSL\src\CJDSL.WebComponent\src\types.ts` | 轻量改动 | 新增可选 `CjdslJsonViewerDetail` 类型（`{ open: boolean; objectCode?: string }`），导出供宿主类型使用 |
| 3 | `D:\Pro\CJ.Plug.Github\CJDSL\src\CJDSL.WebComponent\src\index.ts` | 轻量改动 | 类型导出补充（可选）；注册逻辑不变（bundle 自动包含新代码，无需改 build.cjs） |
| 4 | `D:\Pro\CJ.Plug.Github\ABWork\decentralized-agent\Clients\DSH\DA.DSHPlug.CJDSL\src\client\bridge.tsx` | 配置透传 | 创建 `<cjdsl-page>` 时组合判定：`window.__cjdslConfig?.jsonViewerEnabled !== false` 且 `dslObj?.meta?.jsonViewer !== false` 才保留默认；否则 `el.setAttribute("json-viewer", "false")`；浮层展示的 JSON 与 `dedupe` 同源，无需额外处理 |
| 5 | `D:\Pro\CJ.Plug.Github\ABWork\decentralized-agent\Clients\DSH\DA.DSHPlug.CJDSL\src\index.ts` | 配置新增 | `CjdslConfig` 增加 `jsonViewerEnabled: boolean = true`；`apply(ctx, pluginConfig)` 合并后写入 `window.__cjdslConfig`（host 端同步写 window 不可行时，改由 `src/client/index.tsx` 从 `ctx` 读取后写） |
| 6 | `D:\Pro\CJ.Plug.Github\ABWork\decentralized-agent\Clients\DSH\DA.DSHPlug.CJDSL\src\client\index.tsx` | 配置传递 | `apply(ctx)` 启动时写 `window.__cjdslConfig = { jsonViewerEnabled: <由 host 注入或默认 true> }`；若 host→client 传递不便，v1 可默认 `true`，仅开放 WebComponent 属性与 DSL meta 两级关闭 |
| 7 | `D:\Pro\CJ.Plug.Github\CJDSL\src\CJDSL.React\src\ChatDslNode.tsx` | 可选（一致性） | PersonalPA 等仍走 React slot 的宿主如需同能力，可在其卡片容器右上角加同款按钮（独立实现，不依赖 WebComponent）；DSH 场景**无需改动** |
| 8 | `D:\Pro\CJ.Plug.Github\CJDSL\src\CJDSL.WebComponent\build.cjs` | 无改动 | bundle 自动纳入新代码；重新执行 `npm run build` 后按既有流程拷贝 `dist/cjdsl-page.js` 至插件 `lib/cjdsl-page.js` |

**改动量估算**：核心改动集中在 `cjdsl-page.ts`（约 +150 行），桥与配置透传各约 +20 行；不改渲染链路、不改 DSL 白名单、不改 MAUI 壳。

---

## 5. 风险与注意事项

| # | 风险/注意点 | 等级 | 说明与对策 |
|---|---|---|---|
| 1 | 按钮遮挡表单控件 | 中 | 按钮仅在 hover 时出现且为小尺寸（28px）、置于右上角；表单第一行靠右控件（如顶部工具栏）可能被短暂遮挡，可通过浮层/按钮 `right` 偏移或 `pointer-events` 策略缓解；评审确认 |
| 2 | 浮层内 JSON 超长导致性能/视觉问题 | 中 | `max-height: 60vh + overflow:auto`；>500KB 截断提示；格式化成本可接受（单卡片规模小） |
| 3 | XSS 注入 | 高（纪律） | JSON 一律 `textContent` 渲染；禁止任何 `innerHTML` 拼接 DSL 内容；不引入高亮正则处理用户可控文本（v1 关闭） |
| 4 | 事件冒泡干扰 | 中 | 按钮/浮层 click 一律 `stopPropagation`；验证与 `cjdsl-action`、`cjdsl-submitted`、聊天行点击互不干扰 |
| 5 | 与 dedupe 语义一致性 | 低 | 按钮展示的是 `dsl` 属性值（最终渲染版，含 KB 预填 patch），`dedupe` 判定用同源对象，不存在"看到与隐藏不同的源码"歧义；如需原始未预填版可在 meta 中保留 `rawDsl`（v1 不做，文档说明即可） |
| 6 | 多宿主一致性 | 低 | 核心实现在 WebComponent，所有 `<cjdsl-page>` 宿主自动获得；仅 PersonalPA 的 React slot 渲染器（`ChatDslNode`）需独立实现（见文件 7），列为可选，不阻塞 DSH 目标 |
| 7 | 全局开关 host→client 传递链路 | 中 | DSH 插件 client `apply(ctx)` 无法直接拿 `pluginConfig`；采用 `window.__cjdslConfig` 桥（有 `__dshpaCurrentSessionId` 先例）；若评估过重，v1 可只开放渲染级与卡片级两级关闭，全局默认 `true` |
| 8 | bundle 重新构建与分发 | 低 | 改完 `cjdsl-page.ts` 后必须重新 `npm run build`（WebComponent）并拷贝 `dist/cjdsl-page.js` → 插件 `lib/cjdsl-page.js`，否则线上仍是旧 bundle；`/api/cjdsl/cjdsl-page.js` 路由已设 `Cache-Control: no-cache`，客户端需强刷或重启 DSH |
| 9 | 键盘可达性与无障碍 | 低 | 按钮提供 `aria-label`/`title`；`focus-within` 显示；浮层 Esc 关闭 |
| 10 | 不改变既有行为 | 低 | 开关默认打开，现有卡片仅增加"hover 时可见的右上角小按钮"，不改变渲染结果与交互路径；关闭时零 DOM/运行时开销 |

---

## 6. 附录：核心接口草案（伪代码）

```ts
// CJDSL.WebComponent/src/cjdsl-page.ts（新增部分）
static get observedAttributes(): string[] {
  return ["dsl", "context", "submitted", "values", "json-viewer"];
}

private rawDslJson = "";            // 浮层展示源（dsl 属性原始字符串）
private jsonPanel: HTMLDivElement | null = null;

private syncJsonViewer(): void {
  const on = this.getAttribute("json-viewer") !== "false"; // 缺省 true
  const btn = this.shadowRoot?.querySelector<HTMLButtonElement>(".cjdsl-json-viewer-btn");
  if (btn) btn.style.display = on ? "" : "none";
  if (!on) this.closeJsonPanel();
}

private toggleJsonPanel(force?: boolean): void {
  const panel = this.ensureJsonPanel();
  const open = force ?? panel.style.display !== "block";
  panel.style.display = open ? "block" : "none";
  if (open) {
    const code = panel.querySelector("code")!;
    code.textContent = this.formatJson(this.rawDslJson); // textContent，禁止 innerHTML
  }
  this.dispatchEvent(new CustomEvent("cjdsl-json-view", {
    bubbles: true, composed: true,
    detail: { open, objectCode: this.objectCode() },
  }));
}
```

```ts
// DA.DSHPlug.CJDSL/src/client/bridge.tsx（新增判定，创建 <cjdsl-page> 处）
const jsonViewerEnabled =
  (window as any).__cjdslConfig?.jsonViewerEnabled !== false &&
  dslObj?.meta?.jsonViewer !== false;
if (!jsonViewerEnabled) el.setAttribute("json-viewer", "false");
```

---

## 7. 实施记录与变更说明（2026-08-29）

> 按本方案落地实施完成。以下记录实现过程中方案未覆盖或与伪代码有出入的点，均为按方案思路的合理决策。

### 7.1 改动文件清单

| # | 文件 | 改动类型 | 内容 |
|---|---|---|---|
| 1 | `D:\Pro\CJ.Plug.Github\CJDSL\src\CJDSL.WebComponent\src\cjdsl-page.ts` | 核心实现 | ①`observedAttributes` 增加 `json-viewer`；②BASE_STYLE 追加 `.cjdsl-json-viewer-btn`（默认 `opacity:0; pointer-events:none`，`:host(:hover)/:host(:focus-within)` 时显示）与 `.cjdsl-json-viewer-panel` 浮层样式；③新增 `rawDslJson` 私有字段，`parseDsl()` 同步保存 dsl 属性原始 JSON 字符串（含 innerHTML 退化源）；④新增 `ensureJsonViewerUi`（惰性创建按钮+浮层，关闭时不建 DOM）、`syncJsonViewer`（`json-viewer !== "false"` 即开）、`toggleJsonPanel`（`textContent` 渲染、>500KB 截断提示、dispatch `cjdsl-json-view`）、`formatJson`、`copyRawDslJson`（clipboard 优先、execCommand 降级）；⑤Esc 关闭浮层（window 级监听，disconnected 时移除）；⑥浮层外点击关闭（shadow 内监听） |
| 2 | `D:\Pro\CJ.Plug.Github\CJDSL\src\CJDSL.WebComponent\src\types.ts` | 类型 | 新增导出 `CjdslJsonViewerDetail { open: boolean; objectCode?: string }` |
| 3 | `D:\Pro\CJ.Plug.Github\CJDSL\src\CJDSL.WebComponent\src\index.ts` | 类型 | `CjdslJsonViewerDetail` 加入导入与导出 |
| 4 | `D:\Pro\CJ.Plug.Github\ABWork\decentralized-agent\Clients\DSH\DA.DSHPlug.CJDSL\src\client\bridge.tsx` | 开关透传 | 创建 `<cjdsl-page>` 后按 `window.__cjdslConfig?.jsonViewerEnabled !== false && dslObj?.meta?.jsonViewer !== false` 判定，任一关闭则 `el.setAttribute("json-viewer","false")` |
| 5 | `D:\Pro\CJ.Plug.Github\ABWork\decentralized-agent\Clients\DSH\DA.DSHPlug.CJDSL\src\index.ts` | 配置 | `CjdslConfig` 增加 `jsonViewerEnabled: boolean`，`DEFAULT_CONFIG` 默认 `true`；apply 合并后输出日志 `jsonViewerEnabled=...` |
| 6 | `D:\Pro\CJ.Plug.Github\ABWork\decentralized-agent\Clients\DSH\DA.DSHPlug.CJDSL\src\client\index.tsx` | 全局开关 | `apply()` 内 `ensureCjdslBundle()` 后写 `window.__cjdslConfig = { jsonViewerEnabled: existing?.jsonViewerEnabled !== false }`（尊重宿主预写值，v1 默认 true） |

### 7.2 构建命令与结果

- WebComponent（独立构建）：`cd CJDSL\src\CJDSL.WebComponent && npm run build` → 产出 `dist/cjdsl-page.js`（1158251 B）与 `dist/cjdsl-page.esm.js`（55578 B）。
- 插件（推荐一键构建）：`cd DA.DSHPlug.CJDSL && node build.mjs` → ①`lib/index.js`（host，ESM）；②`lib/client.js`（client，CJS 包进 `__ModuleLoader__.load`）；③自动 `npm run build` WebComponent 并将 `dist/cjdsl-page.js` 拷贝至 `lib/cjdsl-page.js`。三产物均已更新时间戳并验证包含新逻辑（bundle 内 `json-viewer`×19、`rawDslJson`×8、`cjdsl-json-view`×15；client.js 内 `jsonViewerEnabled`×4、`__cjdslConfig`×4；host index.js 内 `jsonViewerEnabled`×2）。
- 说明：`build.mjs` 经 PowerShell 管道 `| Select-Object` 截断时输出可能被吞，属控制台行为；直接执行可看到完整输出与 `build done` 结尾。依赖已装（WebComponent 与插件 node_modules 均存在），无需额外安装。

### 7.3 方案未覆盖问题的决策记录

1. **`objectCode()` 已有实现，直接复用**：附录伪代码引用 `this.objectCode()` 但未给出实现；实施时发现 `cjdsl-page.ts` 已存在该方法（`String(dslNode?.id || userContext?.objectCode || "dsl")`），保留原实现，避免重复定义（首次新增重复方法触发 esbuild `duplicate-class-member` 警告后已删除）。
2. **浮层布局用 `flex` 而非 `block`**：伪代码 `panel.style.display = open ? "block" : "none"`；实际浮层为列布局（标题栏 + 滚动体），CSS 声明 `display:flex`，JS 切换用 `"flex"`，效果等价。
3. **浮层外点击关闭的命中范围**：方案未细化；实现为 shadow DOM 内 click 监听，浮层不含目标时关闭（按钮已 `stopPropagation` 不干扰）；浮层内部与按钮点击均 `stopPropagation`，验证与 `cjdsl-action`、聊天行事件互不干扰。
4. **host→client 全局开关链路**：采用方案 §5 风险 7 的第二路径——由 `src/client/index.tsx` 从 `ctx`/window 读取后写 `window.__cjdslConfig`（v1 默认 true、尊重宿主预写值），host 侧 `apply` 仅记录日志，不直接写 window（避免 host ESM 上下文无 window 的边界问题）。
5. **超大 JSON 截断**：>500KB 时 `textContent` 前缀提示"内容过大，请从控制台查看"并截断展示前 20000 字符（截断成本可控，单卡片规模小）。
6. **Esc 关闭监听级别**：挂在 `window`（而非 shadow 内），浮层打开期间任意焦点均可 Esc 关闭，`disconnectedCallback` 移除，避免多卡片重复监听泄漏。

### 7.4 验证结论

- 静态验证：新 bundle 与 client/host 产物均确认包含目标标识符（见 7.2），`cjdsl-page.js` 与 `dist` 构建产物字节数一致（1158251 B），拷贝链路正确。
- 行为验证建议（运行时，需启动 DSH 宿主）：悬停卡片右上角出现按钮（28px、半透明、hover 加深）；点击展开内嵌浮层（`CJDSL 源 JSON` 标题 + 复制/关闭）；浮层展示与 `dedupe` 同源的 `dsl` 属性原始 JSON（格式化后）；`dsl.meta.jsonViewer=false` 或全局 `jsonViewerEnabled=false` 时按钮不出现；Esc/浮层外点击可关闭；`cjdsl-json-view` 事件（`{open, objectCode}`）可被宿主监听。
- 回归风险：仅新增 shadow DOM 顶部绝对定位元素，不改变 DslRenderer 渲染路径；开关关闭时按钮/浮层 DOM 不创建（惰性），零运行时开销。

---

*本文档为方案评审稿，欢迎就按钮交互、浮层形态与参数命名提出意见。*
*（内容由AI生成，仅供参考）*

*（内容由AI生成，仅供参考）*
