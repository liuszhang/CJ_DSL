using CJDSL.Domain;
using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Entities.MetaModel;
using CJDSL.Domain.Interfaces;

namespace CJDSL.Infrastructure.Services;

/// <summary>
/// 基于模板和规则的 DSL 生成器
/// </summary>
public class TemplateDslGenerator : IDslGenerator
{
    public Task<DslPage> GenerateFormAsync(M1_Object metaObject, GenerateOptions options, CancellationToken ct = default)
    {
        var dsl = new DslPage
        {
            Id = $"form_{metaObject.Code}",
            Title = metaObject.Name,
            Description = metaObject.Description,
            Layout = "form",
            Components = BuildFormComponents(metaObject, options)
        };
        return Task.FromResult(dsl);
    }

    public Task<DslPage> GenerateListAsync(M1_Object metaObject, GenerateOptions options, CancellationToken ct = default)
    {
        var dsl = new DslPage
        {
            Id = $"list_{metaObject.Code}",
            Title = $"{metaObject.Name}列表",
            Description = $"{metaObject.Name}列表页面",
            Layout = "list",
            Components = BuildListComponents(metaObject, options)
        };
        return Task.FromResult(dsl);
    }

    public Task<DslPage> GenerateDetailAsync(M1_Object metaObject, GenerateOptions options, CancellationToken ct = default)
    {
        var dsl = new DslPage
        {
            Id = $"detail_{metaObject.Code}",
            Title = $"{metaObject.Name}详情",
            Description = $"{metaObject.Name}详情页面",
            Layout = "detail",
            Components = BuildDetailComponents(metaObject, options)
        };
        return Task.FromResult(dsl);
    }

    public Task<DslPage> GenerateFromNlpAsync(string description, UserContext user, GenerateOptions options, CancellationToken ct = default)
    {
        // 简化实现：根据关键词匹配元对象
        var dsl = new DslPage
        {
            Id = $"nlp_{Guid.NewGuid():N}",
            Title = "智能生成页面",
            Description = description,
            Layout = "form",
            Components = new List<DslComponent>
            {
                new()
                {
                    Type = "textDisplay",
                    Props = new Dictionary<string, object> { { "Typo", "h6" } },
                    Label = "NLP 生成结果（演示）"
                }
            }
        };
        return Task.FromResult(dsl);
    }

    public Task<DslPage> GenerateDashboardAsync(M4_Scene scene, GenerateOptions options, CancellationToken ct = default)
    {
        var dsl = new DslPage
        {
            Id = "dashboard",
            Title = "仪表盘",
            Layout = "dashboard",
            Components = new List<DslComponent>()
        };
        return Task.FromResult(dsl);
    }

    public Task<DslPage> AdaptAsync(DslPage baseDsl, UserContext user, DataContext data, CancellationToken ct = default)
    {
        // 根据用户角色调整可见性
        var adapted = CloneDslPage(baseDsl);
        foreach (var component in adapted.GetAllComponents())
        {
            if (component.VisibleIf != null && component.VisibleIf.Contains("user"))
            {
                // 简化：管理员角色可见所有
                if (!user.Roles.Contains("admin") && component.VisibleIf.Contains("admin"))
                {
                    component.VisibleIf = "false";
                }
            }
        }
        return Task.FromResult(adapted);
    }

