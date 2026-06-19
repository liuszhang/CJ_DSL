# CJDSL 新一代 DSL 驱动 Web 应用系统架构设计

> 基于 AIPage 项目 DSL 概念演进，目标技术栈：.NET 10 + Blazor + MudBlazor
> 核心理念：**界面不是写死的，而是大模型根据用户上下文与系统数据实时生成 DSL，再由统一渲染引擎动态呈现**

---

## 1. 核心设计哲学

```
传统前端开发：          DSL 驱动开发：
┌─────────────┐        ┌─────────────┐
│  需求变更   │        │  需求变更   │
└──────┬──────┘        └──────┬──────┘
       ▼                      ▼
┌─────────────┐        ┌─────────────┐
│ 人工改代码   │        │ LLM 重生成  │
│ React/Vue   │        │ DSL JSON    │
│ 组件树      │        │ 声明式描述   │
└──────┬──────┘        └──────┬──────┘
       ▼                      ▼
┌─────────────┐        ┌─────────────┐
│ 重新编译部署 │        │ 即时渲染     │
│ 发版上线    │        │ 无需发版    │
└─────────────┘        └─────────────┘
```

### 1.1 三层抽象

| 层级 | 名称 | 职责 | 受众 |
|------|------|------|------|
| L3 | **DSL 声明层** | 描述"界面长什么样" | 大模型 / 开发者 |
| L2 | **渲染引擎层** | 将 DSL 映射到 MudBlazor 组件 | 框架开发者 |
| L1 | **元模型层** | 描述"业务是什么" | 业务分析师 / 大模型 |

---

## 2. 系统总体架构

```
┌──────────────────────────────────────────────────────────────────────┐
│                         客户端 (Blazor WASM / Server)               │
│  ┌─────────────────┐  ┌─────────────────┐  ┌───────────────────┐  │
│  │   DSL 渲染引擎   │  │   状态管理中枢   │  │    事件总线        │  │
│  │  (Component     │  │  (DSLStateStore) │  │  (DslEventBus)    │  │
│  │   Renderer)     │  │                 │  │                   │  │
│  └────────┬────────┘  └────────┬────────┘  └─────────┬─────────┘  │
│           │                    │                     │            │
│           └──────────────────────┴─────────────────────┘            │
│                              │                                      │
│                              ▼                                      │
│  ┌─────────────────────────────────────────────────────────────┐   │
│  │              MudBlazor 组件树 (动态构建)                      │   │
│  │  ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐ ┌────────┐      │   │
│  │  │MudCard │ │MudForm │ │MudText │ │MudTable│ │MudBtn  │ ...  │   │
│  │  └────────┘ └────────┘ └────────┘ └────────┘ └────────┘      │   │
│  └─────────────────────────────────────────────────────────────┘   │
└──────────────────────────────────────────────────────────────────────┘
                                    │
                                    │ HTTP / SignalR
                                    ▼
┌──────────────────────────────────────────────────────────────────────┐
│                         服务端 (.NET 10)                              │
│  ┌─────────────────┐  ┌─────────────────┐  ┌───────────────────┐      │
│  │  DSL 生成服务    │  │  元模型服务      │  │   业务 API 层     │      │
│  │  (DslGenerator) │  │ (MetaModelSvc)  │  │  (Application)   │      │
│  │                 │  │                 │  │                  │      │
│  │  ┌───────────┐  │  │  ┌───────────┐  │  │  ┌───────────┐   │      │
│  │  │ LLM 适配器 │  │  │  │ M0-M7  │  │  │  │ CQRS/    │   │      │
│  │  │ (OpenAI/  │  │  │  │ 引擎     │  │  │  │ MediatR  │   │      │
│  │  │  Local)   │  │  │  └───────────┘  │  │  └───────────┘   │      │
│  │  └───────────┘  │  └─────────────────┘  └───────────────────┘      │
│  └─────────────────┘                                                  │
│  ┌─────────────────┐  ┌─────────────────┐  ┌───────────────────┐      │
│  │  意图理解引擎    │  │   数据上下文     │  │    缓存层         │      │
│  │ (IntentParser)  │  │ (DataContext)   │  │  (Redis/Memory)  │      │
│  └─────────────────┘  └─────────────────┘  └───────────────────┘      │
└──────────────────────────────────────────────────────────────────────┘
                                    │
                                    ▼
┌──────────────────────────────────────────────────────────────────────┐
│                         数据层                                       │
│  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  ┌─────────────┐  │
│  │  PostgreSQL │  │  SQLite     │  │  Vector DB  │  │  MinIO/S3   │  │
│  │  (主业务库)  │  │  (本地/测试)│  │  (语义检索)  │  │  (对象存储)  │  │
│  └─────────────┘  └─────────────┘  └─────────────┘  └─────────────┘  │
└──────────────────────────────────────────────────────────────────────┘
```

---

## 3. DSL 规范设计 (CJDSL Schema)

> 扩展自 AIPage DSL，增加条件渲染、权限控制、数据源绑定、响应式布局等能力

### 3.1 核心类型定义

```csharp
// ============================================
// DSL 页面根节点
// ============================================
public class DslPage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    
    /// <summary>页面布局模式：form(表单), list(列表), detail(详情), dashboard(仪表盘), custom(自定义)</summary>
    public string Layout { get; set; } = "form";
    
    /// <summary>页面级数据源配置</summary>
    public DslDataSource? DataSource { get; set; }
    
    /// <summary>页面级权限控制</summary>
    public DslPermission? Permission { get; set; }
    
    /// <summary>响应式断点配置</summary>
    public DslResponsive? Responsive { get; set; }
    
    /// <summary>组件树</summary>
    public List<DslComponent> Components { get; set; } = new();
    
    /// <summary>页面级事件处理器</summary>
    public List<DslEventHandler>? PageEvents { get; set; }
    
    /// <summary>页面样式配置</summary>
    public DslStyle? Style { get; set; }
}

// ============================================
// 通用组件节点
// ============================================
public class DslComponent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    
    /// <summary>组件类型：与 MudBlazor 组件映射</summary>
    public string Type { get; set; } = "text";
    
    /// <summary>组件属性（透传给 MudBlazor）</summary>
    public Dictionary<string, object>? Props { get; set; }
    
    /// <summary>子组件（递归树）</summary>
    public List<DslComponent>? Children { get; set; }
    
    /// <summary>数据绑定路径，如 "user.name" 或 "@datasource.items"</summary>
    public string? DataBind { get; set; }
    
    /// <summary>标签文本（用于表单字段）</summary>
    public string? Label { get; set; }
    
    /// <summary>表单字段名</summary>
    public string? FieldName { get; set; }
    
    /// <summary>Grid 布局占用列数 (1-12)</summary>
    public int? Span { get; set; } = 12;
    
    /// <summary>条件渲染表达式：如 "@user.role == 'admin'"</summary>
    public string? VisibleIf { get; set; }
    
    /// <summary>禁用条件表达式</summary>
    public string? DisabledIf { get; set; }
    
    /// <summary>事件处理器列表</summary>
    public List<DslEvent>? Events { get; set; }
    
    /// <summary>数据源覆盖（组件级）</summary>
    public DslDataSource? DataSource { get; set; }
    
    /// <summary>验证规则（表单字段）</summary>
    public List<DslValidationRule>? ValidationRules { get; set; }
    
    /// <summary>组件样式</summary>
    public DslStyle? Style { get; set; }
    
    /// <summary>Tooltip / 帮助文本</summary>
    public string? HelpText { get; set; }
}

// ============================================
// 事件定义
// ============================================
public class DslEvent
{
    /// <summary>事件类型：onClick, onChange, onSubmit, onLoad, onRowClick, onSearch...</summary>
    public string Type { get; set; } = string.Empty;
    
    /// <summary>处理器名称：预定义行为或自定义 API 调用</summary>
    public string Handler { get; set; } = string.Empty;
    
    /// <summary>处理器参数</summary>
    public Dictionary<string, object>? Params { get; set; }
    
    /// <summary>执行前确认对话框配置</summary>
    public DslConfirm? Confirm { get; set; }
    
    /// <summary>防抖/节流（毫秒）</summary>
    public int? DebounceMs { get; set; }
}

// ============================================
// 预定义 Handler 类型
// ============================================
public static class DslHandlers
{
    public const string Submit = "submit";           // 表单提交
    public const string Navigate = "navigate";        // 页面跳转
    public const string ApiCall = "apiCall";          // 调用后端 API
    public const string OpenModal = "openModal";      // 打开弹窗
    public const string CloseModal = "closeModal";    // 关闭弹窗
    public const string Refresh = "refresh";            // 刷新数据
    public const string SetValue = "setValue";        // 设置字段值
    public const string ShowToast = "showToast";       // 显示提示
    public const string Export = "export";             // 导出数据
    public const string Validate = "validate";         // 执行验证
    public const string Chain = "chain";               // 链式调用多个 handler
}
```

