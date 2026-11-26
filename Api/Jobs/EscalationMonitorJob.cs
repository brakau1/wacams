using WACAMS.Infrastructure.Services;
using Wacam.Api.Services;
using Microsoft.EntityFrameworkCore;
using Wacam.Api.Data;
using WACAMS.Core.Entities;
using WACAMS.Core.Enums;

namespace Wacam.Api.Jobs;

/// <summary>
/// Hourly job: Monitor escalations and auto-escalate after 24h of inactivity.
/// Follows manager hierarchy: Team Leader → Production Manager → Next Level.
/// </summary>
public class EscalationMonitorJob : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EscalationMonitorJob> _logger;
    private readonly TimeSpan _interval = TimeSpan.FromHours(1);

    public EscalationMonitorJob(IServiceProvider serviceProvider, ILogger<EscalationMonitorJob> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("EscalationMonitorJob started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await RunEscalationCheckAsync();
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error in EscalationMonitorJob");
            }

            await Task.Delay(_interval, stoppingToken);
        }
    }

    private async Task RunEscalationCheckAsync()
    {
        using var scope = _serviceProvider.CreateScope();
        var context = scope.ServiceProvider.GetRequiredService<WacamDbContext>();
        var auditService = scope.ServiceProvider.GetRequiredService<AuditService>();
        var notificationService = scope.ServiceProvider.GetRequiredService<NotificationService>();

        var now = DateTime.UtcNow;
        var cutoff = now.AddHours(-24);

        // Find escalations that haven't been acted on in 24h
        var staleEscalations = await context.Escalations
            .Include(e => e.Employee)
                .ThenInclude(e => e!.Manager)
            .Where(e => e.Status == EscalationStatus.Open && e.UpdatedAt < cutoff)
            .ToListAsync();

        int escalatedCount = 0;

        foreach (var escalation in staleEscalations)
        {
            if (escalation.Employee?.ManagerId == null) continue;

            // Get next manager in hierarchy
            var currentManager = escalation.Employee.Manager;
            if (currentManager?.ManagerId == null)
            {
                _logger.LogWarning("Escalation {Id} reached top of hierarchy, cannot auto-escalate", escalation.Id);
                continue;
            }

            var nextManager = await context.Users.FindAsync(currentManager.ManagerId);
            if (nextManager == null) continue;

            // Escalate to next level
            escalation.Level++;
            escalation.AssignedToManagerId = nextManager.Id;
            escalation.EscalatedAt = now;
            escalation.UpdatedAt = now;
            escalation.Status = EscalationStatus.Escalated;

            // Log to history
            context.EscalationHistories.Add(new EscalationHistory
            {
                EscalationId = escalation.Id,
                FromLevel = escalation.Level - 1,
                ToLevel = escalation.Level,
                FromManagerId = currentManager.Id,
                ToManagerId = nextManager.Id,
                Reason = "Auto-escalation after 24h inactivity",
                EscalatedAt = now
            });

            // Audit log
            await auditService.LogActionAsync(
                "system",
                "AutoEscalation",
                "Escalation",
                escalation.Id.ToString(),
                null,
                $"Auto-escalated from Level {escalation.Level - 1} to Level {escalation.Level}"
            );

            // Notify next manager
            await notificationService.NotifyEscalationAsync(
                escalation.EmployeeId,
                escalation.Id,
                escalation.Level,
                nextManager.Id
            );

            escalatedCount++;
            _logger.LogInformation("Auto-escalated Escalation {Id} to Level {Level}, Manager {ManagerId}",
                escalation.Id, escalation.Level, nextManager.Id);
        }

        await context.SaveChangesAsync();

        _logger.LogInformation("EscalationMonitorJob completed: {Count} escalations auto-escalated", escalatedCount);
    }
}
