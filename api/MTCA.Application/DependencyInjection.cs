using FluentValidation;
using Microsoft.Extensions.DependencyInjection;

namespace MTCA.Application;

public static class DependencyInjection
{
    public static IServiceCollection AddApplication(this IServiceCollection services)
    {
        services.AddMediatR(cfg => cfg.RegisterServicesFromAssemblyContaining<IAssemblyMarker>());
        services.AddValidatorsFromAssemblyContaining<IAssemblyMarker>();
        return services;
    }
}