### 3.2 组件类型映射表

| CJDSL Type | MudBlazor 组件 | 说明 |
|------------|---------------|------|
| `page` | `MudContainer` | 页面根容器 |
| `card` | `MudCard` / `MudCardContent` | 卡片容器 |
| `form` | `MudForm` | 表单容器 |
| `text` | `MudTextField` | 文本输入 |
| `number` | `MudNumericField` | 数字输入 |
| `select` | `MudSelect<T>` | 下拉选择 |
| `autocomplete` | `MudAutocomplete<T>` | 自动补全 |
| `textarea` | `MudTextField` (Multiline) | 多行文本 |
| `date` | `MudDatePicker` | 日期选择 |
| `datetime` | `MudDatePicker` + Time | 日期时间 |
| `time` | `MudTimePicker` | 时间选择 |
| `checkbox` | `MudCheckBox` | 复选框 |
| `switch` | `MudSwitch` | 开关 |
| `radio` | `MudRadioGroup` | 单选组 |
| `slider` | `MudSlider` | 滑块 |
| `rating` | `MudRating` | 评分 |
| `file` | `MudFileUpload` | 文件上传 |
| `button` | `MudButton` | 按钮 |
| `iconButton` | `MudIconButton` | 图标按钮 |
| `fab` | `MudFab` | 浮动按钮 |
| `table` | `MudTable<T>` | 数据表格 |
| `dataGrid` | `MudDataGrid<T>` | 高级数据网格 |
| `list` | `MudList` / `MudListItem` | 列表 |
| `tabs` | `MudTabs` / `MudTabPanel` | 标签页 |
| `stepper` | `MudStepper` | 步骤条 |
| `expansion` | `MudExpansionPanels` | 折叠面板 |
| `dialog` | `MudDialog` | 对话框 |
| `snackbar` | `MudSnackbar` | 消息条 |
| `progress` | `MudProgressLinear` / `Circular` | 进度条 |
| `chart` | `MudChart` | 图表 |
| `markdown` | `MudMarkdown` (自定义) | Markdown 渲染 |
| `grid` | `MudGrid` / `MudItem` | 网格布局 |
| `stack` | `MudStack` | 堆叠布局 |
| `paper` | `MudPaper` | 纸张容器 |
| `divider` | `MudDivider` | 分割线 |
| `textDisplay` | `MudText` / `MudTypography` | 纯文本展示 |
| `avatar` | `MudAvatar` | 头像 |
| `chip` | `MudChip` | 标签 chip |
| `badge` | `MudBadge` | 徽标 |
| `tooltip` | `MudTooltip` | 工具提示 |
| `skeleton` | `MudSkeleton` | 骨架屏 |
| `appBar` | `MudAppBar` | 顶部导航 |
| `drawer` | `MudDrawer` | 侧边抽屉 |
| `breadcrumb` | `MudBreadcrumbs` | 面包屑 |
| `pagination` | `MudPagination` | 分页 |
| `tree` | `MudTreeView` | 树形控件 |
| `timeline` | `MudTimeline` | 时间线 |
| `carousel` | `MudCarousel` | 轮播 |
| `colorPicker` | `MudColorPicker` | 颜色选择器 |
| `richText` | `MudRichText` (自定义) | 富文本编辑器 |
| `jsonEditor` | `MudJsonEditor` (自定义) | JSON 编辑器 |
| `codeBlock` | `MudCodeBlock` (自定义) | 代码块 |
| `kanban` | `MudKanban` (自定义) | 看板 |
| `calendar` | `MudCalendar` (自定义) | 日历 |
| `map` | `MudMap` (自定义) | 地图组件 |
| `iframe` | `MudElement` (iframe) | 内嵌页面 |
| `custom` | 任意自定义组件 | 自定义渲染器注册 |

### 3.3 示例 DSL：设备报修单表单

```json
{
  "id": "page_equipment_repair_001",
  "title": "设备报修单",
  "description": "设备故障报修录入页面",
  "layout": "form",
  "permission": {
    "requiredRoles": ["operator", "admin"],
    "requiredPermissions": ["repair:create"]
  },
  "dataSource": {
    "type": "api",
    "endpoint": "/api/repair/{id}",
    "method": "GET",
    "params": { "id": "@route.id" }
  },
  "components": [
    {
      "type": "card",
      "props": { "Elevation": 2, "Class": "pa-4" },
      "children": [
        {
          "type": "textDisplay",
          "props": { "Typo": "h5" },
          "dataBind": "@page.title"
        },
        {
          "type": "form",
          "id": "repairForm",
          "props": { "ValidationDelay": 300 },
          "children": [
            {
              "type": "grid",
              "children": [
                {
                  "type": "text",
                  "span": 6,
                  "label": "报修单号",
                  "fieldName": "repairNo",
                  "dataBind": "@data.repairNo",
                  "props": { "Required": true, "ReadOnly": true, "Variant": "Filled" },
                  "validationRules": [
                    { "type": "required", "message": "报修单号必填" },
                    { "type": "regex", "pattern": "^[A-Z]{2}-\\d{6}$", "message": "格式：XX-000000" }
                  ]
                },
                {
                  "type": "text",
                  "span": 6,
                  "label": "设备名称",
                  "fieldName": "equipmentName",
                  "props": { "Required": true, "AdornmentIcon": "@Icons.Material.Filled.Devices" }
                },
                {
                  "type": "select",
                  "span": 6,
                  "label": "设备类型",
                  "fieldName": "equipmentType",
                  "props": { "Required": true },
                  "dataSource": {
                    "type": "dictionary",
                    "code": "equipment_type"
                  }
                },
                {
                  "type": "autocomplete",
                  "span": 6,
                  "label": "报修人",
                  "fieldName": "reporter",
                  "props": { "Required": true, "CoerceValue": true },
                  "dataSource": {
                    "type": "api",
                    "endpoint": "/api/users/search",
                    "searchParam": "keyword"
                  }
                },
                {
                  "type": "date",
                  "span": 6,
                  "label": "报修日期",
                  "fieldName": "repairDate",
                  "props": { "Required": true, "MaxDate": "@today" }
                },
                {
                  "type": "select",
                  "span": 6,
                  "label": "优先级",
                  "fieldName": "priority",
                  "props": { "Required": true },
                  "dataSource": {
                    "type": "enum",
                    "code": "priority"
                  },
                  "style": { "Color": "@priorityColor(@data.priority)" }
                },
                {
                  "type": "textarea",
                  "span": 12,
                  "label": "故障描述",
                  "fieldName": "faultDescription",
                  "props": { "Required": true, "Lines": 4, "MaxLength": 500 }
                }
              ]
            },
            {
              "type": "divider",
              "props": { "Class": "my-4" }
            },
            {
              "type": "stack",
              "props": { "Row": true, "Justify": "flex-end", "Spacing": 2 },
              "children": [
                {
                  "type": "button",
                  "props": { "Variant": "Outlined", "Color": "Secondary" },
                  "label": "重置",
                  "events": [
                    { "type": "onClick", "handler": "reset", "params": { "formId": "repairForm" } }
                  ]
                },
                {
                  "type": "button",
                  "props": { "Variant": "Filled", "Color": "Primary", "StartIcon": "Save" },
                  "label": "保存草稿",
                  "visibleIf": "@user.hasPermission('repair:save')",
                  "events": [
                    {
                      "type": "onClick",
                      "handler": "apiCall",
                      "params": {
                        "endpoint": "/api/repair/save",
                        "method": "POST",
                        "formId": "repairForm",
                        "successMessage": "保存成功",
                        "onSuccess": [
                          { "handler": "showToast", "params": { "message": "已保存为草稿", "severity": "success" } },
                          { "handler": "navigate", "params": { "path": "/repair/list" } }
                        ]
                      }
                    }
                  ]
                },
                {
                  "type": "button",
                  "props": { "Variant": "Filled", "Color": "Tertiary", "StartIcon": "Send" },
                  "label": "提交",
                  "events": [
                    {
                      "type": "onClick",
                      "handler": "chain",
                      "confirm": {
                        "title": "确认提交",
                        "message": "提交后将进入审批流程，是否继续？",
                        "confirmText": "确认提交",
                        "cancelText": "取消"
                      },
                      "params": {
                        "chain": [
                          { "handler": "validate", "params": { "formId": "repairForm" } },
                          {
                            "handler": "apiCall",
                            "params": {
                              "endpoint": "/api/repair/submit",
                              "method": "POST",
                              "formId": "repairForm"
                            }
                          },
                          { "handler": "showToast", "params": { "message": "提交成功", "severity": "success" } },
                          { "handler": "navigate", "params": { "path": "/repair/list" } }
                        ]
                      }
                    }
                  ]
                }
              ]
            }
          ]
        }
      ]
    }
  ]
}
```

