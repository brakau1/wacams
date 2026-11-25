using WACAMS.Api.Services;

namespace WACAMS.Api.Extensions;

/// <summary>
/// Registers all application services (controllers, auth, business services).
/// </summary>
public static class ApiServiceCollectionExtensions
{
    public static IServiceCollection AddApiServices(this IServiceCollection services)
    {
        // ── Auth Services ──
        services.AddScoped<AuthService>();

        // ── Business Services ──
        services.AddScoped<EmployeeService>();
        services.AddScoped<AttendanceService>();
        services.AddScoped<ExceptionService>();
        services.AddScoped<WarningService>();
        services.AddScoped<EscalationService>();
        services.AddScoped<ReportService>();
        services.AddScoped<AuditService>();
        services.AddScoped<NotificationService>();

        // ── Controllers are auto-registered by AddControllers() ──

        return services;
    }
}
