using CJDSL.Api;
using CJDSL.Api.Endpoints;
using CJDSL.Blazor.Components.Renderers;
using CJDSL.Infrastructure;
using CJDSL.Web.Components;
using CJDSL.Web.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor.Services;
using CJCore.Framework.Api;
using CJCore.Framework.Abstractions;

namespace CJDSL.Web;

public class Program
{
    public static void Main(string[] args)
    {
        var builder = WebApplication.CreateBuilder(args);

        // 添加服务
        builder.Services.AddRazorComponents()
            .AddInteractiveServerComponents();

        builder.Services.AddMudServices();

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
        builder.Services.AddCJDSLInfrastructure(builder.Configuration);
        builder.Services.AddDslRenderers();

        // 浮窗管理服务
        builder.Services.AddSingleton<FloatService>();

        // ★ CJDSL 模块 + 菜单注册
        builder.Services.AddSingleton<IModule, CJDSLModule>();
        builder.Services.AddSingleton<IMenuService, CJDSLMenuService>();

        var app = builder.Build();

        if (!app.Environment.IsDevelopment())
        {
            app.UseExceptionHandler("/Error");
            app.UseHsts();
        }

        app.UseHttpsRedirection();
        app.UseStaticFiles();
        app.UseAntiforgery();

        app.UseCJDSLApi();
        app.MapDslEndpoints();

        // Blazor 组件
        app.MapRazorComponents<App>()
            .AddInteractiveServerRenderMode();

        app.Run();
    }
}