### 3.4 示例 DSL：列表页

```json
{
  "id": "page_equipment_repair_list",
  "title": "报修单列表",
  "layout": "list",
  "dataSource": {
    "type": "api",
    "endpoint": "/api/repair/list",
    "method": "POST",
    "pagination": { "pageParam": "pageIndex", "sizeParam": "pageSize", "defaultSize": 20 },
    "serverSide": true
  },
  "components": [
    {
      "type": "card",
      "children": [
        {
          "type": "stack",
          "props": { "Row": true, "Spacing": 2, "Class": "mb-4" },
          "children": [
            {
              "type": "text",
              "props": { "Placeholder": "请输入报修单号或设备名称", "AdornmentIcon": "Search", "Style": "width: 300px;" },
              "dataBind": "@query.keyword"
            },
            {
              "type": "select",
              "props": { "Placeholder": "状态", "Clearable": true },
              "dataBind": "@query.status",
              "dataSource": { "type": "enum", "code": "repair_status" }
            },
            {
              "type": "date",
              "props": { "Placeholder": "开始日期" },
              "dataBind": "@query.startDate"
            },
            {
              "type": "date",
              "props": { "Placeholder": "结束日期" },
              "dataBind": "@query.endDate"
            },
            {
              "type": "button",
              "props": { "Color": "Primary", "StartIcon": "Search" },
              "label": "查询",
              "events": [
                { "type": "onClick", "handler": "refresh", "params": { "targetId": "repairTable" } }
              ]
            },
            {
              "type": "button",
              "props": { "Variant": "Outlined", "StartIcon": "Add" },
              "label": "新增",
              "visibleIf": "@user.hasPermission('repair:create')",
              "events": [
                { "type": "onClick", "handler": "navigate", "params": { "path": "/repair/create" } }
              ]
            }
          ]
        },
        {
          "type": "dataGrid",
          "id": "repairTable",
          "dataBind": "@datasource.items",
          "props": {
            "Hover": true,
            "Striped": true,
            "Dense": true,
            "Loading": "@datasource.loading",
            "FixedHeader": true,
            "Height": "calc(100vh - 280px)"
          },
          "children": [
            {
              "type": "column",
              "props": { "Title": "报修单号", "Field": "repairNo", "Sortable": true, "Width": "150px" }
            },
            {
              "type": "column",
              "props": { "Title": "设备名称", "Field": "equipmentName", "Sortable": true }
            },
            {
              "type": "column",
              "props": { "Title": "状态", "Field": "status", "Width": "120px" },
              "style": { "CellTemplate": "chipStatus" }
            },
            {
              "type": "column",
              "props": { "Title": "优先级", "Field": "priority", "Width": "100px" }
            },
            {
              "type": "column",
              "props": { "Title": "报修人", "Field": "reporter", "Width": "120px" }
            },
            {
              "type": "column",
              "props": { "Title": "报修日期", "Field": "repairDate", "Width": "150px", "Format": "yyyy-MM-dd" }
            },
            {
              "type": "column",
              "props": { "Title": "操作", "Width": "200px" },
              "style": { "CellTemplate": "actions" },
              "children": [
                {
                  "type": "iconButton",
                  "props": { "Icon": "Edit", "Size": "Small", "Color": "Primary" },
                  "events": [
                    { "type": "onClick", "handler": "navigate", "params": { "path": "/repair/edit/{id}", "bind": { "id": "@row.id" } } }
                  ]
                },
                {
                  "type": "iconButton",
                  "props": { "Icon": "Visibility", "Size": "Small", "Color": "Info" },
                  "events": [
                    { "type": "onClick", "handler": "openModal", "params": { "dslPath": "/api/dsl/repair/detail/{id}", "bind": { "id": "@row.id" } } }
                  ]
                }
              ]
            }
          ]
        },
        {
          "type": "pagination",
          "props": {
            "TotalItems": "@datasource.totalCount",
            "PageSize": "@datasource.pageSize",
            "CurrentPage": "@datasource.pageIndex"
          },
          "events": [
            { "type": "onPageChange", "handler": "refresh", "params": { "targetId": "repairTable" } }
          ]
        }
      ]
    }
  ]
}
```

---

## 4. 元模型层 (MetaModel) — 七维本体

> 继承并扩展 AIPage 的七层元模型，使其成为 DSL 生成的知识底座

```
┌─────────────────────────────────────────────────────────────────┐
│                         M7: 质量约束模型                          │
│   响应时间、可用性、TPS、并发用户数、数据一致性策略                    │
├─────────────────────────────────────────────────────────────────┤
│                         M6: 异常补偿模型                          │
│   异常类型、重试策略、补偿动作、Saga 编排、死信队列                  │
├─────────────────────────────────────────────────────────────────┤
│                         M5: 主体模型                              │
│   参与者、角色、权限、外部契约、接口定义 (M5.5)                      │
├─────────────────────────────────────────────────────────────────┤
│                         M4: 场景模型                              │
│   业务流程、用例、场景时间线、动作编排、网关分支                     │
├─────────────────────────────────────────────────────────────────┤
│                         M3: 规则模型                              │
│   验证规则、计算规则、推导规则、风控规则、规则表达式引擎              │
├─────────────────────────────────────────────────────────────────┤
│                         M2: 行为模型                              │
│   业务动作、前置条件、后置状态、领域事件、所需权限                   │
├─────────────────────────────────────────────────────────────────┤
│                       M1.5: 关系模型                              │
│   对象关联、继承、组合、聚合、基数约束、传递性/对称性               │
├─────────────────────────────────────────────────────────────────┤
│                         M1: 对象模型                              │
│   业务实体、属性、生命周期状态、约束、数据映射、时态配置              │
├─────────────────────────────────────────────────────────────────┤
│                         M0: 基础数据模型                          │
│   枚举、数据字典、量纲、基本属性、密级、人员-数据映射                │
└─────────────────────────────────────────────────────────────────┘
```

### 4.1 元模型 → DSL 生成映射

```csharp
public interface IDslGenerator
{
    /// <summary>从 M1 对象模型生成表单 DSL</summary>
    Task<DslPage> GenerateFormAsync(M1_Object metaObject, GenerateOptions options);
    
    /// <summary>从 M1 对象模型生成列表 DSL</summary>
    Task<DslPage> GenerateListAsync(M1_Object metaObject, GenerateOptions options);
    
    /// <summary>从 M1 对象模型生成详情 DSL</summary>
    Task<DslPage> GenerateDetailAsync(M1_Object metaObject, GenerateOptions options);
    
    /// <summary>从 M4 场景生成仪表盘 DSL</summary>
    Task<DslPage> GenerateDashboardAsync(M4_Scene scene, GenerateOptions options);
    
    /// <summary>基于自然语言描述生成完整 DSL</summary>
    Task<DslPage> GenerateFromNlpAsync(string description, UserContext user, GenerateOptions options);
    
    /// <summary>基于用户上下文动态调整现有 DSL</summary>
    Task<DslPage> AdaptAsync(DslPage baseDsl, UserContext user, DataContext data);
}

public class GenerateOptions
{
    /// <summary>目标布局：Form, List, Detail, Dashboard, Custom</summary>
    public string Layout { get; set; } = "form";
    
    /// <summary>用户角色（影响字段权限和按钮可见性）</summary>
    public List<string> Roles { get; set; } = new();
    
    /// <summary>用户偏好（紧凑/宽松、深色/浅色等）</summary>
    public UserPreference Preference { get; set; } = new();
    
    /// <summary>设备类型：Desktop, Tablet, Mobile</summary>
    public string DeviceType { get; set; } = "Desktop";
    
    /// <summary>数据源上下文（预加载数据）</summary>
    public Dictionary<string, object>? DataContext { get; set; }
    
    /// <summary>LLM 温度参数（创造性 vs 确定性）</summary>
    public float Temperature { get; set; } = 0.3f;
}
```

