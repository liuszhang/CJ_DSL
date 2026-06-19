using CJDSL.Domain.Entities.Dsl;
using CJDSL.Domain.Interfaces;
using System.Text.Json;
using System.Text.RegularExpressions;

namespace CJDSL.Infrastructure.Services;

/// <summary>
/// DSL 内存仓储实现
/// </summary>
public class InMemoryDslRepository : IDslRepository
{
    private readonly Dictionary<string, DslPage> _pages = new();
    private readonly object _lock = new();

    public InMemoryDslRepository()
    {
        // 预加载示例 DSL
        LoadSampleDsls();
    }

    public Task<DslPage?> GetAsync(string pageCode, string version = "latest", CancellationToken ct = default)
    {
        lock (_lock)
        {
            _pages.TryGetValue(pageCode, out var page);
            return Task.FromResult(page);
        }
    }

    public Task<DslPage> SaveAsync(DslPage dsl, CancellationToken ct = default)
    {
        lock (_lock)
        {
            _pages[dsl.Id] = dsl;
        }
        return Task.FromResult(dsl);
    }

    public Task<bool> DeleteAsync(string pageCode, string version, CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_pages.Remove(pageCode));
        }
    }

    public Task<List<DslPage>> GetAllAsync(CancellationToken ct = default)
    {
        lock (_lock)
        {
            return Task.FromResult(_pages.Values.ToList());
        }
    }

    private void LoadSampleDsls()
    {
        var sampleDsl = new DslPage
        {
            Id = "page_equipment_repair",
            Title = "设备报修单",
            Description = "设备故障报修录入页面",
            Layout = "form",
            Components = new List<DslComponent>
            {
                new()
                {
                    Type = "card",
                    Props = new Dictionary<string, object> { { "Elevation", 2 }, { "Class", "pa-4" } },
                    Children = new List<DslComponent>
                    {
                        new() { Type = "textDisplay", Props = new Dictionary<string, object> { { "Typo", "h5" } }, Label = "设备报修单" },
                        new()
                        {
                            Type = "form",
                            Id = "repairForm",
                            Children = new List<DslComponent>
                            {
                                new()
                                {
                                    Type = "grid",
                                    Children = new List<DslComponent>
                                    {
                                        new()
                                        {
                                            Type = "text", Span = 6, Label = "报修单号", FieldName = "repairNo",
                                            Props = new Dictionary<string, object> { { "Required", true }, { "ReadOnly", true }, { "Variant", "Filled" } }
                                        },
                                        new()
                                        {
                                            Type = "text", Span = 6, Label = "设备名称", FieldName = "equipmentName",
                                            Props = new Dictionary<string, object> { { "Required", true } }
                                        },
                                        new()
                                        {
                                            Type = "select", Span = 6, Label = "设备类型", FieldName = "equipmentType",
                                            DataSource = new DslDataSource { Type = "dictionary", Code = "equipment_type" },
                                            Props = new Dictionary<string, object> { { "Required", true } }
                                        },
                                        new()
                                        {
                                            Type = "date", Span = 6, Label = "报修日期", FieldName = "repairDate",
                                            Props = new Dictionary<string, object> { { "Required", true } }
                                        },
                                        new()
                                        {
                                            Type = "select", Span = 6, Label = "优先级", FieldName = "priority",
                                            DataSource = new DslDataSource { Type = "dictionary", Code = "priority" },
                                            Props = new Dictionary<string, object> { { "Required", true } }
                                        },
                                        new()
                                        {
                                            Type = "textarea", Span = 12, Label = "故障描述", FieldName = "faultDescription",
                                            Props = new Dictionary<string, object> { { "Required", true }, { "Lines", 4 }, { "MaxLength", 500 } }
                                        }
                                    }
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
                                            Props = new Dictionary<string, object> { { "Variant", "Outlined" }, { "Color", "Secondary" } }
                                        },
                                        new()
                                        {
                                            Type = "button", Label = "保存",
                                            Props = new Dictionary<string, object> { { "Variant", "Filled" }, { "Color", "Primary" } }
                                        },
                                        new()
                                        {
                                            Type = "button", Label = "提交",
                                            Props = new Dictionary<string, object> { { "Variant", "Filled" }, { "Color", "Tertiary" } }
                                        }
                                    }
                                }
                            }
                        }
                    }
                }
            }
        };

        _pages["page_equipment_repair"] = sampleDsl;
        _pages["repair-form"] = sampleDsl;
    }
}
