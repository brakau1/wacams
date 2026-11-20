using WACAMS.Core.Disciplinary.Models;
using WACAMS.Core.Disciplinary.Rules;
using WACAMS.Core.Disciplinary.Repositories;
using Microsoft.Extensions.Logging;

namespace WACAMS.Core.Disciplinary.Services;

/// <summary>
/// Engine for managing corrective actions and progressive discipline
/// </summary>
public class CorrectiveActionEngine : ICorrectiveActionEngine
{
    private readonly ICorrectiveActionRepository _actionRepository;
    private readonly IOffenceCounterRepository _counterRepository;
    private readonly IValidityPeriodRepository _validityRepository;
    private readonly ProgressiveDisciplineRule _disciplineRule;
    private readonly ILogger<CorrectiveActionEngine> _logger;

    public CorrectiveActionEngine(
        ICorrectiveActionRepository actionRepository,
        IOffenceCounterRepository counterRepository,
        IValidityPeriodRepository validityRepository,
        ILogger<CorrectiveActionEngine> logger)
    {
        _actionRepository = actionRepository;
        _counterRepository = counterRepository;
        _validityRepository = validityRepository;
        _disciplineRule = new ProgressiveDisciplineRule();
        _logger = logger;
    }

    public async Task<DisciplinaryActionResult> DetermineActionAsync(
        string employeeId,
        Guid exceptionId,
        OffenceType offenceType,
        string issuedBy,
        string reason)
    {
        try
        {
            _logger.LogInformation("Determining disciplinary action for employee {EmployeeId}, offence type {OffenceType}", 
                employeeId, offenceType);

            // Get current offence count
            var currentCount = await GetOffenceCountAsync(employeeId, offenceType);
            var newCount = currentCount + 1;

            // Determine disciplinary level
            var level = _disciplineRule.DetermineLevel(offenceType, newCount);
            
            if (level == DisciplinaryLevel.None)
            {
                return new DisciplinaryActionResult
                {
                    Success = false,
                    Errors = new List<string> { "Invalid offence count" }
                };
            }

            // Get validity period
            var validityPeriod = await _validityRepository.GetByLevelAsync(level);
            var validUntil = DateTime.UtcNow.AddDays(validityPeriod?.ValidityDays ?? 180);

            // Create corrective action
            var action = new CorrectiveAction
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                ExceptionId = exceptionId,
                OffenceType = offenceType,
                OffenceNumber = newCount,
                Level = level,
                Description = _disciplineRule.GetDescription(level, newCount),
                Reason = reason,
                IssuedBy = issuedBy,
                IssuedAt = DateTime.UtcNow,
                Status = CorrectiveActionStatus.Issued,
                ValidFrom = DateTime.UtcNow,
                ValidUntil = validUntil,
                RequiresHearing = _disciplineRule.RequiresHearing(level),
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow,
                CreatedBy = issuedBy,
                UpdatedBy = issuedBy
            };

            // Save action
            await _actionRepository.CreateAsync(action);

            // Update offence counter
            await UpdateOffenceCounterAsync(employeeId, offenceType, newCount);

            _logger.LogInformation("Created corrective action {ActionId} for employee {EmployeeId}, level {Level}, offence #{OffenceNumber}", 
                action.Id, employeeId, level, newCount);

            return new DisciplinaryActionResult
            {
                Success = true,
                DeterminedLevel = level,
                OffenceNumber = newCount,
                Description = action.Description,
                RequiresManagerApproval = true, // All actions require manager investigation first
                RequiresHearing = action.RequiresHearing,
                ValidUntil = validUntil
            };
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error determining disciplinary action for employee {EmployeeId}", employeeId);
            return new DisciplinaryActionResult
            {
                Success = false,
                Errors = new List<string> { ex.Message }
            };
        }
    }

    public async Task<int> GetOffenceCountAsync(string employeeId, OffenceType offenceType)
    {
        var counter = await _counterRepository.GetByEmployeeAndTypeAsync(employeeId, offenceType);
        return counter?.ActiveOffences ?? 0;
    }

    public async Task<bool> AcknowledgeActionAsync(Guid actionId, string acknowledgedBy)
    {
        try
        {
            var action = await _actionRepository.GetByIdAsync(actionId);
            if (action == null)
            {
                _logger.LogWarning("Corrective action {ActionId} not found", actionId);
                return false;
            }

            action.Status = CorrectiveActionStatus.Acknowledged;
            action.IsAcknowledged = true;
            action.AcknowledgedAt = DateTime.UtcNow;
            action.AcknowledgedBy = acknowledgedBy;
            action.UpdatedAt = DateTime.UtcNow;
            action.UpdatedBy = acknowledgedBy;

            await _actionRepository.UpdateAsync(action);

            _logger.LogInformation("Corrective action {ActionId} acknowledged by {AcknowledgedBy}", actionId, acknowledgedBy);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error acknowledging corrective action {ActionId}", actionId);
            return false;
        }
    }

    public async Task<bool> DisputeActionAsync(Guid actionId, string disputedBy, string reason)
    {
        try
        {
            var action = await _actionRepository.GetByIdAsync(actionId);
            if (action == null)
            {
                _logger.LogWarning("Corrective action {ActionId} not found", actionId);
                return false;
            }

            action.Status = CorrectiveActionStatus.Disputed;
            action.ResolutionNotes = reason;
            action.UpdatedAt = DateTime.UtcNow;
            action.UpdatedBy = disputedBy;

            await _actionRepository.UpdateAsync(action);

            _logger.LogInformation("Corrective action {ActionId} disputed by {DisputedBy}: {Reason}", actionId, disputedBy, reason);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error disputing corrective action {ActionId}", actionId);
            return false;
        }
    }

    public async Task<bool> ScheduleHearingAsync(Guid actionId, DateTime hearingDate, string scheduledBy)
    {
        try
        {
            var action = await _actionRepository.GetByIdAsync(actionId);
            if (action == null)
            {
                _logger.LogWarning("Corrective action {ActionId} not found", actionId);
                return false;
            }

            if (!action.RequiresHearing)
            {
                _logger.LogWarning("Corrective action {ActionId} does not require a hearing", actionId);
                return false;
            }

            action.Status = CorrectiveActionStatus.UnderHearing;
            action.HearingScheduledAt = hearingDate;
            action.UpdatedAt = DateTime.UtcNow;
            action.UpdatedBy = scheduledBy;

            await _actionRepository.UpdateAsync(action);

            _logger.LogInformation("Hearing scheduled for corrective action {ActionId} on {HearingDate}", actionId, hearingDate);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error scheduling hearing for corrective action {ActionId}", actionId);
            return false;
        }
    }

    public async Task<bool> RecordHearingOutcomeAsync(Guid actionId, string outcome, string recordedBy)
    {
        try
        {
            var action = await _actionRepository.GetByIdAsync(actionId);
            if (action == null)
            {
                _logger.LogWarning("Corrective action {ActionId} not found", actionId);
                return false;
            }

            action.HearingOutcome = outcome;
            action.HearingConductedAt = DateTime.UtcNow;
            
            // Determine final status based on outcome
            if (outcome.ToLower().Contains("upheld") || outcome.ToLower().Contains("dismissal"))
            {
                action.Status = CorrectiveActionStatus.Upheld;
            }
            else if (outcome.ToLower().Contains("overturned") || outcome.ToLower().Contains("withdrawn"))
            {
                action.Status = CorrectiveActionStatus.Overturned;
                // Decrement offence counter if overturned
                await DecrementOffenceCounterAsync(action.EmployeeId, action.OffenceType);
            }

            action.UpdatedAt = DateTime.UtcNow;
            action.UpdatedBy = recordedBy;

            await _actionRepository.UpdateAsync(action);

            _logger.LogInformation("Hearing outcome recorded for corrective action {ActionId}: {Outcome}", actionId, outcome);
            return true;
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error recording hearing outcome for corrective action {ActionId}", actionId);
            return false;
        }
    }

    private async Task UpdateOffenceCounterAsync(string employeeId, OffenceType offenceType, int newCount)
    {
        var counter = await _counterRepository.GetByEmployeeAndTypeAsync(employeeId, offenceType);
        
        if (counter == null)
        {
            counter = new OffenceCounter
            {
                Id = Guid.NewGuid(),
                EmployeeId = employeeId,
                OffenceType = offenceType,
                TotalOffences = newCount,
                ActiveOffences = newCount,
                LastOffenceDate = DateTime.UtcNow,
                CreatedAt = DateTime.UtcNow,
                UpdatedAt = DateTime.UtcNow
            };
            await _counterRepository.CreateAsync(counter);
        }
        else
        {
            counter.TotalOffences = newCount;
            counter.ActiveOffences = newCount;
            counter.LastOffenceDate = DateTime.UtcNow;
            counter.UpdatedAt = DateTime.UtcNow;
            await _counterRepository.UpdateAsync(counter);
        }
    }

    private async Task DecrementOffenceCounterAsync(string employeeId, OffenceType offenceType)
    {
        var counter = await _counterRepository.GetByEmployeeAndTypeAsync(employeeId, offenceType);
        
        if (counter != null && counter.ActiveOffences > 0)
        {
            counter.ActiveOffences--;
            counter.UpdatedAt = DateTime.UtcNow;
            await _counterRepository.UpdateAsync(counter);
        }
    }
}
