using CJDSL.Api;
using CJDSL.Api.Endpoints;
using CJDSL.Blazor.Components.Renderers;
using CJDSL.Infrastructure;
using CJDSL.Web.Components;
using CJDSL.Web.Services;
using Microsoft.AspNetCore.Components;
using MudBlazor.Services;

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

        // CJDSL 各层服务
        builder.Services.AddCJDSLApi();
        builder.Services.AddCJDSLInfrastructure(builder.Configuration);
        builder.Services.AddDslRenderers();

        // 浮窗管理服务
        builder.Services.AddSingleton<FloatService>();

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