### 4.2 LLM 提示词工程（DSL 生成）

```csharp
public class DslPromptBuilder
{
    public string BuildFormPrompt(M1_Object obj, UserContext user, GenerateOptions opts)
    {
        return $@"
你是一位精通 Blazor + MudBlazor 的资深前端架构师。请根据以下业务对象元模型，生成一份 CJDSL JSON。

## 业务对象信息
- 对象名称：{obj.Name}
- 对象编码：{obj.Code}
- 描述：{obj.Description}

## 属性列表
{FormatProperties(obj.Properties)}

## 生命周期状态
{FormatStates(obj.LifeCycleStates)}

## 用户上下文
- 角色：{string.Join(", ", user.Roles)}
- 权限：{string.Join(", ", user.Permissions)}
- 设备：{opts.DeviceType}
- 偏好：{JsonSerializer.Serialize(opts.Preference)}

## 生成规则
1. 使用 CJDSL Schema v2，组件类型必须是规范中定义的 type
2. 根据用户角色设置 visibleIf 条件，无权限的按钮不渲染
3. 根据生命周期状态设置字段的 disabledIf（已归档数据不可编辑）
4. 为 select/ autocomplete 类型字段配置 DataSource（字典或 API）
5. 为必填字段添加 validationRules（required）
6. 使用 MudBlazor 的 Props 命名（PascalCase，如 Required, Variant, Color）
7. Grid 布局：桌面端每行 2 列，移动端每行 1 列
8. 生成响应式配置（Responsive.Breakpoints）
9. 为每个 action 配置合理的 events 和 handlers
10. 输出必须是合法的 JSON，不要包含注释

## 输出格式
只输出 DslPage JSON，不要任何解释文字。
";
    }
}
```

---

## 5. 渲染引擎设计 (Blazor)

### 5.1 核心架构

```csharp
// ============================================
// 渲染器注册表
// ============================================
public interface IDslComponentRenderer
{
    string ComponentType { get; }
    RenderFragment Render(DslComponent component, DslRenderContext context);
}

public class DslRendererRegistry
{
    private readonly Dictionary<string, IDslComponentRenderer> _renderers = new();
    
    public void Register(IDslComponentRenderer renderer) => _renderers[renderer.ComponentType] = renderer;
    public IDslComponentRenderer? Get(string type) => _renderers.GetValueOrDefault(type);
}

// ============================================
// 渲染上下文（贯穿整个渲染树）
// ============================================
public class DslRenderContext
{
    /// <summary>当前页面 DSL</summary>
    public DslPage Page { get; set; } = null!;
    
    /// <summary>数据存储（支持嵌套路径，如 @data.user.name）</summary>
    public DslDataStore DataStore { get; } = new();
    
    /// <summary>用户上下文</summary>
    public UserContext User { get; set; } = null!;
    
    /// <summary>表达式引擎（用于解析 VisibleIf / DisabledIf）</summary>
    public IExpressionEvaluator ExpressionEvaluator { get; set; } = null!;
    
    /// <summary>表单状态管理</summary>
    public Dictionary<string, FormState> Forms { get; } = new();
    
    /// <summary>当前行数据（用于表格行内渲染）</summary>
    public object? RowData { get; set; }
    
    /// <summary>组件引用（用于事件回调）</summary>
    public Dictionary<string, object> ComponentRefs { get; } = new();
    
    /// <summary>父级上下文（支持嵌套）</summary>
    public DslRenderContext? Parent { get; set; }
}
```

### 5.2 核心渲染组件

```razor
@* DslPageRenderer.razor — 页面级根组件 *@
@inject DslRendererRegistry Registry
@inject IExpressionEvaluator Evaluator
@inject IServiceProvider ServiceProvider

@if (_dslPage != null)
{
    <CascadingValue Value="_context">
        <MudContainer MaxWidth="MaxWidth.ExtraLarge" Class="pa-4">
            @foreach (var component in _dslPage.Components)
            {
                <DslComponentRenderer Component="component" />
            }
        </MudContainer>
    </CascadingValue>
}

@code {
    [Parameter] public DslPage? Dsl { get; set; }
    [Parameter] public UserContext? User { get; set; }
    [Parameter] public Dictionary<string, object>? InitialData { get; set; }

    private DslPage? _dslPage;
    private DslRenderContext _context = null!;

    protected override void OnParametersSet()
    {
        if (Dsl != null)
        {
            _dslPage = Dsl;
            _context = new DslRenderContext
            {
                Page = _dslPage,
                User = User ?? new UserContext(),
                ExpressionEvaluator = Evaluator
            };
            if (InitialData != null)
            {
                foreach (var kv in InitialData)
                    _context.DataStore.Set($"data.{kv.Key}", kv.Value);
            }
        }
    }
}
```

