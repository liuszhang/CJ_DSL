using CJDSL.Api;
using CJDSL.Api.Endpoints;
using CJDSL.Infrastructure;
using CJDSL.Web.Components;
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

        // CJDSL 各层服务
        builder.Services.AddCJDSLApi();
        builder.Services.AddCJDSLInfrastructure();

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
