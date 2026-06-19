using System.Text.Json.Serialization;

namespace CJDSL.Domain.Entities.Dsl;

/// <summary>
/// CJDSL 页面根节点
/// </summary>
public class DslPage
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string Description { get; set; } = string.Empty;

    /// <summary>
    /// 页面布局模式：form, list, detail, dashboard, custom
    /// </summary>
    public string Layout { get; set; } = "form";

    public DslDataSource? DataSource { get; set; }
    public DslPermission? Permission { get; set; }
    public DslResponsive? Responsive { get; set; }
    public List<DslComponent> Components { get; set; } = new();
    public List<DslPageEvent>? PageEvents { get; set; }
    public DslStyle? Style { get; set; }

    /// <summary>
    /// 获取组件树中所有组件（扁平化）
    /// </summary>
    public IEnumerable<DslComponent> GetAllComponents()
    {
        return Components.SelectMany(c => c.Descendants());
    }
}

public class DslPageEvent
{
    public string Type { get; set; } = string.Empty;
    public string Handler { get; set; } = string.Empty;
    public Dictionary<string, object>? Params { get; set; }
}