```razor
@* DslComponentRenderer.razor — 递归组件渲染 *@
@inject DslRendererRegistry Registry
@inject IExpressionEvaluator Evaluator
@inject DslEventDispatcher EventDispatcher
@inject IJSRuntime JSRuntime

@if (ShouldRender())
{
    var renderer = Registry.Get(Component.Type);
    if (renderer != null)
    {
        @renderer.Render(Component, Context)
    }
    else if (Component.Type == "grid")
    {
        <MudGrid>
            @foreach (var child in Component.Children ?? Enumerable.Empty<DslComponent>())
            {
                <MudItem xs="12" sm="@GetSpan(child)" md="@GetSpan(child)" lg="@GetSpan(child)">
                    <DslComponentRenderer Component="child" />
                </MudItem>
            }
        </MudGrid>
    }
    else if (Component.Type == "stack")
    {
        <MudStack Row="@(GetProp<bool?>("Row") == true)" 
                  Spacing="@GetProp<int?>("Spacing")"
                  Justify="@GetJustify()"
                  AlignItems="@GetAlignItems()"
                  Class="@GetProp<string>("Class")">
            @foreach (var child in Component.Children ?? Enumerable.Empty<DslComponent>())
            {
                <DslComponentRenderer Component="child" />
            }
        </MudStack>
    }
    else if (Component.Type == "card")
    {
        <MudCard Elevation="@GetProp<int?>("Elevation")" Class="@GetProp<string>("Class")">
            <MudCardContent>
                @foreach (var child in Component.Children ?? Enumerable.Empty<DslComponent>())
                {
                    <DslComponentRenderer Component="child" />
                }
            </MudCardContent>
        </MudCard>
    }
    else if (Component.Type == "form")
    {
        <MudForm @ref="_formRef" @bind-IsValid="_isValid" ValidationDelay="@GetProp<int?>("ValidationDelay")">
            @foreach (var child in Component.Children ?? Enumerable.Empty<DslComponent>())
            {
                <DslComponentRenderer Component="child" />
            }
        </MudForm>
    }
    else if (Component.Type == "text")
    {
        <MudTextField T="string"
                      @bind-Value="_stringValue"
                      Label="@Component.Label"
                      Required="@GetProp<bool?>("Required")"
                      ReadOnly="@GetProp<bool?>("ReadOnly")"
                      Variant="@GetVariant()"
                      Placeholder="@GetProp<string>("Placeholder")"
                      AdornmentIcon="@GetIcon("AdornmentIcon")"
                      MaxLength="@GetProp<int?>("MaxLength")"
                      Disabled="@IsDisabled()"
                      Validation="@GetValidation()"
                      For="@(() => _stringValue)"
                      @attributes="GetExtraAttributes()" />
    }
    else if (Component.Type == "select")
    {
        <MudSelect T="string"
                   @bind-Value="_stringValue"
                   Label="@Component.Label"
                   Required="@GetProp<bool?>("Required")"
                   Clearable="@GetProp<bool?>("Clearable")"
                   Variant="@GetVariant()"
                   Disabled="@IsDisabled()"
                   @attributes="GetExtraAttributes()">
            @foreach (var item in GetSelectItems())
            {
                <MudSelectItem Value="@item.Value">@item.Label</MudSelectItem>
            }
        </MudSelect>
    }
    else if (Component.Type == "button")
    {
        <MudButton OnClick="@HandleClick"
                   Variant="@GetButtonVariant()"
                   Color="@GetColor()"
                   StartIcon="@GetIcon("StartIcon")"
                   EndIcon="@GetIcon("EndIcon")"
                   Size="@GetSize()"
                   Disabled="@IsDisabled()"
                   Class="@GetProp<string>("Class")">
            @GetButtonLabel()
        </MudButton>
    }
    else if (Component.Type == "table" || Component.Type == "dataGrid")
    {
        <DslDataGridRenderer Component="Component" />
    }
    else if (Component.Type == "textDisplay")
    {
        <MudText Typo="@GetTypo()" Class="@GetProp<string>("Class")" Color="@GetTextColor()">
            @GetDisplayText()
        </MudText>
    }
    else
    {
        <MudAlert Severity="Severity.Warning">
            未识别的组件类型: @Component.Type
        </MudAlert>
    }
}

@code {
    [CascadingParameter] public DslRenderContext Context { get; set; } = null!;
    [Parameter] public DslComponent Component { get; set; } = null!;

    private MudForm? _formRef;
    private string _stringValue = string.Empty;
    private bool _isValid;

    private bool ShouldRender()
    {
        if (string.IsNullOrEmpty(Component.VisibleIf)) return true;
        return Context.ExpressionEvaluator.Evaluate<bool>(Component.VisibleIf, Context.DataStore);
    }

    private bool IsDisabled()
    {
        if (string.IsNullOrEmpty(Component.DisabledIf)) return false;
        return Context.ExpressionEvaluator.Evaluate<bool>(Component.DisabledIf, Context.DataStore);
    }

    private T? GetProp<T>(string key) => Component.Props?.GetValueOrDefault(key) is T v ? v : default;

    private Variant GetVariant() => Enum.TryParse<Variant>(GetProp<string>("Variant"), out var v) ? v : Variant.Text;
    private Variant GetButtonVariant() => Enum.TryParse<Variant>(GetProp<string>("Variant"), out var v) ? v : Variant.Filled;
    private Color GetColor() => Enum.TryParse<Color>(GetProp<string>("Color"), out var c) ? c : Color.Default;
    private Typo GetTypo() => Enum.TryParse<Typo>(GetProp<string>("Typo"), out var t) ? t : Typo.body1;
    private Size GetSize() => Enum.TryParse<Size>(GetProp<string>("Size"), out var s) ? s : Size.Medium;
    private Justify GetJustify() => Enum.TryParse<Justify>(GetProp<string>("Justify"), out var j) ? j : Justify.FlexStart;
    private AlignItems GetAlignItems() => Enum.TryParse<AlignItems>(GetProp<string>("AlignItems"), out var a) ? a : AlignItems.Center;
    private Color GetTextColor() => Enum.TryParse<Color>(GetProp<string>("Color"), out var c) ? c : Color.Default;

    private string? GetIcon(string propKey)
    {
        var iconName = GetProp<string>(propKey);
        if (string.IsNullOrEmpty(iconName)) return null;
        return $"@Icons.Material.Filled.{iconName}";
    }

    private int GetSpan(DslComponent child) => child.Span ?? 12;

    private string GetButtonLabel() => Component.Label ?? Component.Props?.GetValueOrDefault("children")?.ToString() ?? "Button";

    private string GetDisplayText() => Component.DataBind != null 
        ? Context.DataStore.GetString(Component.DataBind) ?? Component.Label ?? ""
        : Component.Label ?? "";

    private IEnumerable<SelectItem> GetSelectItems()
    {
        if (Component.DataSource?.Type == "dictionary" || Component.DataSource?.Type == "enum")
        {
            return Context.DataStore.GetList<SelectItem>($"dict.{Component.DataSource.Code}") ?? Enumerable.Empty<SelectItem>();
        }
        if (Component.DataSource?.Type == "api")
        {
            // 异步加载，返回已缓存数据
            return Context.DataStore.GetList<SelectItem>($"api.{Component.DataSource.Endpoint}") ?? Enumerable.Empty<SelectItem>();
        }
        return Enumerable.Empty<SelectItem>();
    }

    private Func<string, IEnumerable<string>> GetValidation()
    {
        if (Component.ValidationRules == null || !Component.ValidationRules.Any())
            return _ => Enumerable.Empty<string>();

        return value => ValidateField(value);
    }

    private IEnumerable<string> ValidateField(string value)
    {
        foreach (var rule in Component.ValidationRules!)
        {
            var result = rule.Type switch
            {
                "required" => string.IsNullOrWhiteSpace(value) ? rule.Message : null,
                "regex" => rule.Pattern != null && !Regex.IsMatch(value ?? "", rule.Pattern) ? rule.Message : null,
                "minLength" => value?.Length < (rule.MinLength ?? 0) ? rule.Message : null,
                "maxLength" => value?.Length > (rule.MaxLength ?? int.MaxValue) ? rule.Message : null,
                "email" => !Regex.IsMatch(value ?? "", @"^[^@]+@[^@]+\.[^@]+$") ? rule.Message : null,
                _ => null
            };
            if (result != null) yield return result;
        }
    }

    private Dictionary<string, object> GetExtraAttributes()
    {
        var extras = new Dictionary<string, object>();
        if (Component.Props == null) return extras;
        foreach (var kv in Component.Props.Where(p => !new[] { "Required", "ReadOnly", "Variant", "Placeholder", "AdornmentIcon", "MaxLength", "Color", "Typo", "Class", "Row", "Spacing", "Justify", "AlignItems", "Elevation", "ValidationDelay", "Clearable", "Size", "StartIcon", "EndIcon", "children" }.Contains(p.Key)))
        {
            extras[kv.Key] = kv.Value;
        }
        return extras;
    }

    private async Task HandleClick(MouseEventArgs e)
    {
        if (Component.Events == null) return;
        foreach (var evt in Component.Events.Where(ev => ev.Type == "onClick"))
        {
            await EventDispatcher.DispatchAsync(evt, Component, Context);
        }
    }
}
```

### 5.3 事件分发器

