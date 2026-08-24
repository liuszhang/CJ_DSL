using CJDSL.Api;
using CJDSL.Api.Endpoints;
using CJDSL.Blazor.Components.Renderers;
using CJDSL.Infrastructure;
using CJDSL.Web.Components;
using CJDSL.Web.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor;
using MudBlazor.Services;
using CJCore.Framework.Api;
using CJCore.Framework.Abstractions;
using CJCore.Modules.Data;
using CJCore.Modules.LLM;
using System.Security.Cryptography.X509Certificates;

namespace CJDSL.Web;

public class Program
{
    public static async Task Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // ★ 同时绑定 http（5000）与 https（5001），https 使用本机受信任的 ASP.NET Core 开发证书。
        // 浏览器访问 https://localhost:5001（HSTS 升级后也是它）即可正常响应；
        // http 与 https 必须分占不同端口，否则同端口 TCP 冲突。命令行 --urls 优先级更高，可覆盖。
        builder.WebHost.UseUrls("http://localhost:5000", "https://localhost:5001");
        builder.WebHost.ConfigureKestrel((_, serverOptions) =>
        {
            // 显式为 https 端点加载开发证书。注意：直接 dotnet <dll> 启动时环境默认为
            // Production，Kestrel 不会自动选用开发证书，故此处手动从证书存储中加载。
            try
            {
                using var store = new X509Store(StoreName.My, StoreLocation.CurrentUser);
                store.Open(OpenFlags.ReadOnly);
                var cert = store.Certificates
                    .Find(X509FindType.FindBySubjectName, "localhost", validOnly: true)
                    .OfType<X509Certificate2>()
                    .OrderByDescending(c => c.NotBefore)
                    .FirstOrDefault();
                if (cert is not null)
                {
                    serverOptions.ConfigureHttpsDefaults(o => o.ServerCertificate = cert);
                }
            }
            catch
            {
                // 无可用开发证书时回退到纯 HTTP（http://localhost:5001 仍可用）
            }
        });

        // 添加服务
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddMudServices();
        builder.Services.AddMudMarkdownServices(); // MudBlazor.Markdown（markdown 组件渲染）

        builder.Services.AddHttpClient();
        builder.Services.AddScoped<HttpClient>(sp =>
        {
            var nav = sp.GetRequiredService<NavigationManager>();
            return new HttpClient { BaseAddress = new Uri(nav.BaseUri) };
        });

        // ★ CJCore 框架注册（主题 + AppBar + 模块发现 + 日志）
        builder.Services.AddCJCoreFramework(builder.Configuration, options =>
        {
            options.ProductName = "CJDSL";
            options.EnableThemeSwitcher = true;
            options.EnableLogDrawer = true;
            options.BaseUrl = builder.Configuration["BaseUrl"];
        });

        // CJDSL 各层服务
        builder.Services.AddCJDSLApi();
        builder.Services.AddCJDSLGeneration(builder.Configuration);
        builder.Services.AddCJDSLPersistence(builder.Configuration);
        builder.Services.AddDslRenderers();

        // ★ 模块 J：LLM 配置收敛到 CJCore（数据层独立 SQLite：cjdsl_llm.db）
        // - 供应商 / 模型全部由 CJCore「LLM 配置」页面与种子数据管理
        // - 旧版 system-config.json 中激活的提供商由下方 Seed 一次性迁移（幂等，见 CjdslLlmConfigMigrationProvider）
        builder.Services.AddCJCoreLLM(options =>
        {
            // CJDSL 不从代码内嵌 LLM 配置；配置由「LLM 配置」页面与种子数据驱动
            options.UseOpenAI = false;
            options.UseOllama = false;
            // 服务端回连本应用管理接口使用 http://localhost:5000（与下方 http 绑定端口一致；
            // https 仅用于浏览器访问 https://localhost:5001，二者端口不同以避免同端口协议冲突）。
            options.ApiBaseUrl = "http://localhost:5000";
        }, dataOptions =>
        {
            dataOptions.Provider = DataProvider.Sqlite;
            dataOptions.ConnectionString = "Data Source=cjdsl_llm.db";
        });

        // 旧版 system-config.json → CJCore 数据层 一次性迁移（仅当存在激活提供商时执行，幂等）
        builder.Services.AddSingleton<ISeedDataProvider, CjdslLlmConfigMigrationProvider>();

        // 浮窗管理服务
        builder.Services.AddSingleton<FloatService>();

        // ★ CJDSL 模块 + 菜单注册
        builder.Services.AddSingleton<IModule, CJDSLModule>();
        builder.Services.AddSingleton<IMenuService, CJDSLMenuService>();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
        }

        // 模块数据层建表（幂等：CREATE TABLE IF NOT EXISTS）
        await app.Services.EnsureDataDbCreatedAsync();

        // 静态资产（.NET 10 必须 MapStaticAssets：UseStaticFiles 不服务 StaticWebAssets 清单资产，
        // 会导致 _content/_framework/wwwroot 全部回落到首页 HTML —— 2026-08-07 修复）
        app.MapStaticAssets();
        app.UseAntiforgery();

        app.UseCJDSLApi();
        app.MapDslEndpoints();
        app.MapBusinessApiEndpoints();
        // ★ 模块 J：CJCore LLM 配置 API（/api/llm-config/...）
        app.MapCJCoreLLM();

        // Blazor + 模块路由发现（框架统一根组件 FrameworkApp，自动收集 IModule 程序集）
        app.MapCJCoreRazorComponents();

        // 种子数据（供应商预设 + 旧配置迁移）
        await app.Services.RunSeedDataAsync();

        await app.RunAsync();
    }
}
