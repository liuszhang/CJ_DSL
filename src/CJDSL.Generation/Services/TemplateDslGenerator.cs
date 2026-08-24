using CJDSL.Domain;
using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Entities.MetaModel;
using CJDSL.Domain.Interfaces;

namespace CJDSL.Generation.Services;

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
            TargetPlatform = options.TargetPlatform,
            RendererHint = GenerateOptions.ResolveRendererHint(options.TargetPlatform),
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
            TargetPlatform = options.TargetPlatform,
            RendererHint = GenerateOptions.ResolveRendererHint(options.TargetPlatform),
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
            TargetPlatform = options.TargetPlatform,
            RendererHint = GenerateOptions.ResolveRendererHint(options.TargetPlatform),
            Components = BuildDetailComponents(metaObject, options)
        };
        return Task.FromResult(dsl);
    }

    public Task<DslPage> GenerateFromNlpAsync(string description, UserContext user, GenerateOptions options, CancellationToken ct = default)
    {
        var fields = ExtractFieldsFromIntent(description);
        var title = ExtractTitleFromIntent(description);
        var layout = options.Layout ?? "form";

        var dsl = new DslPage
        {
            Id = $"nlp_{Guid.NewGuid():N}",
            Title = title,
            Description = description,
            Layout = layout,
            TargetPlatform = options.TargetPlatform,
            RendererHint = GenerateOptions.ResolveRendererHint(options.TargetPlatform),
            Components = layout == "list"
                ? BuildNlpListComponents(title, fields)
                : BuildNlpFormComponents(title, fields)
        };
        return Task.FromResult(dsl);
    }

    private static string ExtractTitleFromIntent(string description)
    {
        // "用户需要提交设备报修单，请生成..." → "设备报修单"
        var match = System.Text.RegularExpressions.Regex.Match(description, @"用户需要(.+?)[，,]");
        return match.Success ? match.Groups[1].Value.Trim() : "智能生成页面";
    }

    private static List<string> ExtractFieldsFromIntent(string description)
    {
        // "包含设备名称、设备类型、报修日期、故障描述等字段" → ["设备名称", "设备类型", ...]
        var match = System.Text.RegularExpressions.Regex.Match(description, @"包含(.+?)(?:等字段|等)");
        if (!match.Success) return new List<string>();

        return match.Groups[1].Value
            .Split(new[] { '、', '，', ',' }, StringSplitOptions.RemoveEmptyEntries)
            .Select(f => f.Trim())
            .ToList();
    }

    private static string InferComponentType(string fieldName)
    {
        var lower = fieldName.ToLower();
        if (lower.Contains("日期") || lower.Contains("时间")) return "date";
        if (lower.Contains("类型") || lower.Contains("类别") || lower.Contains("状态") || lower.Contains("方式")) return "select";
        if (lower.Contains("描述") || lower.Contains("说明") || lower.Contains("备注") || lower.Contains("意见")) return "textarea";
        if (lower.Contains("金额") || lower.Contains("数量") || lower.Contains("总数") || lower.Contains("数")) return "number";
        if (lower.Contains("开关") || lower.Contains("是否") || lower.Contains("确认")) return "switch";
        return "text";
    }

    private static List<DslComponent> BuildNlpFormComponents(string title, List<string> fields)
    {
        var formFields = fields.Select(f => new DslComponent
        {
            Type = InferComponentType(f),
            Label = f,
            FieldName = f,
            Span = 6,
            Props = new Dictionary<string, object> { { "Placeholder", $"请输入{f}" } },
        }).ToList();

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
                        Label = title
                    },
                    new() { Type = "divider" },
                    new()
                    {
                        Type = "grid",
                        Children = formFields
                    },
                    new() { Type = "divider" },
                    new()
                    {
                        Type = "stack",
                        Props = new Dictionary<string, object> { { "Row", true } },
                        Children = new List<DslComponent>
                        {
                            new()
                            {
                                Type = "button", Label = "取消",
                                Props = new Dictionary<string, object> { { "Variant", "Outlined" }, { "Color", "Secondary" } }
                            },
                            new()
                            {
                                Type = "button", Label = "确认",
                                Props = new Dictionary<string, object> { { "Variant", "Filled" }, { "Color", "Primary" } }
                            }
                        }
                    }
                }
            }
        };
    }

    private static List<DslComponent> BuildNlpListComponents(string title, List<string> fields)
    {
        var columns = fields.Select(f => new Dictionary<string, object>
        {
            { "title", f },
            { "dataIndex", f },
        }).ToList();

        return new List<DslComponent>
        {
            new()
            {
                Type = "card",
                Children = new List<DslComponent>
                {
                    new()
                    {
                        Type = "textDisplay",
                        Props = new Dictionary<string, object> { { "Typo", "h5" } },
                        Label = title
                    },
                    new()
                    {
                        Type = "stack",
                        Props = new Dictionary<string, object> { { "Row", true } },
                        Children = new List<DslComponent>
                        {
                            new()
                            {
                                Type = "text",
                                Props = new Dictionary<string, object> { { "Placeholder", "搜索..." } },
                                DataBind = "@query.keyword"
                            },
                            new()
                            {
                                Type = "button", Label = "查询",
                                Props = new Dictionary<string, object> { { "Color", "Primary" } }
                            }
                        }
                    },
                    new()
                    {
                        Type = "table",
                        Props = new Dictionary<string, object> { { "columns", columns } }
                    }
                }
            }
        };
    }

    public Task<DslPage> GenerateDashboardAsync(M4_Scene scene, GenerateOptions options, CancellationToken ct = default)
    {
        var title = scene?.Name ?? "数据仪表盘";

        // 统计卡片（模板生成器不依赖运行时数据，展示结构占位；具体数值由前端绑定数据源填充）
        var statCards = new List<DslComponent>
        {
            BuildStatCard("业务对象", "—", "Primary"),
            BuildStatCard("枚举项", "—", "Secondary"),
            BuildStatCard("字典项", "—", "Tertiary"),
            BuildStatCard("今日待办", "—", "Info")
        };

        var dsl = new DslPage
        {
            Id = $"dashboard_{scene?.Code ?? "default"}",
            Title = title,
            Description = scene?.Description ?? "基于元模型统计的仪表盘",
            Layout = "dashboard",
            RendererHint = GenerateOptions.ResolveRendererHint(options.TargetPlatform),
            Components = new List<DslComponent>
            {
                new()
                {
                    Type = "grid",
                    Props = new Dictionary<string, object> { { "Spacing", 3 } },
                    Children = statCards
                },
                new()
                {
                    Type = "card",
                    Props = new Dictionary<string, object> { { "Elevation", 2 }, { "Class", "mt-4" } },
                    Children = new List<DslComponent>
                    {
                        new() { Type = "textDisplay", Props = new Dictionary<string, object> { { "Typo", "h6" } }, Label = "趋势图" },
                        new()
                        {
                            Type = "chart",
                            Props = new Dictionary<string, object> { { "ChartType", "line" }, { "Title", "近 7 日趋势" }, { "Height", "280" } }
                        }
                    }
                },
                new()
                {
                    Type = "card",
                    Props = new Dictionary<string, object> { { "Elevation", 2 }, { "Class", "mt-4" } },
                    Children = new List<DslComponent>
                    {
                        new() { Type = "textDisplay", Props = new Dictionary<string, object> { { "Typo", "h6" } }, Label = "最近记录" },
                        new()
                        {
                            Type = "list",
                            Children = new List<DslComponent>
                            {
                                new() { Type = "listItem", Label = "暂无数据" }
                            }
                        }
                    }
                }
            }
        };
        return Task.FromResult(dsl);
    }

    private static DslComponent BuildStatCard(string label, string value, string color) => new()
    {
        Type = "card",
        Props = new Dictionary<string, object> { { "Elevation", 2 }, { "Class", "pa-3" } },
        Children = new List<DslComponent>
        {
            new() { Type = "textDisplay", Props = new Dictionary<string, object> { { "Typo", "body2" }, { "Color", color } }, Label = label },
            new() { Type = "textDisplay", Props = new Dictionary<string, object> { { "Typo", "h3" } }, Label = value }
        }
    };

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
            TargetPlatform = source.TargetPlatform,
            Components = source.Components
        };
    }
}
