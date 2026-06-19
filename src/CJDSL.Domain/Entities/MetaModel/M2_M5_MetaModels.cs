namespace CJDSL.Domain.Entities.MetaModel;

/// <summary>
/// M2: 行为模型 - 业务动作
/// </summary>
public class M2_Action
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string ModelId { get; set; } = string.Empty; // 所属 M1 对象 ID
    public string TargetEntity { get; set; } = string.Empty;
    public string PreConditions { get; set; } = string.Empty;
    public string PostStateChange { get; set; } = string.Empty;
    public List<string> RequiredRules { get; set; } = new();
    public List<string> DomainEvents { get; set; } = new();
    public List<string> RequiredPermissions { get; set; } = new();
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// M3: 规则模型
/// </summary>
public class M3_Rule
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = "validation"; // validation, calculation, derivation, riskControl
    public string Description { get; set; } = string.Empty;
    public string Expression { get; set; } = string.Empty;
    public string Version { get; set; } = "1.0.0";
    public List<RuleInputParam> InputParams { get; set; } = new();
    public string OutputType { get; set; } = "boolean";
    public bool Enabled { get; set; } = true;
}

public class RuleInputParam
{
    public string Name { get; set; } = string.Empty;
    public string Type { get; set; } = string.Empty;
}

/// <summary>
/// M4: 场景模型
/// </summary>
public class M4_Scene
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string Type { get; set; } = "useCase"; // useCase, businessFlow, scenarioTimeline
    public List<string> Steps { get; set; } = new();
    public List<string> Participants { get; set; } = new();
    public List<string> DomainEvents { get; set; } = new();
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// M5: 参与者模型
/// </summary>
public class M5_Participant
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Type { get; set; } = "human"; // human, systemAccount, externalSystem
    public List<string> Roles { get; set; } = new();
    public string Description { get; set; } = string.Empty;
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// M5: 角色
/// </summary>
public class M5_Role
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;
    public string? InheritsFrom { get; set; }
    public List<string> Permissions { get; set; } = new();
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// M5: 权限
/// </summary>
public class M5_Permission
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string Code { get; set; } = string.Empty;
    public string Category { get; set; } = "operation"; // operation, data
    public string Description { get; set; } = string.Empty;
    public string? TargetAction { get; set; }
    public string? Condition { get; set; } // ABAC 条件
    public bool Enabled { get; set; } = true;
}

/// <summary>
/// M1.5: 关系模型
/// </summary>
public class M1_5_Relation
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Name { get; set; } = string.Empty;
    public string FromClass { get; set; } = string.Empty; // 源 M1 对象 Code
    public string ToClass { get; set; } = string.Empty;   // 目标 M1 对象 Code
    public string Type { get; set; } = "association"; // association, composition, aggregation, generalization
    public int? CardinalityMin { get; set; } = 0;
    public int? CardinalityMax { get; set; } = -1; // -1 means *
    public bool Transitive { get; set; } = false;
    public bool Symmetric { get; set; } = false;
    public string? Description { get; set; }
    public bool Enabled { get; set; } = true;
}
