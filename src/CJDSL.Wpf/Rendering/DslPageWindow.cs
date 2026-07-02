using System.Windows;
using System.Windows.Media;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering;

/// <summary>
/// 基于 DslPage 的 WPF 弹窗 — 类似 ABWork 的 DynamicDialogService，但使用 CJDSL 完整模型
/// </summary>
public class DslPageWindow
{
    private readonly DslRendererRegistry _registry;

    public DslPageWindow()
    {
        _registry = new DslRendererRegistry();
        RegisterDefaultRenderers();
    }

    public DslPageWindow(DslRendererRegistry registry)
    {
        _registry = registry;
    }

    /// <summary>
    /// 从 DslPage 构建并显示模态窗口
    /// </summary>
    public DslPageWindowResult ShowDialog(DslPage page, Window? owner = null)
    {
        var renderer = new DslPageRenderer(_registry)
        {
            DslPage = page,
            TargetPlatform = page.TargetPlatform,
        };

        var window = new Window
        {
            Title = page.Title,
            Width = 600,
            SizeToContent = SizeToContent.Height,
            MinHeight = 300,
            MaxHeight = SystemParameters.PrimaryScreenHeight * 0.8,
            WindowStartupLocation = owner != null
                ? WindowStartupLocation.CenterOwner
                : WindowStartupLocation.CenterScreen,
            Content = renderer,
            Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
        };

        if (owner != null)
            window.Owner = owner;

        var dialogResult = window.ShowDialog() == true;
        var dataStore = renderer.GetDataStore();

        return new DslPageWindowResult
        {
            Confirmed = dialogResult,
            DataStore = dataStore,
        };
    }

    /// <summary>
    /// 从 DslPage 构建 Window（非模态，由调用方控制生命周期）
    /// </summary>
    public Window CreateWindow(DslPage page, Window? owner = null)
    {
        var renderer = new DslPageRenderer(_registry)
        {
            DslPage = page,
            TargetPlatform = page.TargetPlatform,
        };

        var window = new Window
        {
            Title = page.Title,
            Width = 600,
            SizeToContent = SizeToContent.Height,
            Content = renderer,
            Background = new SolidColorBrush(Color.FromRgb(245, 245, 245)),
        };

        if (owner != null)
            window.Owner = owner;

        return window;
    }

    /// <summary>
    /// 将 DslPage 渲染为 FrameworkElement（嵌入到现有界面中）
    /// </summary>
    public FrameworkElement Render(DslPage page)
    {
        return new DslPageRenderer(_registry)
        {
            DslPage = page,
            TargetPlatform = page.TargetPlatform,
        };
    }

    private void RegisterDefaultRenderers()
    {
        _registry.Register(new Renderers.TextRenderer());
        _registry.Register(new Renderers.TextDisplayRenderer());
        _registry.Register(new Renderers.NumberRenderer());
        _registry.Register(new Renderers.DateRenderer());
        _registry.Register(new Renderers.TimeRenderer());
        _registry.Register(new Renderers.SelectRenderer());
        _registry.Register(new Renderers.TextAreaRenderer());
        _registry.Register(new Renderers.ButtonRenderer());
        _registry.Register(new Renderers.CardRenderer());
        _registry.Register(new Renderers.GridRenderer());
        _registry.Register(new Renderers.StackRenderer());
        _registry.Register(new Renderers.DividerRenderer());
        _registry.Register(new Renderers.SwitchRenderer());
        _registry.Register(new Renderers.CheckboxRenderer());
        _registry.Register(new Renderers.TableRenderer());
        _registry.Register(new Renderers.PaperRenderer());
        _registry.Register(new Renderers.ChipRenderer());
        _registry.Register(new Renderers.AvatarRenderer());
        _registry.Register(new Renderers.TabsRenderer());
        _registry.Register(new Renderers.ExpansionRenderer());
        _registry.Register(new Renderers.ProgressRenderer());
    }
}

public class DslPageWindowResult
{
    public bool Confirmed { get; set; }
    public DslDataStore? DataStore { get; set; }
}
