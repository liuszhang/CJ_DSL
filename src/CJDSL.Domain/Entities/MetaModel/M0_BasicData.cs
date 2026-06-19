namespace CJDSL.Domain.Entities.MetaModel;

/// <summary>
/// M0: 枚举项
/// </summary>
public class M0_EnumItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Value { get; set; } = string.Empty;
    public int Sort { get; set; } = 0;
    public bool Enabled { get; set; } = true;
    public string? Description { get; set; }
}

/// <summary>
/// M0: 枚举定义
/// </summary>
public class M0_Enum
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<M0_EnumItem> Items { get; set; } = new();
    public bool Builtin { get; set; } = false;
}

/// <summary>
/// M0: 数据字典项
/// </summary>
public class M0_DataDictionaryItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string? ParentCode { get; set; }
    public bool Enabled { get; set; } = true;
    public int? Sort { get; set; }
    public string? Description { get; set; }
    public Dictionary<string, object>? Extra { get; set; }
}

/// <summary>
/// M0: 数据字典
/// </summary>
public class M0_DataDictionary
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public string Category { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public List<M0_DataDictionaryItem> Items { get; set; } = new();
    public bool Builtin { get; set; } = false;
}
