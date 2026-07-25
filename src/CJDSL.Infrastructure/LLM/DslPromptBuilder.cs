using System.Text.Json;
using CJDSL.Domain;
using CJDSL.Domain.Configuration;
using CJDSL.Domain.Entities.MetaModel;
using CJDSL.Infrastructure.Configuration;

namespace CJDSL.Infrastructure.LLM;

/// <summary>
/// DSL Prompt 构建器
/// </summary>
public interface IDslPromptBuilder
{
    string BuildSystemPrompt();
    string BuildFormPrompt(M1_Object metaObject, GenerateOptions options);
    string BuildListPrompt(M1_Object metaObject, GenerateOptions options);
    string BuildNlpPrompt(string description, UserContext user, GenerateOptions options);
    string BuildDashboardPrompt(M4_Scene scene, GenerateOptions options);
}

public class DslPromptBuilder : IDslPromptBuilder
{
    private readonly SystemConfigService _configService;

    public DslPromptBuilder(SystemConfigService configService)
    {
        _configService = configService;
    }

    private DslPromptConfig GetConfig() => _configService.GetDslPromptConfig();

    public string BuildSystemPrompt()
    {
        var config = GetConfig();
        if (!string.IsNullOrWhiteSpace(config.SystemPrompt))
            return config.SystemPrompt;

        return DefaultSystemPrompt;
    }

    public string BuildFormPrompt(M1_Object metaObject, GenerateOptions options)
    {
        var config = GetConfig();
        if (!string.IsNullOrWhiteSpace(config.FormPromptTemplate))
        {
            return ApplyFormPlaceholders(config.FormPromptTemplate, metaObject, options);
        }

        var properties = string.Join("\n", metaObject.Properties.Select(p =>
            $"- {p.Name} ({p.Code}): 类型={p.Type}, 必填={p.Required}, 字典={p.DictCode ?? "无"}, 长度={p.Length?.ToString() ?? "N/A"}, 描述={p.Description}"));

        var states = string.Join("\n", metaObject.LifeCycleStates.Select(s =>
            $"- {s.Name} ({s.Code}): 起始={s.IsStart}, 终止={s.IsEnd}, 可转移={string.Join(", ", s.NextStates)}"));

        return $@"
根据以下业务对象元模型，生成一份表单布局的 CJDSL JSON。

## 业务对象信息
- 对象名称：{metaObject.Name}
- 对象编码：{metaObject.Code}
- 描述：{metaObject.Description}

## 属性列表
{properties}

## 生命周期状态
{states}

## 用户上下文
- 角色：{string.Join(", ", options.Roles)}
- 设备：{options.DeviceType}
- 目标平台：{options.TargetPlatform}
- 布局偏好：{options.Preference.Density}

## 生成要求
1. layout: ""form""
2. 输出 DslPage 中必须包含 ""targetPlatform"": ""{options.TargetPlatform}""
3. 最外层使用 card 组件包裹
4. 内部使用 form 组件包含字段
5. 字段使用 grid 布局，每行 2 列（桌面端），每列 span=6
6. 根据字段类型选择合适的组件类型：
   - string -> text
   - number -> number
   - date -> date
   - select -> select（配置 DataSource 指向字典编码）
   - textarea -> textarea
   - boolean -> switch
7. 必填字段配置 Required=true 和 validationRules（required 类型）
8. 有字典编码的字段配置 DataSource type=dictionary
9. 表单底部添加按钮区域（stack Row=true Justify=flex-end）：
   - 重置按钮：handler=reset, params formId=formId
   - 保存按钮：handler=submit, params endpoint=/api/{metaObject.Code}/save
   - 提交按钮：handler=chain, 包含 validate + apiCall + showToast + navigate
10. 输出必须是纯 JSON，不要包含 Markdown 代码块标记
";
    }

    public string BuildListPrompt(M1_Object metaObject, GenerateOptions options)
    {
        var config = GetConfig();
        if (!string.IsNullOrWhiteSpace(config.ListPromptTemplate))
        {
            return ApplyFormPlaceholders(config.ListPromptTemplate, metaObject, options);
        }

        var columns = string.Join("\n", metaObject.Properties.Take(8).Select(p =>
            $"- {p.Name} ({p.Code})"));

        return $@"
根据以下业务对象，生成一份列表布局的 CJDSL JSON。

## 业务对象信息
- 对象名称：{metaObject.Name}
- 对象编码：{metaObject.Code}

## 列表列（取前 8 个属性）
{columns}

## 用户上下文
- 目标平台：{options.TargetPlatform}

## 生成要求
1. layout: ""list""
2. 输出 DslPage 中必须包含 ""targetPlatform"": ""{options.TargetPlatform}""
3. 包含搜索区域（stack Row=true）：
   - 搜索输入框（text，Placeholder=搜索）
   - 查询按钮（button，Color=Primary）
   - 新增按钮（button，Variant=Outlined）
4. 表格区域（table 组件）：
   - columns 配置为上述列
   - rowKey=id
   - pagination=true
5. 输出必须是纯 JSON
";
    }

    public string BuildNlpPrompt(string description, UserContext user, GenerateOptions options)
    {
        var config = GetConfig();
        if (!string.IsNullOrWhiteSpace(config.NlpPromptTemplate))
        {
            var template = config.NlpPromptTemplate;
            template = template.Replace("{描述}", description);
            template = template.Replace("{角色}", string.Join(", ", user.Roles));
            template = template.Replace("{设备}", options.DeviceType);
            return template;
        }

        return $@"
根据以下自然语言描述，生成一份 CJDSL JSON。

## 描述
{description}

## 用户上下文
- 角色：{string.Join(", ", user.Roles)}
- 设备：{options.DeviceType}
- 目标平台：{options.TargetPlatform}

## 生成要求
1. 从描述中提取业务对象和字段
2. 推断合适的字段类型（text, number, date, select, textarea）
3. 选择最合适的布局（form, list, detail）
4. 输出 DslPage 中必须包含 ""targetPlatform"": ""{options.TargetPlatform}""
5. 生成完整的 CJDSL 组件树
6. 输出必须是纯 JSON
";
    }

