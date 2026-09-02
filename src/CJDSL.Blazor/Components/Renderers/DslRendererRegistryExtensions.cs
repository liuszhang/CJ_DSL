using Microsoft.Extensions.DependencyInjection;

namespace CJDSL.Blazor.Components.Renderers;

public static class DslRendererRegistryExtensions
{
    public static IServiceCollection AddDslRenderers(this IServiceCollection services)
    {
        services.AddSingleton<DslRendererRegistry>(sp =>
        {
            var registry = new DslRendererRegistry();

            registry.Register(new CardRenderer());
            registry.Register(new TextRenderer());
            registry.Register(new TextDisplayRenderer());
            registry.Register(new ButtonRenderer());
            registry.Register(new GridRenderer());
            registry.Register(new StackRenderer());
            registry.Register(new SelectRenderer());
            registry.Register(new DividerRenderer());
            registry.Register(new FlowRenderer());

            return registry;
        });

        return services;
    }
}
