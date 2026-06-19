namespace CJDSL.Domain.Entities.Dsl;

/// <summary>
/// DSL 权限控制
/// </summary>
public class DslPermission
{
    /// <summary>
    /// 所需角色列表
    /// </summary>
    public List<string>? RequiredRoles { get; set; }

    /// <summary>
    /// 所需权限编码列表
    /// </summary>
    public List<string>? RequiredPermissions { get; set; }
}
