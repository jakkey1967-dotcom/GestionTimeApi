using GestionTime.Infrastructure.Services.Freshdesk;
using GestionTime.Infrastructure.Persistence;

namespace Microsoft.Extensions.DependencyInjection;

public static class FreshdeskServiceExtensions
{
    public static IServiceCollection AddFreshdeskIntegration(
        this IServiceCollection services,
        Microsoft.Extensions.Configuration.IConfiguration configuration)
    {
        services.Configure<FreshdeskOptions>(
            configuration.GetSection(FreshdeskOptions.SectionName));
        
        services.AddHttpClient<FreshdeskClient>();
        services.AddScoped<FreshdeskService>();
        
        return services;
    }
}