```csharp
public class DslEventDispatcher
{
    private readonly IServiceProvider _serviceProvider;
    private readonly IHttpClientFactory _httpClientFactory;
    private readonly ISnackbar _snackbar;
    private readonly NavigationManager _navigation;
    private readonly IDialogService _dialogService;
    private readonly IJSRuntime _jsRuntime;

    public async Task DispatchAsync(DslEvent evt, DslComponent component, DslRenderContext context)
    {
        // 防抖处理
        if (evt.DebounceMs.HasValue && evt.DebounceMs.Value > 0)
        {
            await Task.Delay(evt.DebounceMs.Value);
        }

        // 确认对话框
        if (evt.Confirm != null)
        {
            var confirmed = await ShowConfirmAsync(evt.Confirm);
            if (!confirmed) return;
        }

        // 根据 handler 类型分发
        switch (evt.Handler)
        {
            case DslHandlers.Submit:
                await HandleSubmitAsync(evt, component, context);
                break;
            case DslHandlers.ApiCall:
                await HandleApiCallAsync(evt, component, context);
                break;
            case DslHandlers.Navigate:
                await HandleNavigateAsync(evt, context);
                break;
            case DslHandlers.OpenModal:
                await HandleOpenModalAsync(evt, context);
                break;
            case DslHandlers.CloseModal:
                await HandleCloseModalAsync(evt, context);
                break;
            case DslHandlers.Refresh:
                await HandleRefreshAsync(evt, context);
                break;
            case DslHandlers.SetValue:
                await HandleSetValueAsync(evt, context);
                break;
            case DslHandlers.ShowToast:
                await HandleShowToastAsync(evt);
                break;
            case DslHandlers.Export:
                await HandleExportAsync(evt, context);
                break;
            case DslHandlers.Validate:
                await HandleValidateAsync(evt, context);
                break;
            case DslHandlers.Chain:
                await HandleChainAsync(evt, component, context);
                break;
            default:
                // 尝试从容器中解析自定义 handler
                var customHandler = _serviceProvider.GetService<ICustomEventHandler>() 
                    ?? throw new InvalidOperationException($"Unknown handler: {evt.Handler}");
                await customHandler.HandleAsync(evt, component, context);
                break;
        }
    }

    private async Task HandleApiCallAsync(DslEvent evt, DslComponent component, DslRenderContext context)
    {
        var endpoint = ResolveTemplate(evt.Params?["endpoint"]?.ToString() ?? "", context);
        var method = evt.Params?["method"]?.ToString() ?? "GET";
        var formId = evt.Params?["formId"]?.ToString();
        
        object? payload = null;
        if (!string.IsNullOrEmpty(formId) && context.Forms.TryGetValue(formId, out var formState))
        {
            payload = formState.GetValues();
        }
        else if (component.DataBind != null)
        {
            payload = context.DataStore.Get(component.DataBind);
        }

        var client = _httpClientFactory.CreateClient("DslApi");
        using var response = method.ToUpper() switch
        {
            "GET" => await client.GetAsync(endpoint),
            "POST" => await client.PostAsJsonAsync(endpoint, payload),
            "PUT" => await client.PutAsJsonAsync(endpoint, payload),
            "DELETE" => await client.DeleteAsync(endpoint),
            _ => throw new NotSupportedException($"HTTP method {method} not supported")
        };

        response.EnsureSuccessStatusCode();
        var result = await response.Content.ReadFromJsonAsync<ApiResponse>();
        
        if (result?.Success == true)
        {
            // 执行成功回调链
            if (evt.Params?.ContainsKey("onSuccess") == true && evt.Params["onSuccess"] is List<Dictionary<string, object>> callbacks)
            {
                foreach (var cb in callbacks)
                {
                    var cbEvent = new DslEvent
                    {
                        Type = "callback",
                        Handler = cb["handler"].ToString()!,
                        Params = cb.GetValueOrDefault("params") as Dictionary<string, object>
                    };
                    await DispatchAsync(cbEvent, component, context);
                }
            }
        }
    }

    private async Task HandleChainAsync(DslEvent evt, DslComponent component, DslRenderContext context)
    {
        if (evt.Params?.GetValueOrDefault("chain") is not List<Dictionary<string, object>> chain) return;
        
        foreach (var step in chain)
        {
            var stepEvent = new DslEvent
            {
                Type = "chain",
                Handler = step["handler"].ToString()!,
                Params = step.GetValueOrDefault("params") as Dictionary<string, object>,
                Confirm = step.ContainsKey("confirm") ? MapConfirm(step["confirm"]) : null
            };
            await DispatchAsync(stepEvent, component, context);
        }
    }

    private string ResolveTemplate(string template, DslRenderContext context)
    {
        // 支持 {id} 占位符替换为 context.DataStore 中的值
        return Regex.Replace(template, @"\{(\w+)\}", m =>
        {
            var key = m.Groups[1].Value;
            var value = context.DataStore.GetString($"data.{key}") ?? context.DataStore.GetString($"row.{key}") ?? key;
            return Uri.EscapeDataString(value);
        });
    }

    private async Task<bool> ShowConfirmAsync(DslConfirm confirm)
    {
        var result = await _dialogService.ShowMessageBox(
            confirm.Title,
            confirm.Message,
            yesText: confirm.ConfirmText,
            cancelText: confirm.CancelText);
        return result == true;
    }

    private async Task HandleShowToastAsync(DslEvent evt)
    {
        var message = evt.Params?["message"]?.ToString() ?? "操作成功";
        var severity = Enum.TryParse<Severity>(evt.Params?["severity"]?.ToString(), out var s) ? s : Severity.Success;
        _snackbar.Add(message, severity);
    }

    private async Task HandleNavigateAsync(DslEvent evt, DslRenderContext context)
    {
        var path = ResolveTemplate(evt.Params?["path"]?.ToString() ?? "/", context);
        _navigation.NavigateTo(path);
    }

    // ... 其他 handler 实现
}
```

---

## 6. 服务端架构 (.NET 10)

### 6.1 项目分层

```
CJDSL/
├── CJDSL.Domain/                    # 领域层
│   ├── Entities/                    # 元模型实体 (M0-M7)
│   ├── Aggregates/                  # 聚合根
│   ├── ValueObjects/                # 值对象
│   ├── DomainEvents/                # 领域事件
│   ├── Services/                    # 领域服务
│   └── Interfaces/                  # 仓储接口
│
├── CJDSL.Application/               # 应用层
│   ├── Dsl/                         # DSL 相关 UseCase
│   │   ├── GenerateDslCommand.cs
│   │   ├── GenerateDslCommandHandler.cs
│   │   ├── GetDslQuery.cs
│   │   ├── AdaptDslCommand.cs
│   │   └── DslDto.cs
│   ├── MetaModel/                   # 元模型 UseCase
│   ├── Intent/                      # 意图理解 UseCase
│   └── Mapping/                     # AutoMapper Profile
│
├── CJDSL.Infrastructure/            # 基础设施层
│   ├── LLM/                         # 大模型适配器
│   │   ├── ILLMClient.cs
│   │   ├── OpenAIClient.cs
│   │   ├── LocalLLMClient.cs        # Ollama / vLLM
│   │   ├── DslPromptTemplates.cs
│   │   └── DslResponseParser.cs     # 解析 LLM 输出的 JSON
│   ├── Persistence/                 # 数据持久化
│   │   ├── EF Core DbContext
│   │   ├── Configurations/
│   │   └── Migrations/
│   ├── Caching/                     # 缓存
│   ├── Expression/                  # 表达式引擎
│   └── External/                    # 外部服务集成
│
├── CJDSL.Api/                       # 接口层 (Blazor Server + Minimal API)
│   ├── Program.cs
│   ├── Endpoints/                   # Minimal API 路由
│   ├── Hubs/                        # SignalR Hub (实时推送 DSL)
│   ├── Middleware/
│   └── wwwroot/
│
├── CJDSL.Blazor/                    # Blazor 共享层
│   ├── Components/                  # 渲染引擎组件
│   ├── Dsl/                         # DSL 客户端解析
│   ├── Events/                      # 客户端事件处理
│   ├── DataStore/                   # 客户端数据存储
│   └── Services/                    # 客户端服务
│
├── CJDSL.Web/                       # Web 入口 (Blazor Server / WASM)
│   ├── App.razor
│   ├── Pages/
│   ├── Shared/
│   └── Program.cs
│
└── CJDSL.Tests/                     # 测试层
    ├── Unit/
    ├── Integration/
    └── DslSamples/                  # DSL 测试用例
```

### 6.2 Minimal API 端点设计

```csharp
// Program.cs 中的路由注册
public static class DslEndpoints
{
    public static IEndpointRouteBuilder MapDslEndpoints(this IEndpointRouteBuilder app)
    {
        var group = app.MapGroup("/api/dsl")
            .WithTags("DSL")
            .WithOpenApi()
            .RequireAuthorization();

        // 基于元模型生成 DSL
        group.MapPost("/generate", async (
            GenerateDslRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new GenerateDslCommand(
                request.MetaObjectCode,
                request.Layout,
                request.UserContext,
                request.Options);
            var result = await mediator.Send(command, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

        // 基于自然语言生成 DSL
        group.MapPost("/generate-from-nlp", async (
            GenerateFromNlpRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new GenerateDslFromNlpCommand(
                request.Description,
                request.UserContext,
                request.Options);
            var result = await mediator.Send(command, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

        // 基于当前上下文动态调整 DSL
        group.MapPost("/adapt", async (
            AdaptDslRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var command = new AdaptDslCommand(
                request.BaseDsl,
                request.UserContext,
                request.DataContext);
            var result = await mediator.Send(command, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.BadRequest(result.Errors);
        });

        // 获取页面 DSL（带缓存）
        group.MapGet("/page/{pageCode}", async (
            string pageCode,
            [FromQuery] string? role,
            [FromQuery] string? device,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetDslQuery(pageCode, role, device);
            var result = await mediator.Send(query, ct);
            return result.IsSuccess ? Results.Ok(result.Value) : Results.NotFound();
        });

        // 获取组件数据源
        group.MapGet("/datasource/{sourceCode}", async (
            string sourceCode,
            [AsParameters] DataSourceRequest request,
            IMediator mediator,
            CancellationToken ct) =>
        {
            var query = new GetDataSourceQuery(sourceCode, request);
            var result = await mediator.Send(query, ct);
            return Results.Ok(result.Value);
        });

        // 验证 DSL 语法
        group.MapPost("/validate", async (
            DslPage dsl,
            [FromServices] IDslValidator validator) =>
        {
            var result = await validator.ValidateAsync(dsl);
            return Results.Ok(result);
        });

        return app;
    }
}

// 请求/响应 DTO
public record GenerateDslRequest(
    string MetaObjectCode,
    string Layout,
    UserContext UserContext,
    GenerateOptions? Options = null);

public record GenerateFromNlpRequest(
    string Description,
    UserContext UserContext,
    GenerateOptions? Options = null);

public record AdaptDslRequest(
    DslPage BaseDsl,
    UserContext UserContext,
    Dictionary<string, object>? DataContext = null);

public record UserContext(
    string UserId,
    string UserName,
    List<string> Roles,
    List<string> Permissions,
    string? Department = null,
    string? TenantId = null);
```

