using System.Text.Json.Serialization;

namespace CJDSL.Domain.Entities.Dsl;

/// <summary>
/// CJDSL 通用组件节点
/// </summary>
public class DslComponent
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");

    /// <summary>
    /// 组件类型：与 MudBlazor 组件映射
    /// </summary>
    public string Type { get; set; } = "text";

    /// <summary>
    /// 组件属性（透传给 MudBlazor）
    /// </summary>
    public Dictionary<string, object>? Props { get; set; }

    /// <summary>
    /// 子组件（递归树）
    /// </summary>
    public List<DslComponent>? Children { get; set; }

    /// <summary>
    /// 数据绑定路径，如 "data.user.name" 或 "@datasource.items"
    /// </summary>
    public string? DataBind { get; set; }

    /// <summary>
    /// 标签文本（用于表单字段）
    /// </summary>
    public string? Label { get; set; }

    /// <summary>
    /// 表单字段名
    /// </summary>
    public string? FieldName { get; set; }

    /// <summary>
    /// Grid 布局占用列数 (1-12)
    /// </summary>
    public int? Span { get; set; } = 12;

    /// <summary>
    /// 条件渲染表达式：如 "user.role == 'admin'"
    /// </summary>
    public string? VisibleIf { get; set; }

    /// <summary>
    /// 禁用条件表达式
    /// </summary>
    public string? DisabledIf { get; set; }

    /// <summary>
    /// 事件处理器列表
    /// </summary>
    public List<DslEvent>? Events { get; set; }

    /// <summary>
    /// 数据源覆盖（组件级）
    /// </summary>
    public DslDataSource? DataSource { get; set; }

    /// <summary>
    /// 验证规则（表单字段）
    /// </summary>
    public List<DslValidationRule>? ValidationRules { get; set; }

    /// <summary>
    /// 组件样式
    /// </summary>
    public DslStyle? Style { get; set; }

    /// <summary>
    /// Tooltip / 帮助文本
    /// </summary>
    public string? HelpText { get; set; }

    /// <summary>
    /// 递归获取所有后代组件
    /// </summary>
    public IEnumerable<DslComponent> Descendants()
    {
        yield return this;
        if (Children == null) yield break;
        foreach (var child in Children.SelectMany(c => c.Descendants()))
            yield return child;
    }

    /// <summary>
    /// 查找指定字段名的组件
    /// </summary>
    public DslComponent? FindByFieldName(string fieldName)
    {
        return Descendants().FirstOrDefault(c => c.FieldName == fieldName);
    }
}

/// <summary>
/// 预定义的 Handler 类型常量
/// </summary>
public static class DslHandlers
{
    public const string Submit = "submit";
    public const string Navigate = "navigate";
    public const string ApiCall = "apiCall";
    public const string OpenModal = "openModal";
    public const string CloseModal = "closeModal";
    public const string Refresh = "refresh";
    public const string SetValue = "setValue";
    public const string ShowToast = "showToast";
    public const string Export = "export";
    public const string Validate = "validate";
    public const string Reset = "reset";
    public const string Chain = "chain";
}
