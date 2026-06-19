using CJDSL.Application.Mapping;
using FluentValidation;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.DependencyInjection;

namespace CJDSL.Api;

public static class ApiServiceExtensions
{
    public static IServiceCollection AddCJDSLApi(this IServiceCollection services)
    {
        // AutoMapper
        services.AddAutoMapper(cfg =>
        {
            cfg.AddProfile<DslMappingProfile>();
        });

        // MediatR
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssembly(typeof(Application.Dsl.Commands.GenerateDslCommand).Assembly));

        // FluentValidation
        services.AddValidatorsFromAssembly(typeof(Application.Dsl.Commands.GenerateDslCommand).Assembly);

        // Swagger
        services.AddEndpointsApiExplorer();
        services.AddSwaggerGen();

        return services;
    }

    public static IApplicationBuilder UseCJDSLApi(this IApplicationBuilder app)
    {
        app.UseSwagger();
        app.UseSwaggerUI();
        return app;
    }
}
