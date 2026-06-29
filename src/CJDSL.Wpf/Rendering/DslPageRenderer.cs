using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using CJDSL.Domain;
using CJDSL.Domain.Entities.Dsl;

namespace CJDSL.Wpf.Rendering;

/// <summary>
/// WPF 页面级渲染控件 — 接收 DslPage 并递归渲染整个组件树
/// </summary>
public class DslPageRenderer : ContentControl
{
    private readonly DslRendererRegistry _registry;
    private DslRenderContext? _context;

    public DslPageRenderer()
    {
        _registry = new DslRendererRegistry();
        RegisterDefaultRenderers();
    }

    public DslPageRenderer(DslRendererRegistry registry)
    {
        _registry = registry;
        RegisterDefaultRenderers();
    }

    #region 依赖属性

    public static readonly DependencyProperty DslPageProperty =
        DependencyProperty.Register(nameof(DslPage), typeof(DslPage), typeof(DslPageRenderer),
            new PropertyMetadata(null, OnDslPageChanged));

    public DslPage? DslPage
    {
        get => (DslPage?)GetValue(DslPageProperty);
        set => SetValue(DslPageProperty, value);
    }

    public static readonly DependencyProperty TargetPlatformProperty =
        DependencyProperty.Register(nameof(TargetPlatform), typeof(TargetPlatform), typeof(DslPageRenderer),
            new PropertyMetadata(TargetPlatform.Wpf));

    public TargetPlatform TargetPlatform
    {
        get => (TargetPlatform)GetValue(TargetPlatformProperty);
        set => SetValue(TargetPlatformProperty, value);
    }

    public static readonly DependencyProperty DataStoreProperty =
        DependencyProperty.Register(nameof(DataStore), typeof(DslDataStore), typeof(DslPageRenderer),
            new PropertyMetadata(null));

    public DslDataStore? DataStore
    {
        get => (DslDataStore?)GetValue(DataStoreProperty);
        set => SetValue(DataStoreProperty, value);
    }

    #endregion

    private static void OnDslPageChanged(DependencyObject d, DependencyPropertyChangedEventArgs e)
    {
        if (d is DslPageRenderer renderer)
            renderer.RenderPage();
    }

    private void RenderPage()
    {
        if (DslPage is null)
        {
            Content = new TextBlock
            {
                Text = "无 DSL 页面",
                Foreground = Brushes.Gray,
                HorizontalAlignment = HorizontalAlignment.Center,
                Margin = new Thickness(20),
            };
            return;
        }

        _context = new DslRenderContext
        {
            Page = DslPage,
            TargetPlatform = TargetPlatform,
        };

        if (DataStore != null)
        {
            foreach (var kv in DataStore.Snapshot())
                _context.DataStore.Set(kv.Key, kv.Value);
        }

        DslComponentRenderer.Initialize(_registry);

        var root = new ScrollViewer
        {
            VerticalScrollBarVisibility = ScrollBarVisibility.Auto,
            HorizontalScrollBarVisibility = ScrollBarVisibility.Disabled,
        };

        var panel = new StackPanel { Margin = new Thickness(16) };

        foreach (var component in DslPage.Components)
        {
            var rendered = DslComponentRenderer.RenderComponent(component, _context);
            if (rendered != null)
                panel.Children.Add(rendered);
        }

        root.Content = panel;
        Content = root;
    }

    /// <summary>
    /// 注册自定义渲染器
    /// </summary>
    public void RegisterRenderer(IDslComponentRenderer renderer)
    {
        _registry.Register(renderer);
    }

    /// <summary>
    /// 获取数据存储（用于外部写入数据）
    /// </summary>
    public DslDataStore? GetDataStore() => _context?.DataStore;

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
