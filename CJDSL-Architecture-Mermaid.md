# CJDSL 系统架构 Mermaid 图

## 1. 总体架构

```mermaid
graph TB
    subgraph "用户层"
        U[用户/浏览器]
    end

    subgraph "LLM 层"
        LLM[OpenAI / Local LLM]
        PB[Prompt Builder]
        RP[Response Parser]
    end

    subgraph ".NET 10 服务端"
        API[Minimal API<br/>DslEndpoints]
        CQRS[MediatR + CQRS]
        DG[DSL 生成器]
        DA[DSL 适配器]
        DC[DSL 缓存]
        MM[元模型服务<br/>M0-M7]
        IP[意图理解引擎]
    end

    subgraph "数据层"
        PG[(PostgreSQL<br/>SQLite)]
        RD[(Redis<br/>缓存)]
        VD[(Vector DB)]
        S3[(MinIO<br/>对象存储)]
    end

    subgraph "Blazor 客户端"
        PR[DslPageRenderer<br/>根渲染器]
        CR[递归<br/>DslComponentRenderer]
        DS[DslDataStore<br/>状态管理]
        ED[DslEventDispatcher<br/>事件分发]
        EE[ExpressionEvaluator<br/>条件表达式]
    end

    subgraph "MudBlazor 组件树"
        MC[MudCard / MudForm / MudGrid]
        MF[MudTextField / MudSelect / MudDatePicker]
        MT[MudTable / MudDataGrid]
        MB[MudButton / MudIconButton / MudDialog]
    end

    subgraph "七维元模型"
        M0[M0: 基础数据]
        M1[M1: 对象模型]
        M15[M1.5: 关系模型]
        M2[M2: 行为模型]
        M3[M3: 规则模型]
        M4[M4: 场景模型]
        M5[M5: 主体模型]
        M6[M6: 异常补偿]
        M7[M7: 质量约束]
    end

    subgraph "CJDSL 规范"
        DSL1[DslPage]
        DSL2[DslComponent]
        DSL3[DslEvent / DataBind]
        DSL4[VisibleIf / DisabledIf]
        DSL5[ValidationRules]
        DSL6[DslDataSource]
        DSL7[DslPermission]
        DSL8[Responsive]
    end

    U -->|自然语言| LLM
    U -->|请求 DSL| API
    LLM -->|Prompt| PB
    PB -->|DSL JSON| RP
    RP -->|原始 DSL| DG
    DG -->|后处理| DA
    DA -->|缓存| DC
    DC -->|DSL JSON| API
    API -->|返回 DSL| PR
    PR -->|递归渲染| CR
    CR -->|映射| MC
    CR -->|映射| MF
    CR -->|映射| MT
    CR -->|映射| MB
    CR -->|读写| DS
    ED -->|触发| API
    EE -->|计算条件| DS
    MC -->|用户交互| ED
    MF -->|用户交互| ED
    MT -->|用户交互| ED
    MB -->|用户交互| ED
    MM -->|元数据| DG
    MM -->|元数据| M0
    MM -->|元数据| M1
    MM -->|元数据| M15
    MM -->|元数据| M2
    MM -->|元数据| M3
    MM -->|元数据| M4
    MM -->|元数据| M5
    MM -->|元数据| M6
    MM -->|元数据| M7
    API -->|CRUD| PG
    API -->|Cache| RD
    API -->|Vector| VD
    API -->|Storage| S3
```

## 2. DSL 生成流水线

```mermaid
flowchart LR
    A[用户输入] --> B{输入类型?}
    B -->|自然语言| C[IntentParser<br/>意图解析]
    B -->|元对象编码| D[加载 M1_Object]
    C --> E[PromptBuilder<br/>构建 Prompt]
    D --> E
    E --> F[LLMClient<br/>调用大模型]
    F --> G[ResponseParser<br/>解析 JSON]
    G --> H{解析成功?}
    H -->|否| I[返回错误 / 重试]
    H -->|是| J[PostProcessor<br/>后处理]
    J --> K[注入权限控制]
    J --> L[注入数据源绑定]
    J --> M[注入验证规则]
    K --> N[DslValidator<br/>语义验证]
    L --> N
    M --> N
    N --> O{验证通过?}
    O -->|否| P[修复/降级]
    O -->|是| Q[DslCache<br/>缓存结果]
    P --> Q
    Q --> R[返回 DslPage]
```

## 3. 客户端渲染流程

```mermaid
sequenceDiagram
    participant U as 用户
    participant B as Blazor Router
    participant R as DslPageRenderer
    participant C as DslComponentRenderer
    participant S as DslDataStore
    participant M as MudBlazor
    participant API as .NET API

    U->>B: 打开 /repair/form
    B->>API: GET /api/dsl/page/repair-form
    API-->>B: DslPage JSON
    B->>R: 传入 DslPage
    R->>R: 反序列化 + 创建 RenderContext
    R->>C: 递归渲染 Components
    C->>C: 解析 VisibleIf/DisabledIf
    C->>S: 注册数据绑定
    C->>M: 实例化 MudBlazor 组件
    M-->>U: 呈现界面
    U->>M: 输入数据
    M->>S: 更新 DataStore
    U->>M: 点击提交按钮
    M->>C: 触发 onClick 事件
    C->>S: 获取表单数据
    C->>API: POST /api/repair/submit
    API-->>C: 返回结果
    C->>S: 更新状态
    C->>M: 显示 Toast / 跳转
```

## 4. 七维元模型层次

```mermaid
graph BT
    M7[M7: 质量约束模型<br/>响应时间/可用性/SLA]
    M6[M6: 异常补偿模型<br/>重试/补偿/Saga]
    M5[M5: 主体模型<br/>参与者/角色/权限]
    M4[M4: 场景模型<br/>流程/用例/编排]
    M3[M3: 规则模型<br/>验证/计算/推导]
    M2[M2: 行为模型<br/>动作/前置/后置]
    M15[M1.5: 关系模型<br/>关联/继承/聚合]
    M1[M1: 对象模型<br/>实体/属性/生命周期]
    M0[M0: 基础数据模型<br/>枚举/字典/量纲]

    M0 --> M1
    M1 --> M15
    M1 --> M2
    M2 --> M3
    M2 --> M4
    M4 --> M5
    M4 --> M6
    M6 --> M7
    M3 --> M4
    M5 --> M6
```

## 5. 数据绑定与表达式求值

```mermaid
graph LR
    subgraph "CJDSL 表达式"
        E1["@data.user.name"]
        E2["@user.hasPermission('repair:create')"]
        E3["@row.status == 'completed'"]
        E4["@datasource.totalCount > 0"]
        E5["today() - @data.repairDate < 7"]
    end

    subgraph "ExpressionEvaluator"
        J[Jint / JavaScript 引擎]
        C[上下文注入]
    end

    subgraph "DslDataStore"
        D1[data.user.name = "张三"]
        D2[user.permissions = ["repair:create"]]
        D3[row.status = "pending"]
        D4[datasource.totalCount = 42]
        D5[data.repairDate = 2026-06-10]
    end

    E1 --> J
    E2 --> J
    E3 --> J
    E4 --> J
    E5 --> J
    D1 --> C
    D2 --> C
    D3 --> C
    D4 --> C
    D5 --> C
    C --> J
    J -->|求值结果| R[true/false/string/number]
```
