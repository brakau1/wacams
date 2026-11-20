using Microsoft.EntityFrameworkCore;
using Wacam.Api.Data;
using WACAMS.Core.Entities;

namespace Wacam.Api.Jobs;

/// <summary>
/// Hourly job: Expire corrective actions past their validity period.
/// Marks actions as expired and logs audit trail.
/// </summary>
public class CorrectiveActionExpiryJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<CorrectiveActionExpiryJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public CorrectiveActionExpiryJob(IServiceProvider serviceProvider, ILogger<CorrectiveActionExpiryJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("CorrectiveActionExpiryJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunExpiryCheckAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in CorrectiveActionExpiryJob");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunExpiryCheckAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WacamDbContext>();
        var auditService = scope.ServiceProvider.GetRequiredService<AuditService>();

        var now = DateTime.UtcNow;
        int expiredCount = 0;

        // Find active corrective actions past expiry date
        var expiredActions = await context.CorrectiveActions
            .Include(ca => ca.Employee)
            .Where(ca => ca.IsActive && ca.ExpiryDate < now)
            .ToListAsync();

        foreach (var action in expiredActions)
        {
            action.IsActive = false;
            action.Status = "Expired";
            action.UpdatedAt = now;

            // Audit log
            await auditService.LogActionAsync(
                "system",
                "CorrectiveActionExpired",
                "CorrectiveAction",
                action.Id.ToString(),
                null,
                $"Corrective action expired (expiry date: {action.ExpiryDate:yyyy-MM-dd})"
            );

            expiredCount++;
            _logger.LogInformation("Expired CorrectiveAction {Id} for Employee {EmployeeId}",
                action.Id, action.EmployeeId);
        }

        await context.SaveChangesAsync();

        _logger.LogInformation("CorrectiveActionExpiryJob completed: {Count} actions expired", expiredCount);
    }
}
