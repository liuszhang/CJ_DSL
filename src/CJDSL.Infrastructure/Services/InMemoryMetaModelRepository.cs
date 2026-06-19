using CJDSL.Domain.Entities.MetaModel;
using CJDSL.Domain.Interfaces;

namespace CJDSL.Infrastructure.Services;

/// <summary>
/// 元模型内存仓储实现
/// </summary>
public class InMemoryMetaModelRepository : IMetaModelRepository
{
    private readonly Dictionary<string, M1_Object> _objects = new();
    private readonly Dictionary<string, M0_Enum> _enums = new();
    private readonly Dictionary<string, M0_DataDictionary> _dictionaries = new();

    public InMemoryMetaModelRepository()
    {
        LoadSampleData();
    }

    public Task<M1_Object?> GetObjectAsync(string code, CancellationToken ct = default)
    {
        _objects.TryGetValue(code, out var obj);
        return Task.FromResult(obj);
    }

    public Task<M1_Object?> GetObjectByIdAsync(string id, CancellationToken ct = default)
    {
        var obj = _objects.Values.FirstOrDefault(o => o.Id == id);
        return Task.FromResult(obj);
    }

    public Task<List<M1_Object>> GetAllObjectsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_objects.Values.ToList());
    }

    public Task<M1_Object> AddObjectAsync(M1_Object obj, CancellationToken ct = default)
    {
        _objects[obj.Code] = obj;
        return Task.FromResult(obj);
    }

    public Task<M1_Object> UpdateObjectAsync(M1_Object obj, CancellationToken ct = default)
    {
        _objects[obj.Code] = obj;
        return Task.FromResult(obj);
    }

    public Task<bool> DeleteObjectAsync(string id, CancellationToken ct = default)
    {
        var obj = _objects.Values.FirstOrDefault(o => o.Id == id);
        if (obj != null) return Task.FromResult(_objects.Remove(obj.Code));
        return Task.FromResult(false);
    }

    public Task<M0_Enum?> GetEnumAsync(string code, CancellationToken ct = default)
    {
        _enums.TryGetValue(code, out var en);
        return Task.FromResult(en);
    }

    public Task<List<M0_Enum>> GetAllEnumsAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_enums.Values.ToList());
    }

    public Task<M0_Enum> AddEnumAsync(M0_Enum enumDef, CancellationToken ct = default)
    {
        _enums[enumDef.Code] = enumDef;
        return Task.FromResult(enumDef);
    }

    public Task<M0_DataDictionary?> GetDictionaryAsync(string code, CancellationToken ct = default)
    {
        _dictionaries.TryGetValue(code, out var dict);
        return Task.FromResult(dict);
    }

    public Task<List<M0_DataDictionary>> GetAllDictionariesAsync(CancellationToken ct = default)
    {
        return Task.FromResult(_dictionaries.Values.ToList());
    }

    private void LoadSampleData()
    {
        // 设备报修单
        var equipmentRepair = new M1_Object
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "设备报修单",
            Code = "equipment_repair",
            Description = "设备报修单业务对象",
            Properties = new List<M1_Property>
            {
                new() { Name = "报修单号", Code = "repair_no", Type = "string", Required = true, Length = 50, Description = "唯一标识报修单" },
                new() { Name = "设备名称", Code = "equipment_name", Type = "string", Required = true, Length = 100, Description = "报修设备名称" },
                new() { Name = "设备类型", Code = "equipment_type", Type = "select", Required = true, DictCode = "equipment_type", Description = "设备类型" },
                new() { Name = "报修人", Code = "reporter", Type = "string", Required = true, Length = 50, Description = "报修人姓名" },
                new() { Name = "报修日期", Code = "repair_date", Type = "date", Required = true, Description = "报修日期" },
                new() { Name = "故障描述", Code = "fault_description", Type = "textarea", Required = true, Length = 500, Description = "详细故障描述" },
                new() { Name = "状态", Code = "status", Type = "select", Required = true, DictCode = "repair_status", Description = "报修状态" },
                new() { Name = "优先级", Code = "priority", Type = "select", Required = true, DictCode = "priority", DefaultValue = "medium", Description = "优先级" }
            },
            LifeCycleStates = new List<M1_LifeCycleState>
            {
                new() { Code = "draft", Name = "草稿", IsStart = true, NextStates = new List<string> { "pending" } },
                new() { Code = "pending", Name = "待处理", NextStates = new List<string> { "processing", "cancelled" } },
                new() { Code = "processing", Name = "处理中", NextStates = new List<string> { "completed", "cancelled" } },
                new() { Code = "completed", Name = "已完成", IsEnd = true },
                new() { Code = "cancelled", Name = "已取消", IsEnd = true }
            }
        };

        // 设备
        var equipment = new M1_Object
        {
            Id = Guid.NewGuid().ToString("N"),
            Name = "设备",
            Code = "equipment",
            Description = "设备业务对象",
            Properties = new List<M1_Property>
            {
                new() { Name = "设备编号", Code = "equipment_no", Type = "string", Required = true, Length = 50, Description = "设备唯一编号" },
                new() { Name = "设备名称", Code = "equipment_name", Type = "string", Required = true, Length = 100, Description = "设备名称" },
                new() { Name = "设备类型", Code = "equipment_type", Type = "select", Required = true, DictCode = "equipment_type", Description = "设备类型" },
                new() { Name = "所在位置", Code = "location", Type = "string", Required = true, Length = 200, Description = "所在位置" },
                new() { Name = "购买日期", Code = "purchase_date", Type = "date", Required = true, Description = "购买日期" },
                new() { Name = "状态", Code = "status", Type = "select", Required = true, DictCode = "equipment_status", Description = "设备状态" }
            }
        };

        _objects["equipment_repair"] = equipmentRepair;
        _objects["equipment"] = equipment;

        // 枚举
        _enums["priority"] = new M0_Enum
        {
            Code = "priority",
            Name = "优先级",
            Description = "工单优先级",
            Items = new List<M0_EnumItem>
            {
                new() { Code = "low", Name = "低", Value = "low", Sort = 1 },
                new() { Code = "medium", Name = "中", Value = "medium", Sort = 2 },
                new() { Code = "high", Name = "高", Value = "high", Sort = 3 },
                new() { Code = "urgent", Name = "紧急", Value = "urgent", Sort = 4 }
            }
        };

        _enums["repair_status"] = new M0_Enum
        {
            Code = "repair_status",
            Name = "报修状态",
            Description = "设备报修单状态",
            Items = new List<M0_EnumItem>
            {
                new() { Code = "draft", Name = "草稿", Value = "draft", Sort = 1 },
                new() { Code = "pending", Name = "待处理", Value = "pending", Sort = 2 },
                new() { Code = "processing", Name = "处理中", Value = "processing", Sort = 3 },
                new() { Code = "completed", Name = "已完成", Value = "completed", Sort = 4 },
                new() { Code = "cancelled", Name = "已取消", Value = "cancelled", Sort = 5 }
            }
        };

        _enums["equipment_type"] = new M0_Enum
        {
            Code = "equipment_type",
            Name = "设备类型",
            Description = "设备分类",
            Items = new List<M0_EnumItem>
            {
                new() { Code = "server", Name = "服务器", Value = "server", Sort = 1 },
                new() { Code = "storage", Name = "存储设备", Value = "storage", Sort = 2 },
                new() { Code = "network", Name = "网络设备", Value = "network", Sort = 3 },
                new() { Code = "pc", Name = "个人电脑", Value = "pc", Sort = 4 },
                new() { Code = "other", Name = "其他", Value = "other", Sort = 99 }
            }
        };

        _enums["equipment_status"] = new M0_Enum
        {
            Code = "equipment_status",
            Name = "设备状态",
            Description = "设备使用状态",
            Items = new List<M0_EnumItem>
            {
                new() { Code = "available", Name = "可用", Value = "available", Sort = 1 },
                new() { Code = "in_use", Name = "使用中", Value = "in_use", Sort = 2 },
                new() { Code = "fault", Name = "故障", Value = "fault", Sort = 3 },
                new() { Code = "maintenance", Name = "维护中", Value = "maintenance", Sort = 4 }
            }
        };
    }
}
