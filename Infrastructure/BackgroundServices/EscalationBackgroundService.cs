using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;
using WACAMS.Core.Services;

namespace WACAMS.Infrastructure.BackgroundServices;

/// <summary>
/// Background service that periodically checks for escalations due for escalation
/// </summary>
public class EscalationBackgroundService : BackgroundService
{
    private readonly IServiceProvider _serviceProvider;
    private readonly ILogger<EscalationBackgroundService> _logger;
    private readonly TimeSpan _checkInterval = TimeSpan.FromMinutes(15);

    public EscalationBackgroundService(
        IServiceProvider serviceProvider,
        ILogger<EscalationBackgroundService> logger)
    {
        _serviceProvider = serviceProvider;
        _logger = logger;
    }

    protected override async Task ExecuteAsync(CancellationToken stoppingToken)
    {
        _logger.LogInformation("Escalation background service started");

        while (!stoppingToken.IsCancellationRequested)
        {
            try
            {
                await ProcessEscalationsAsync(stoppingToken);
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Error processing escalations");
            }

            await Task.Delay(_checkInterval, stoppingToken);
        }

        _logger.LogInformation("Escalation background service stopped");
    }

    private async Task ProcessEscalationsAsync(CancellationToken stoppingToken)
    {
        using var scope = _serviceProvider.CreateScope();
        var escalationService = scope.ServiceProvider.GetRequiredService<IEscalationService>();

        _logger.LogDebug("Checking for escalations due for escalation");

        await escalationService.ProcessEscalationsAsync();

        _logger.LogDebug("Escalation processing completed");
    }
}
