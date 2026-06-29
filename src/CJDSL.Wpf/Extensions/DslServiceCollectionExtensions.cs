using CJDSL.Wpf.Rendering;
using CJDSL.Wpf.Rendering.Renderers;

namespace CJDSL.Wpf.Extensions;

/// <summary>
/// CJDSL WPF 静态工厂 — 无需 DI 容器即可创建渲染器
/// </summary>
public static class DslServiceFactory
{
    /// <summary>
    /// 创建带默认渲染器的 DslPageWindow
    /// </summary>
    public static DslPageWindow CreatePageWindow() => new();

    /// <summary>
    /// 创建带默认渲染器的 DslPageRenderer 控件
    /// </summary>
    public static DslPageRenderer CreatePageRenderer() => new();

    /// <summary>
    /// 创建带默认渲染器的注册表
    /// </summary>
    public static DslRendererRegistry CreateRegistry()
    {
        var registry = new DslRendererRegistry();
        registry.Register(new TextRenderer());
        registry.Register(new TextDisplayRenderer());
        registry.Register(new NumberRenderer());
        registry.Register(new DateRenderer());
        registry.Register(new TimeRenderer());
        registry.Register(new SelectRenderer());
        registry.Register(new TextAreaRenderer());
        registry.Register(new ButtonRenderer());
        registry.Register(new CardRenderer());
        registry.Register(new GridRenderer());
        registry.Register(new StackRenderer());
        registry.Register(new DividerRenderer());
        registry.Register(new SwitchRenderer());
        registry.Register(new CheckboxRenderer());
        registry.Register(new TableRenderer());
        registry.Register(new PaperRenderer());
        registry.Register(new ChipRenderer());
        registry.Register(new AvatarRenderer());
        registry.Register(new TabsRenderer());
        registry.Register(new ExpansionRenderer());
        registry.Register(new ProgressRenderer());
        return registry;
    }
}