### 6.3 LLM 集成与 DSL 生成流水线

```csharp
public class GenerateDslCommandHandler : IRequestHandler<GenerateDslCommand, Result<DslPage>>
{
    private readonly ILLMClient _llmClient;
    private readonly IMetaModelRepository _metaModelRepo;
    private readonly IDslPromptBuilder _promptBuilder;
    private readonly IDslResponseParser _parser;
    private readonly IDslCache _cache;
    private readonly ILogger<GenerateDslCommandHandler> _logger;

    public async Task<Result<DslPage>> Handle(GenerateDslCommand request, CancellationToken ct)
    {
        // 1. 构建缓存键
        var cacheKey = $"dsl:{request.MetaObjectCode}:{request.Layout}:{string.Join(",", request.UserContext.Roles)}:{request.Options?.DeviceType}";
        
        // 2. 尝试命中缓存
        var cached = await _cache.GetAsync<DslPage>(cacheKey, ct);
        if (cached != null) return Result.Success(cached);

        // 3. 加载元模型
        var metaObject = await _metaModelRepo.GetObjectAsync(request.MetaObjectCode, ct);
        if (metaObject == null) return Result.Failure<DslPage>("MetaObject.NotFound", $"未找到元对象: {request.MetaObjectCode}");

        // 4. 构建提示词
        var prompt = _promptBuilder.Build(metaObject, request.Layout, request.UserContext, request.Options);
        
        // 5. 调用 LLM
        var llmResponse = await _llmClient.GenerateAsync(new LLMRequest
        {
            Prompt = prompt,
            Temperature = request.Options?.Temperature ?? 0.3f,
            MaxTokens = 4096,
            ResponseFormat = "json_object"  // 强制 JSON 输出
        }, ct);

        // 6. 解析响应
        var dslPage = _parser.Parse(llmResponse.Text);
        if (dslPage == null) return Result.Failure<DslPage>("Dsl.ParseError", "LLM 生成的 DSL 无法解析");

        // 7. 后处理：注入权限、数据源、验证规则
        dslPage = PostProcess(dslPage, metaObject, request.UserContext);

        // 8. 验证 DSL 语义正确性
        var validation = await _validator.ValidateAsync(dslPage, ct);
        if (!validation.IsValid) return Result.Failure<DslPage>("Dsl.ValidationError", string.Join("; ", validation.Errors));

        // 9. 缓存结果
        await _cache.SetAsync(cacheKey, dslPage, TimeSpan.FromMinutes(10), ct);

        return Result.Success(dslPage);
    }

    private DslPage PostProcess(DslPage dsl, M1_Object metaObject, UserContext user)
    {
        // 注入数据绑定
        foreach (var prop in metaObject.Properties)
        {
            var component = FindComponentByFieldName(dsl, prop.Code);
            if (component != null)
            {
                component.DataBind ??= $"@data.{prop.Code}";
                component.Label ??= prop.Name;
                component.FieldName = prop.Code;
                
                // 注入验证规则
                if (prop.Required && (component.ValidationRules == null || !component.ValidationRules.Any(r => r.Type == "required")))
                {
                    component.ValidationRules ??= new List<DslValidationRule>();
                    component.ValidationRules.Add(new DslValidationRule { Type = "required", Message = $"{prop.Name}必填" });
                }
                
                // 注入状态权限控制
                if (prop.StatePermissions?.Any() == true)
                {
                    var visibleActions = prop.StatePermissions
                        .Where(sp => sp.Visible)
                        .Select(sp => sp.Action);
                    if (visibleActions.Any())
                    {
                        component.VisibleIf = $"@user.canView('{prop.Code}')";
                    }
                }
            }
        }

        // 注入数据源配置
        foreach (var prop in metaObject.Properties.Where(p => !string.IsNullOrEmpty(p.DictCode)))
        {
            var component = FindComponentByFieldName(dsl, prop.Code);
            if (component != null)
            {
                component.DataSource = new DslDataSource
                {
                    Type = "dictionary",
                    Code = prop.DictCode
                };
            }
        }

        return dsl;
    }

    private DslComponent? FindComponentByFieldName(DslPage dsl, string fieldName)
    {
        return dsl.Components.SelectMany(c => c.Descendants()).FirstOrDefault(c => c.FieldName == fieldName);
    }
}
```

---

## 7. 数据流与状态管理

### 7.1 客户端状态架构

```csharp
// 类似 Redux 的不可变状态存储
public class DslDataStore
{
    private readonly Dictionary<string, object> _data = new();
    private readonly JsonSerializerOptions _jsonOptions = new() { PropertyNameCaseInsensitive = true };

    public event EventHandler<DataChangedEventArgs>? DataChanged;

    public void Set(string path, object? value)
    {
        var oldValue = _data.GetValueOrDefault(path);
        if (value == null)
            _data.Remove(path);
        else
            _data[path] = value;
        
        DataChanged?.Invoke(this, new DataChangedEventArgs(path, oldValue, value));
    }

    public object? Get(string path)
    {
        // 支持嵌套路径：data.user.name
        if (path.StartsWith('@')) path = path.Substring(1);
        
        var segments = path.Split('.');
        var current = _data.GetValueOrDefault(segments[0]);
        
        foreach (var segment in segments.Skip(1))
        {
            if (current == null) return null;
            current = GetProperty(current, segment);
        }
        
        return current;
    }

    public T? Get<T>(string path) => Get(path) is T value ? value : default;
    public string? GetString(string path) => Get(path)?.ToString();
    public List<T>? GetList<T>(string path) => Get(path) as List<T>;

    public void Merge(Dictionary<string, object> data)
    {
        foreach (var kv in data) Set(kv.Key, kv.Value);
    }

    private static object? GetProperty(object target, string propertyName)
    {
        if (target is JsonElement jsonElement)
        {
            if (jsonElement.ValueKind == JsonValueKind.Object && jsonElement.TryGetProperty(propertyName, out var prop))
                return prop;
            return null;
        }
        
        var property = target.GetType().GetProperty(propertyName, 
            System.Reflection.BindingFlags.Public | System.Reflection.BindingFlags.Instance | System.Reflection.BindingFlags.IgnoreCase);
        return property?.GetValue(target);
    }
}

// 表达式求值引擎（用于 VisibleIf / DisabledIf / DataBind）
public interface IExpressionEvaluator
{
    T Evaluate<T>(string expression, DslDataStore dataStore);
}

public class JintExpressionEvaluator : IExpressionEvaluator
{
    private readonly Engine _engine = new();

    public T Evaluate<T>(string expression, DslDataStore dataStore)
    {
        // 注入数据到 JS 引擎上下文
        var context = new { data = dataStore.Get("data"), user = dataStore.Get("user"), row = dataStore.Get("row") };
        _engine.SetValue("$ctx", context);
        
        // 预置辅助函数
        _engine.SetValue("today", DateTime.Today);
        _engine.SetValue("now", DateTime.Now);
        
        var result = _engine.Evaluate(expression);
        return result.ToObject() is T typed ? typed : default!;
    }
}
```

### 7.2 完整数据流

```
用户打开页面 → 路由解析 pageCode
    ↓
Blazor 页面加载 → 调用 /api/dsl/page/{pageCode}
    ↓
服务端检查缓存 → 未命中则进入生成流水线
    ↓
生成流水线：
    a. 加载 M1_Object 元模型
    b. 构建 LLM Prompt（含用户上下文）
    c. 调用 LLM 生成原始 DSL
    d. 后处理（注入权限、数据源、验证）
    e. 验证 DSL 语法与语义
    f. 存入缓存并返回
    ↓
Blazor 接收 DSL JSON → 反序列化为 DslPage
    ↓
DslPageRenderer 递归渲染 → 构建 MudBlazor 组件树
    ↓
组件挂载后 → 加载数据源（DataSource）
    ↓
用户交互 → 触发 DslEvent → EventDispatcher 处理
    ↓
API 调用 → 更新 DslDataStore → 触发 StateHasChanged
    ↓
UI 自动更新（无需手动操作 DOM）
```

