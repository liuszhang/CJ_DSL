using CJCore.Framework.Abstractions;
using MudBlazor;

namespace CJDSL.Web.Services;

/// <summary>
/// CJDSL 菜单注册 — 将侧边栏导航项接入 CJCore 框架的 CoreMenu。
/// </summary>
public class CJDSLMenuService : IMenuService
{
    public ValueTask<IEnumerable<MenuItem>> GetMenuItemsAsync(CancellationToken cancellationToken = default)
    {
        var items = new List<MenuItem>
        {
            new MenuItem
            {
                Text = "首页",
                Href = "/",
                Icon = Icons.Material.Filled.Home,
                Match = "Exact",
                Order = 10,
                GroupName = "首页"
            },
            new MenuItem
            {
                Text = "DSL 测试",
                Href = "/dsl-test",
                Icon = Icons.Material.Filled.Science,
                Match = "Prefix",
                Order = 20,
                GroupName = "DSL 测试"
            },
            new MenuItem
            {
                Text = "组件映射",
                Href = "/component-mapping",
                Icon = Icons.Material.Filled.AccountTree,
                Match = "Prefix",
                Order = 30,
                GroupName = "组件映射"
            },
            new MenuItem
            {
                Text = "提示词配置",
                Href = "/config/prompt",
                Icon = Icons.Material.Filled.Code,
                Match = "Prefix",
                Order = 50,
                GroupName = "系统管理"
            },
            new MenuItem
            {
                Text = "本体源配置",
                Href = "/config/ontology",
                Icon = Icons.Material.Filled.AccountTree,
                Match = "Prefix",
                Order = 60,
                GroupName = "系统管理"
            },
        };

        return ValueTask.FromResult<IEnumerable<MenuItem>>(items);
    }
}
