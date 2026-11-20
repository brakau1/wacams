using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WACAMS.Api.Integration.GIS.BackgroundServices;
using WACAMS.Api.Integration.GIS.Configuration;
using WACAMS.Api.Integration.GIS.Repositories;
using WACAMS.Api.Integration.GIS.Services;

namespace WACAMS.Api.Integration.GIS.Extensions;

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
