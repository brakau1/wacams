using WACAMS.Core.Disciplinary.Models;
using WACAMS.Core.Disciplinary.Repositories;
using Microsoft.Extensions.Logging;

namespace WACAMS.Core.Disciplinary.Services;

/// <summary>
/// Service for automatically expiring corrective actions
/// </summary>
public class AutoExpiryService : IAutoExpiryService
{
    private readonly ICorrectiveActionRepository _actionRepository;
    private readonly IOffenceCounterRepository _counterRepository;
    private readonly ILogger<AutoExpiryService> _logger;

    public AutoExpiryService(
        ICorrectiveActionRepository actionRepository,
        IOffenceCounterRepository counterRepository,
        ILogger<AutoExpiryService> logger)
    {
        _actionRepository = actionRepository;
        _counterRepository = counterRepository;
        _logger = logger;
    }

    public async Task<int> ProcessExpiredActionsAsync()
    {
        try
        {
            _logger.LogInformation("Starting auto-expiry process for corrective actions");

            var expiredActions = await _actionRepository.GetExpiredActionsAsync();
            var processedCount = 0;

            foreach (var action in expiredActions)
            {
                if (action.ValidUntil < DateTime.UtcNow && !action.IsExpired)
                {
                    action.IsExpired = true;
                    action.ExpiredAt = DateTime.UtcNow;
                    action.Status = CorrectiveActionStatus.Expired;
                    action.UpdatedAt = DateTime.UtcNow;
                    action.UpdatedBy = "SYSTEM";

                    await _actionRepository.UpdateAsync(action);

                    // Decrement active offence counter
                    await DecrementActiveCounterAsync(action.EmployeeId, action.OffenceType);

                    processedCount++;
                    _logger.LogInformation("Expired corrective action {ActionId} for employee {EmployeeId}", 
                        action.Id, action.EmployeeId);
                }
            }

            _logger.LogInformation("Auto-expiry process completed. Processed {Count} actions", processedCount);
            return processedCount;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error during auto-expiry process");
            throw;
        }
    }

    public async Task<bool> IsExpiredAsync(Guid actionId)
    {
        var action = await _actionRepository.GetByIdAsync(actionId);
        if (action == null)
        {
            return false;
        }

        return action.ValidUntil < DateTime.UtcNow;
    }

    public async Task<List<CorrectiveAction>> GetExpiringSoonAsync(int days)
    {
        var allActions = new List<CorrectiveAction>();
        
        // Get all actions and filter in memory (could be optimized with repository method)
        // For now, get actions from all employees
        // This is a simplified implementation - in production, add a repository method
        var cutoffDate = DateTime.UtcNow.AddDays(days);
        
        // This would need a repository method like GetActionsExpiringBeforeAsync
        // For demonstration, returning empty list
        _logger.LogWarning("GetExpiringSoonAsync not fully implemented - requires repository enhancement");
        
        return allActions;
    }

    private async Task DecrementActiveCounterAsync(string employeeId, OffenceType offenceType)
    {
        var counter = await _counterRepository.GetByEmployeeAndTypeAsync(employeeId, offenceType);
        
        if (counter != null && counter.ActiveOffences > 0)
        {
            counter.ActiveOffences--;
            counter.UpdatedAt = DateTime.UtcNow;
            await _counterRepository.UpdateAsync(counter);
            
            _logger.LogInformation("Decremented active offence counter for employee {EmployeeId}, type {OffenceType}. Active: {Count}", 
                employeeId, offenceType, counter.ActiveOffences);
        }
    }
}
