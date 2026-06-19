namespace CJDSL.Domain.Entities.MetaModel;

/// <summary>
/// M1: 对象属性
/// </summary>
public class M1_Property
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string Type { get; set; } = "string"; // string, number, date, select, textarea, boolean
    public bool IsList { get; set; } = false;
    public bool Required { get; set; } = false;
    public int? Length { get; set; }
    public bool Nullable { get; set; } = true;
    public string? DefaultValue { get; set; }
    public string? DictCode { get; set; }
    public string? ControlType { get; set; }
    public int? MinLength { get; set; }
    public int? MaxLength { get; set; }
    public int? Min { get; set; }
    public int? Max { get; set; }
    public string? Pattern { get; set; }
    public List<M0_StatePermission>? StatePermissions { get; set; }
    public string Description { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// M1: 生命周期状态
/// </summary>
public class M1_LifeCycleState
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Code { get; set; } = string.Empty;
    public string Name { get; set; } = string.Empty;
    public bool IsStart { get; set; } = false;
    public bool IsEnd { get; set; } = false;
    public List<string> NextStates { get; set; } = new();
}

/// <summary>
/// M1: 引用约束
/// </summary>
public class M1_ReferentialConstraint
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Type { get; set; } = "check"; // foreignKey, unique, check
    public string Name { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? Expression { get; set; }
    public string? ReferencedEntity { get; set; }
}

/// <summary>
/// M1: 对象模型
/// </summary>
public class M1_Object
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "object"; // object, event
    public string? SuperClass { get; set; }
    public string? InheritMode { get; set; } = "inherit";
    public List<string> DisjointWith { get; set; } = new();
    public List<M1_Property> Properties { get; set; } = new();
    public List<M1_LifeCycleState> LifeCycleStates { get; set; } = new();
    public List<M1_ReferentialConstraint> Constraints { get; set; } = new();
    public bool Enabled { get; set; } = true;
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;
    public DateTime? UpdatedAt { get; set; }
}

/// <summary>
/// 状态权限
/// </summary>
public class M0_StatePermission
{
    public string Action { get; set; } = string.Empty; // create, view, edit, delete, approve, archive
    public bool Visible { get; set; } = true;
    public bool Editable { get; set; } = true;
}
