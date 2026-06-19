namespace CJDSL.Web.Services;

/// <summary>
/// 浮窗项
/// </summary>
public class FloatItem
{
    public string Id { get; set; } = Guid.NewGuid().ToString("N");
    public string Title { get; set; } = string.Empty;
    public string PageCode { get; set; } = string.Empty;
    public string Icon { get; set; } = "M4 4h16v16H4z"; // placeholder, overridden by caller
    public DateTime CreatedAt { get; set; } = DateTime.Now;
}

/// <summary>
/// 浮窗管理服务 — 管理被最小化的 DSL 预览弹窗
/// </summary>
public class FloatService
{
    private readonly List<FloatItem> _items = new();
    private readonly object _lock = new();

    public event Action? Changed;

    public IReadOnlyList<FloatItem> Items
    {
        get { lock (_lock) { return _items.ToList(); } }
    }

    public void Add(string title, string pageCode, string? icon = null)
    {
        lock (_lock)
        {
            if (_items.Any(x => x.PageCode == pageCode)) return;
            _items.Add(new FloatItem { Title = title, PageCode = pageCode, Icon = icon ?? "view_quilt" });
        }
        Changed?.Invoke();
    }

    public void Remove(string id)
    {
        lock (_lock)
        {
            _items.RemoveAll(x => x.Id == id);
        }
        Changed?.Invoke();
    }

    public void RemoveByPageCode(string pageCode)
    {
        lock (_lock)
        {
            _items.RemoveAll(x => x.PageCode == pageCode);
        }
        Changed?.Invoke();
    }
}
