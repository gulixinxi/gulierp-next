using Microsoft.Extensions.DependencyInjection;

namespace GuliERP.Foundation;

public static class DependencyInjection
{
    public static IServiceCollection AddGuliErpFoundation(this IServiceCollection services)
    {
        services.AddSingleton<IFoundationBoundary, FoundationBoundary>();
        return services;
    }
}

