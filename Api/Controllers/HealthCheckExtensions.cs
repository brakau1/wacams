using Microsoft.AspNetCore.Diagnostics.HealthChecks;
using Microsoft.Extensions.Diagnostics.HealthChecks;

namespace WACAMS.Api.Controllers;

/// <summary>
/// Enhanced health check endpoints for deployment monitoring
/// </summary>
public static class HealthCheckExtensions
{
    public static void MapEnhancedHealthChecks(this WebApplication app)
    {
        // Basic health check
        app.MapHealthChecks("/health", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthCheckResponse
        });

        // Detailed health check (includes all dependencies)
        app.MapHealthChecks("/health/detailed", new HealthCheckOptions
        {
            ResponseWriter = WriteDetailedHealthCheckResponse,
            Predicate = _ => true
        });

        // Database-only health check
        app.MapHealthChecks("/health/db", new HealthCheckOptions
        {
            ResponseWriter = WriteHealthCheckResponse,
            Predicate = check => check.Name.Contains("Database", StringComparison.OrdinalIgnoreCase)
        });

        // Readiness check (for Kubernetes)
        app.MapHealthChecks("/health/ready", new HealthCheckOptions
        {
            ResponseWriter = WriteReadinessResponse,
            Predicate = _ => true
        });

        // Liveness check (for Kubernetes)
        app.MapHealthChecks("/health/live", new HealthCheckOptions
        {
            ResponseWriter = WriteLivenessResponse,
            Predicate = _ => false // Only check if app is running
        });
    }

    private static async Task WriteHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        
        var json = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(json);
    }

    private static async Task WriteDetailedHealthCheckResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        
        var json = new
        {
            status = report.Status.ToString(),
            duration = report.TotalDuration.TotalMilliseconds,
            timestamp = DateTime.UtcNow,
            checks = report.Entries.Select(e => new
            {
                name = e.Key,
                status = e.Value.Status.ToString(),
                description = e.Value.Description,
                duration = e.Value.Duration.TotalMilliseconds,
                exception = e.Value.Exception?.Message,
                data = e.Value.Data
            })
        };

        await context.Response.WriteAsJsonAsync(json);
    }

    private static async Task WriteReadinessResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = report.Status == HealthStatus.Healthy ? 200 : 503;
        
        var json = new
        {
            status = report.Status.ToString(),
            ready = report.Status == HealthStatus.Healthy,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(json);
    }

    private static async Task WriteLivenessResponse(HttpContext context, HealthReport report)
    {
        context.Response.ContentType = "application/json";
        context.Response.StatusCode = 200;
        
        var json = new
        {
            status = "Healthy",
            live = true,
            timestamp = DateTime.UtcNow
        };

        await context.Response.WriteAsJsonAsync(json);
    }
}
