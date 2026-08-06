using CJCore.Framework.Abstractions;

namespace CJDSL.Web.Services;

/// <summary>
/// CJDSL 模块 — 实现 IModule 以接入 CJCore 框架的程序集发现（CJDSL.Web 程序集含 6 个 @page 页面，
/// 收编 FrameworkApp 后由本模块保证宿主程序集进入路由发现）。
/// 同时贡献产品特有静态资源标签（Google Fonts / app.css / 静态 title / richtext.js / 内联导出 JS，
/// 与原 App.razor 行为等价）。
/// </summary>
public class CJDSLModule : ModuleBase
{
    private readonly IAppHeadService _head;

    public CJDSLModule(IAppHeadService head) => _head = head;

    public override string Name => "CJDSL";

    public override ValueTask InitializeAsync(CancellationToken cancellationToken = default)
    {
        // head：Google Fonts + 产品样式 + 静态 title（保留原 App.razor 行为）
        _head.AddStylesheet("https://fonts.googleapis.com/css?family=Roboto:300,400,500,700&display=swap");
        _head.AddStylesheet("/css/app.css");
        _head.AddHeadHtml("<title>CJDSL - DSL 驱动 Web 应用系统</title>");

        // body 尾：richtext.js + 内联导出脚本（原样保留）
        _head.AddScript("_content/CJDSL.Blazor/js/cjdsl-richtext.js");
        _head.AddBodyHtml("""
            <script>
                // export Handler 通过 IJSRuntime 调用，触发浏览器下载 CSV
                window.CJDSL = window.CJDSL || {};
                window.CJDSL.downloadFile = function (name, content) {
                    var blob = new Blob([content], { type: 'text/csv;charset=utf-8;' });
                    var url = URL.createObjectURL(blob);
                    var a = document.createElement('a');
                    a.href = url;
                    a.download = name;
                    document.body.appendChild(a);
                    a.click();
                    document.body.removeChild(a);
                    URL.revokeObjectURL(url);
                };
            </script>
            """);

        return ValueTask.CompletedTask;
    }
}