    private List<DslComponent> BuildFormComponents(M1_Object metaObject, GenerateOptions options)
    {
        var columns = options.DeviceType == "Mobile" ? 1 : 2;
        var span = 12 / columns;

        var formFields = metaObject.Properties
            .Where(p => p.Enabled)
            .Select(prop => new DslComponent
            {
                Type = MapToComponentType(prop.Type),
                Label = prop.Name,
                FieldName = prop.Code,
                DataBind = $"@data.{prop.Code}",
                Span = span,
                Props = new Dictionary<string, object>
                {
                    { "Required", prop.Required },
                    { "Label", prop.Name }
                },
                ValidationRules = prop.Required ? new List<DslValidationRule>
                {
                    new() { Type = "required", Message = $"{prop.Name}必填" }
                } : null,
                DataSource = !string.IsNullOrEmpty(prop.DictCode)
                    ? new DslDataSource { Type = "dictionary", Code = prop.DictCode }
                    : null
            })
            .ToList();

        return new List<DslComponent>
        {
            new()
            {
                Type = "card",
                Props = new Dictionary<string, object> { { "Elevation", 2 } },
                Children = new List<DslComponent>
                {
                    new()
                    {
                        Type = "textDisplay",
                        Props = new Dictionary<string, object> { { "Typo", "h5" } },
                        Label = metaObject.Name
                    },
                    new()
                    {
                        Type = "form",
                        Id = $"form_{metaObject.Code}",
                        Children = new List<DslComponent>
                        {
                            new()
                            {
                                Type = "grid",
                                Children = formFields
                            },
                            new()
                            {
                                Type = "divider",
                                Props = new Dictionary<string, object> { { "Class", "my-4" } }
                            },
                            new()
                            {
                                Type = "stack",
                                Props = new Dictionary<string, object> { { "Row", true }, { "Justify", "flex-end" }, { "Spacing", 2 } },
                                Children = new List<DslComponent>
                                {
                                    new()
                                    {
                                        Type = "button", Label = "重置",
                                        Props = new Dictionary<string, object> { { "Variant", "Outlined" }, { "Color", "Secondary" } },
                                        Events = new List<DslEvent> { new() { Type = "onClick", Handler = DslHandlers.Reset, Params = new Dictionary<string, object> { { "formId", $"form_{metaObject.Code}" } } } }
                                    },
                                    new()
                                    {
                                        Type = "button", Label = "保存",
                                        Props = new Dictionary<string, object> { { "Variant", "Filled" }, { "Color", "Primary" } },
                                        Events = new List<DslEvent> { new() { Type = "onClick", Handler = DslHandlers.Submit, Params = new Dictionary<string, object> { { "formId", $"form_{metaObject.Code}" }, { "endpoint", $"/api/{metaObject.Code}/save" } } } }
                                    },
                                    new()
                                    {
                                        Type = "button", Label = "提交",
                                        Props = new Dictionary<string, object> { { "Variant", "Filled" }, { "Color", "Tertiary" } },
                                        Events = new List<DslEvent>
                                        {
                                            new()
                                            {
                                                Type = "onClick", Handler = DslHandlers.Chain,
                                                Confirm = new DslConfirm { Title = "确认提交", Message = "提交后将进入审批流程，是否继续？" },
                                                Params = new Dictionary<string, object>
                                                {
                                                    { "chain", new List<Dictionary<string, object>>
                                                        {
                                                            new() { { "handler", "validate" }, { "params", new Dictionary<string, object> { { "formId", $"form_{metaObject.Code}" } } } },
                                                            new() { { "handler", "apiCall" }, { "params", new Dictionary<string, object> { { "endpoint", $"/api/{metaObject.Code}/submit" }, { "method", "POST" }, { "formId", $"form_{metaObject.Code}" } } } },
                                                            new() { { "handler", "showToast" }, { "params", new Dictionary<string, object> { { "message", "提交成功" }, { "severity", "success" } } } },
                                                            new() { { "handler", "navigate" }, { "params", new Dictionary<string, object> { { "path", $"/{metaObject.Code}/list" } } } }
                                                        }
                                                    }
                                                }
                                            }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };
    }

    private List<DslComponent> BuildListComponents(M1_Object metaObject, GenerateOptions options)
    {
        var columns = metaObject.Properties
            .Where(p => p.Enabled)
            .Take(8)
            .Select(prop => new Dictionary<string, object>
            {
                { "title", prop.Name },
                { "dataIndex", prop.Code },
                { "key", prop.Code }
            })
            .ToList();

        return new List<DslComponent>
        {
            new()
            {
                Type = "card",
                Children = new List<DslComponent>
                {
                    new()
                    {
                        Type = "stack",
                        Props = new Dictionary<string, object> { { "Row", true }, { "Spacing", 2 } },
                        Children = new List<DslComponent>
                        {
                            new()
                            {
                                Type = "text",
                                Props = new Dictionary<string, object> { { "Placeholder", "搜索" }, { "AdornmentIcon", "Search" } },
                                DataBind = "@query.keyword"
                            },
                            new()
                            {
                                Type = "button", Label = "查询",
                                Props = new Dictionary<string, object> { { "Color", "Primary" }, { "StartIcon", "Search" } }
                            },
                            new()
                            {
                                Type = "button", Label = "新增",
                                Props = new Dictionary<string, object> { { "Variant", "Outlined" }, { "StartIcon", "Add" } }
                            }
                        }
                    },
                    new()
                    {
                        Type = "table",
                        Props = new Dictionary<string, object>
                        {
                            { "columns", columns },
                            { "rowKey", "id" },
                            { "pagination", true }
                        }
                    }
                }
            }
        };
    }

    private List<DslComponent> BuildDetailComponents(M1_Object metaObject, GenerateOptions options)
    {
        return BuildFormComponents(metaObject, options);
    }

    private static string MapToComponentType(string propertyType) => propertyType.ToLower() switch
    {
        "string" => "text",
        "number" => "number",
        "date" => "date",
        "datetime" => "datetime",
        "select" => "select",
        "textarea" => "textarea",
        "boolean" => "switch",
        _ => "text"
    };

    private static DslPage CloneDslPage(DslPage source)
    {
        // 简化克隆
        return new DslPage
        {
            Id = source.Id,
            Title = source.Title,
            Description = source.Description,
            Layout = source.Layout,
            Components = source.Components
        };
    }
}
