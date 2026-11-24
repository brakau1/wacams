using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WACAMS.Infrastructure.BackgroundServices;
using WACAMS.Infrastructure.Configuration;
using WACAMS.Infrastructure.Repositories.GIS;
using WACAMS.Infrastructure.Services.GIS;

namespace WACAMS.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering GIS iWork integration services
/// </summary>
public static class GISServiceCollectionExtensions
{
    /// <summary>
    /// Register GIS iWork integration services
    /// </summary>
    public static IServiceCollection AddGISIntegration(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Bind configuration
        var gisConfig = new GISConfiguration();
        configuration.GetSection(GISConfiguration.SectionName).Bind(gisConfig);
        services.AddSingleton(gisConfig);

        // Register repositories
        services.AddScoped<IGISIntegrationRepository, GISIntegrationRepository>();

        // Register services
        services.AddScoped<IGISIntegrationService, GISIntegrationService>();

        // Register background services
        if (gisConfig.EnableScheduledSync)
        {
            services.AddHostedService<GISScheduledSyncService>();
        }

        return services;
    }

    /// <summary>
    /// Register GIS iWork integration with custom configuration
    /// </summary>
    public static IServiceCollection AddGISIntegration(
        this IServiceCollection services,
        Action<GISConfiguration> configureOptions)
    {
        var gisConfig = new GISConfiguration();
        configureOptions(gisConfig);
        services.AddSingleton(gisConfig);

        // Register repositories
        services.AddScoped<IGISIntegrationRepository, GISIntegrationRepository>();

        // Register services
        services.AddScoped<IGISIntegrationService, GISIntegrationService>();

        // Register background services
        if (gisConfig.EnableScheduledSync)
        {
            services.AddHostedService<GISScheduledSyncService>();
        }

        return services;
    }
}