    public string BuildDashboardPrompt(M4_Scene scene, GenerateOptions options)
    {
        return $@"
根据以下业务场景元模型，生成一份仪表盘（dashboard）布局的 CJDSL JSON。

## 场景信息
- 场景名称：{scene?.Name ?? "数据仪表盘"}
- 场景编码：{scene?.Code ?? "default"}
- 描述：{scene?.Description ?? ""}

## 用户上下文
- 角色：{string.Join(", ", options.Roles)}
- 目标平台：{options.TargetPlatform}
- 布局偏好：{options.Preference.Density}

## 生成要求
1. layout: ""dashboard""
2. 输出 DslPage 中必须包含 ""targetPlatform"": ""{options.TargetPlatform}""
3. 顶部一排统计卡片（grid, Spacing=3），每张卡片用 card 包裹一个关键指标（如业务对象数、枚举项数、字典项数、今日待办等），用 textDisplay 展示标签与数值
4. 一个趋势图卡片（card 内含 chart 组件，ChartType=line，Title 描述趋势）
5. 一个最近记录卡片（card 内含 list，含若干 listItem 占位）
6. 正确嵌套：grid > card > textDisplay / chart / list
7. 输出必须是纯 JSON，不要包含 Markdown 代码块标记
";
    }

    private static string ApplyFormPlaceholders(string template, M1_Object metaObject, GenerateOptions options)
    {
        var properties = string.Join("\n", metaObject.Properties.Select(p =>
            $"- {p.Name} ({p.Code}): 类型={p.Type}, 必填={p.Required}, 字典={p.DictCode ?? "无"}"));
        var states = string.Join("\n", metaObject.LifeCycleStates.Select(s =>
            $"- {s.Name} ({s.Code}): 起始={s.IsStart}, 终止={s.IsEnd}"));

        template = template.Replace("{对象名称}", metaObject.Name);
        template = template.Replace("{对象编码}", metaObject.Code);
        template = template.Replace("{描述}", metaObject.Description);
        template = template.Replace("{属性列表}", properties);
        template = template.Replace("{状态列表}", states);
        template = template.Replace("{角色}", string.Join(", ", options.Roles));
        template = template.Replace("{设备}", options.DeviceType);
        template = template.Replace("{平台}", options.TargetPlatform.ToString());
        template = template.Replace("{密度}", options.Preference.Density);
        return template;
    }

    public const string DefaultSystemPrompt = @"
你是一位精通 CJDSL Schema v2 的前端架构师和 DSL 设计师。
你的任务是根据提供的业务元模型或自然语言描述，生成一份合法的 CJDSL JSON。

## CJDSL Schema 规范

### DslPage 结构
{
  ""id"": ""string"",
  ""title"": ""string"",
  ""description"": ""string"",
  ""layout"": ""form|list|detail|dashboard|custom"",
  ""targetPlatform"": ""Web|Wpf|Maui|React|Vue"",
  ""components"": [ DslComponent ],
  ""dataSource"": { ... },
  ""permission"": { ""requiredRoles"": [], ""requiredPermissions"": [] },
  ""style"": { ... }
}

### DslComponent 结构
{
  ""id"": ""string"",
  ""type"": ""text|number|select|date|textarea|switch|button|card|form|grid|stack|table|divider|textDisplay|..."",
  ""label"": ""string"",
  ""fieldName"": ""string"",
  ""dataBind"": ""@data.fieldName"",
  ""span"": 12,
  ""visibleIf"": ""expression"",
  ""disabledIf"": ""expression"",
  ""props"": { },
  ""children"": [ DslComponent ],
  ""events"": [ { ""type"": ""onClick|onChange|onSubmit"", ""handler"": ""submit|apiCall|navigate|showToast|chain|reset|validate"", ""params"": { } } ],
  ""validationRules"": [ { ""type"": ""required|regex|minLength|maxLength|email|custom"", ""message"": ""string"" } ],
  ""dataSource"": { ""type"": ""dictionary|enum|api|static"", ""code"": ""string"" }
}

### 重要规则
1. 使用 MudBlazor 的 Props 命名（PascalCase）：Required, ReadOnly, Variant, Color, Elevation, Class, Placeholder, AdornmentIcon, Lines, MaxLength
2. 表单组件 span: 桌面端每行 2 列用 6，移动端 1 列用 12
3. Grid 布局组件 type: ""grid""，子组件用 span 属性
4. Stack 布局组件 type: ""stack""，props: { ""Row"": true, ""Justify"": ""flex-end"", ""Spacing"": 2 }
5. 条件渲染表达式使用 JavaScript 语法，如 ""user.roles.includes('admin')""
6. 按钮事件 handler 必须是预定义值：submit, apiCall, navigate, showToast, chain, reset, validate
7. 输出必须是纯 JSON，不要包含任何 Markdown 标记或解释文字
8. 组件 type 必须是以下合法值之一：text, number, select, date, datetime, textarea, switch, checkbox, radio, file, button, iconButton, card, form, grid, stack, table, dataGrid, list, tabs, stepper, divider, textDisplay, paper, avatar, chip, badge, tooltip, skeleton, pagination, tree, progress, chart, markdown, codeBlock, jsonEditor, richText, calendar, map, iframe, custom
";
}
