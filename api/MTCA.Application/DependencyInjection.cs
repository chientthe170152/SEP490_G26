using FluentValidation;
using Microsoft.Extensions.DependencyInjection;
using MTCA.Application.Common.Behaviors;

namespace MTCA.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg =>
        {
            cfg.RegisterServicesFromAssemblyContaining<IAssemblyMarker>();
            cfg.AddOpenBehavior(typeof(ValidationBehavior<,>));
        });
        services.AddValidatorsFromAssemblyContaining<IAssemblyMarker>();
        return services;
    }
}