---

## 8. 关键特性设计

### 8.1 智能布局自适应

```csharp
public class DslResponsive
{
    public Dictionary<string, BreakpointConfig> Breakpoints { get; set; } = new()
    {
        ["xs"] = new() { Columns = 1, ComponentSize = "Small" },    // <600px
        ["sm"] = new() { Columns = 1, ComponentSize = "Small" },    // 600-960px
        ["md"] = new() { Columns = 2, ComponentSize = "Medium" },   // 960-1280px
        ["lg"] = new() { Columns = 2, ComponentSize = "Medium" },   // 1280-1920px
        ["xl"] = new() { Columns = 3, ComponentSize = "Large" }     // >1920px
    };
}

// 渲染时动态计算 span
public int ComputeSpan(DslComponent component, string breakpoint)
{
    var baseSpan = component.Span ?? 12;
    var columns = Breakpoints.GetValueOrDefault(breakpoint)?.Columns ?? 1;
    return Math.Min(baseSpan, 12 / columns);
}
```

### 8.2 渐进式增强策略

```csharp
public class ProgressiveEnhancementEngine
{
    /// <summary>根据设备能力和网络状况决定功能级别</summary>
    public EnhancementLevel DetermineLevel(ClientCapabilities caps)
    {
        if (caps.IsLowBandwidth) return EnhancementLevel.Basic;      // 仅基础表单
        if (caps.IsMobile) return EnhancementLevel.Standard;        // 标准组件
        if (caps.SupportsWebAssembly) return EnhancementLevel.Full; // 完整功能
        return EnhancementLevel.Standard;
    }
}

public enum EnhancementLevel
{
    Basic,      // 仅文本、数字、选择、按钮、表格
    Standard,   // + 日期、文件上传、标签页、分页
    Full        // + 图表、地图、富文本、看板、日历
}
```

### 8.3 DSL 热更新与版本控制

```csharp
public class DslVersionManager
{
    // DSL 版本控制，支持灰度发布
    public async Task<DslPage> GetVersionAsync(string pageCode, string? version = null, UserContext? user = null)
    {
        if (version == null && user != null)
        {
            // 根据用户分组获取对应的灰度版本
            version = await _abTestService.GetVersionForUser(pageCode, user);
        }
        
        return await _dslRepo.GetAsync(pageCode, version ?? "latest");
    }
}
```

### 8.4 安全性设计

```csharp
public class DslSecurityValidator
{
    // 1. 防止 XSS：Props 中的 HTML 需要经过消毒
    public void SanitizeProps(DslComponent component)
    {
        if (component.Props == null) return;
        foreach (var key in component.Props.Keys.ToList())
        {
            if (component.Props[key] is string str && key.Contains("Html", StringComparison.OrdinalIgnoreCase))
            {
                component.Props[key] = SanitizeHtml(str);
            }
        }
    }

    // 2. 防止注入：VisibleIf / DisabledIf 表达式沙箱执行
    public void ValidateExpression(string expression)
    {
        var forbidden = new[] { "eval", "Function", "constructor", "prototype", "import", "require" };
        if (forbidden.Any(f => expression.Contains(f, StringComparison.OrdinalIgnoreCase)))
            throw new SecurityException("表达式包含非法关键词");
    }

    // 3. API 端点白名单验证
    public void ValidateEndpoint(string endpoint)
    {
        if (!endpoint.StartsWith("/api/"))
            throw new SecurityException("API 端点必须以 /api/ 开头");
    }
}
```

---

## 9. 实现路线图

### Phase 1: 基础框架 (4-6 周)
- [ ] 搭建 .NET 10 + Blazor + MudBlazor 项目骨架
- [ ] 实现基础 DSL Schema 和 JSON 序列化
- [ ] 实现核心渲染引擎（支持 text, number, select, button, card, form, grid, stack）
- [ ] 实现事件分发器（submit, apiCall, navigate, showToast）
- [ ] 实现 DslDataStore 和表达式引擎
- [ ] 实现服务端 DSL 生成 API（基础版，无 LLM）
- [ ] 实现元模型 CRUD（M0-M1）

### Phase 2: 智能生成 (4-6 周)
- [ ] 集成 OpenAI / 本地 LLM 适配器
- [ ] 实现 Prompt Builder 和 Response Parser
- [ ] 实现基于 M1_Object 的 DSL 自动生成
- [ ] 实现基于自然语言的 DSL 生成
- [ ] 实现 DSL 后处理引擎（权限注入、数据源绑定）
- [ ] 实现 DSL 缓存与版本控制
- [ ] 实现 DSL 验证器（语法 + 语义）

### Phase 3: 高级组件 (4-6 周)
- [ ] 实现表格/数据网格渲染器（支持排序、筛选、分页）
- [ ] 实现标签页、步骤条、折叠面板渲染器
- [ ] 实现文件上传、富文本、图表渲染器
- [ ] 实现对话框/弹窗系统
- [ ] 实现树形控件、时间线、看板渲染器
- [ ] 实现仪表盘布局（grid-stack / draggable）

### Phase 4: 生产就绪 (4-6 周)
- [ ] 实现响应式布局断点系统
- [ ] 实现渐进式增强策略
- [ ] 实现 SSR / WASM 混合模式
- [ ] 实现 DSL 热更新（SignalR）
- [ ] 实现安全沙箱（表达式验证、XSS 防护）
- [ ] 性能优化（虚拟化、懒加载、缓存）
- [ ] 完善测试覆盖（单元测试、集成测试、E2E）
- [ ] 文档与示例系统

---

## 10. 与 AIPage 的对比演进

| 维度 | AIPage (React) | CJDSL (Blazor) |
|------|---------------|----------------|
| 技术栈 | React + Ant Design | .NET 10 + Blazor + MudBlazor |
| 渲染方式 | CSR (ReactDOM) | Blazor 组件树（Server/WASM） |
| 状态管理 | React Context + useState | DslDataStore (类似 Redux) |
| 事件处理 | 直接回调 | DslEventDispatcher + 链式 Handler |
| 组件库 | Ant Design 映射 | MudBlazor 完整映射 |
| 数据源 | 静态 mock | API + 字典 + 枚举 + 实时数据 |
| LLM 集成 | 简单关键词提取 | 完整 Prompt Engineering + 后处理 |
| 权限控制 | 无 | 字段级 + 按钮级 visibleIf / disabledIf |
| 响应式 | 无 | 断点自适应 + 设备检测 |
| 验证 | 基础 required | 完整 validationRules + 自定义规则 |
| 元模型 | M0-M7 定义 | M0-M7 + 运行时推理引擎 |
| 缓存 | 无 | 多级缓存（Redis + Memory） |
| 安全性 | 前端验证 | 前后端双重验证 + 沙箱 |
| 部署 | 静态文件 | 完整 .NET 应用 + 热更新 |

---

## 11. 核心优势总结

1. **零前端代码**：业务需求变更时，只需调整元模型或自然语言描述，LLM 自动生成新 DSL，无需编写/修改任何前端组件代码
2. **统一渲染引擎**：一套渲染引擎支持所有业务场景，减少 80%+ 的前端组件开发工作量
3. **上下文感知**：DSL 根据当前用户角色、权限、设备、数据状态动态生成，实现真正的千人千面
4. **元模型驱动**：七维元模型不仅驱动 UI，还驱动 API、数据库、权限、流程，实现全栈一致性
5. **渐进增强**：根据客户端能力自动降级或升级功能，保证最佳体验
6. **热更新能力**：DSL 作为数据而非代码，可以实时推送更新，无需重新发版
7. **测试友好**：DSL 是纯 JSON，可以精确比对、快照测试、回归验证
8. **跨平台复用**：同一份 DSL 可以同时渲染为 Web、Mobile Web、甚至通过其他渲染引擎变为 Native

---

> **设计愿景**：让大模型成为"界面架构师"，让人类专注于业务逻辑与元模型设计，让机器处理繁琐的界面细节。
