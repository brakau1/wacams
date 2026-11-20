using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using WACAMS.Core.Interfaces;
using WACAMS.Core.Services;
using WACAMS.Infrastructure.Data;
using WACAMS.Infrastructure.Repositories;
using WACAMS.Infrastructure.Services;

namespace WACAMS.Infrastructure.Extensions;

/// <summary>
/// Extension methods for registering Infrastructure services and DbContext with DI container.
/// Usage: services.AddInfrastructureServices(Configuration);
/// </summary>
public static class ServiceCollectionExtensions
{
    /// <summary>
    /// Registers all Infrastructure services including DbContext, repositories, and domain services.
    /// </summary>
    /// <param name="services">The service collection to add services to.</param>
    /// <param name="configuration">Application configuration (used for connection strings).</param>
    /// <returns>The updated service collection.</returns>
    public static IServiceCollection AddInfrastructureServices(
        this IServiceCollection services,
        IConfiguration configuration)
    {
        // Register DbContext with PostgreSQL provider
        var connectionString = configuration.GetConnectionString("WacamDb")
            ?? throw new InvalidOperationException(
                "Connection string 'WacamDb' not found in configuration.");

        services.AddDbContext<WacamContext>(options =>
            options.UseNpgsql(connectionString,
                o => o.MigrationsAssembly(typeof(WacamContext).Assembly.FullName)),
            ServiceLifetime.Scoped);


        // Register Repositories (Scoped — one per HTTP request / unit of work)
        services.AddScoped(typeof(IRepository<>), typeof(Repository<>));
        services.AddScoped<IEmployeeRepository, EmployeeRepository>();
        services.AddScoped<IAttendanceRepository, AttendanceRepository>();
        services.AddScoped<IWarningRepository, WarningRepository>();
        services.AddScoped<IAuditLogService, AuditLogService>();

        // Register Unit of Work
        services.AddScoped<IUnitOfWork, UnitOfWork>();

        // Register Domain Services
        services.AddScoped<IAttendanceCalculationService, AttendanceCalculationService>();
        services.AddScoped<IWarningGenerationService, WarningGenerationService>();
        services.AddScoped<IEscalationService, EscalationService>();

        return services;
    }
}