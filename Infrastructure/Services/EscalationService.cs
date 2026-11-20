using Microsoft.Extensions.Logging;
using WACAMS.Core.Escalations.Interfaces;
using WACAMS.Core.Escalations.Models;
using WACAMS.Core.Escalations.Rules;

namespace Infrastructure.Services;

/// <summary>
/// Service implementation for escalation workflow management
/// </summary>
public class EscalationService : IEscalationService
{
    private readonly IEscalationRepository _repository;
    private readonly ILogger<EscalationService> _logger;
    private readonly EscalationRules _rules;

    public EscalationService(
        IEscalationRepository repository,
        ILogger<EscalationService> logger,
        EscalationRules rules)
    {
        _repository = repository;
        _logger = logger;
        _rules = rules;
    }

    public async Task<Escalation> CreateEscalationAsync(Guid exceptionId, string createdBy)
    {
        _logger.LogInformation("Creating escalation for exception {ExceptionId}, created by {CreatedBy}",
            exceptionId, createdBy);

        var escalation = new Escalation
        {
            Id = Guid.NewGuid(),
            ExceptionId = exceptionId,
            CurrentLevel = EscalationLevel.ManagerLevel1,
            AssignedTo = "ManagerLevel1", // TODO: Map to actual manager ID
            Status = EscalationStatus.Pending,
            CreatedAt = DateTime.UtcNow,
            NextEscalationTime = _rules.CalculateNextEscalationTime(EscalationLevel.ManagerLevel1, DateTime.UtcNow),
            DocumentUploaded = false,
            CreatedBy = createdBy
        };

        var created = await _repository.CreateAsync(escalation);

        _logger.LogInformation("Escalation {EscalationId} created at level {Level}, assigned to {AssignedTo}",
            created.Id, created.CurrentLevel, created.AssignedTo);

        return created;
    }

    public async Task<Escalation> GetEscalationByIdAsync(Guid id)
    {
        var escalation = await _repository.GetByIdAsync(id);
        return escalation ?? throw new KeyNotFoundException($"Escalation {id} not found");
    }

    public async Task<IEnumerable<Escalation>> GetEscalationsByExceptionAsync(Guid exceptionId)
    {
        return await _repository.GetByExceptionIdAsync(exceptionId);
    }

    public async Task<Escalation> AcknowledgeEscalationAsync(Guid id, string acknowledgedBy)
    {
        var escalation = await _repository.GetByIdAsync(id);
        if (escalation == null)
        {
            throw new KeyNotFoundException($"Escalation {id} not found");
        }

        escalation.Status = EscalationStatus.Acknowledged;
        escalation.AcknowledgedAt = DateTime.UtcNow;
        escalation.UpdatedBy = acknowledgedBy;
        escalation.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(escalation);

        _logger.LogInformation("Escalation {EscalationId} acknowledged by {User}",
            id, acknowledgedBy);

        return escalation;
    }

    public async Task<Escalation> MarkActionTakenAsync(Guid id, string actionNotes, string updatedBy)
    {
        var escalation = await _repository.GetByIdAsync(id);
        if (escalation == null)
        {
            throw new KeyNotFoundException($"Escalation {id} not found");
        }

        escalation.Status = EscalationStatus.ActionTaken;
        escalation.ActionTakenNotes = actionNotes;
        escalation.ActionTakenAt = DateTime.UtcNow;
        escalation.UpdatedBy = updatedBy;
        escalation.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(escalation);

        _logger.LogInformation("Escalation {EscalationId} action taken by {User}",
            id, updatedBy);

        return escalation;
    }

    public async Task<Escalation> EscalateAsync(Guid id, string escalatedBy)
    {
        var escalation = await _repository.GetByIdAsync(id);
        if (escalation == null)
        {
            throw new KeyNotFoundException($"Escalation {id} not found");
        }

        var nextLevel = _rules.GetNextLevel(escalation.CurrentLevel);
        if (nextLevel == null)
        {
            _logger.LogWarning("No higher escalation level found for escalation {EscalationId}", id);
            escalation.Status = EscalationStatus.AutoClosed;
            escalation.AutoClosedAt = DateTime.UtcNow;
            escalation.UpdatedBy = escalatedBy;
            escalation.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(escalation);
            return escalation;
        }

        escalation.CurrentLevel = nextLevel.Value;
        escalation.AssignedTo = nextLevel.Value.ToString();
        escalation.Status = EscalationStatus.Escalated;
        escalation.EscalatedAt = DateTime.UtcNow;
        escalation.NextEscalationTime = _rules.CalculateNextEscalationTime(nextLevel.Value, DateTime.UtcNow);
        escalation.UpdatedBy = escalatedBy;
        escalation.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(escalation);

        _logger.LogInformation("Escalation {EscalationId} escalated to level {Level}, assigned to {AssignedTo}",
            id, nextLevel.Value, escalation.AssignedTo);

        return escalation;
    }

    public async Task<Escalation> UploadDocumentAsync(Guid id, string documentUrl, string uploadedBy)
    {
        var escalation = await _repository.GetByIdAsync(id);
        if (escalation == null)
        {
            throw new KeyNotFoundException($"Escalation {id} not found");
        }

        escalation.DocumentUploaded = true;
        escalation.DocumentUrl = documentUrl;
        escalation.DocumentUploadedAt = DateTime.UtcNow;
        escalation.UpdatedBy = uploadedBy;
        escalation.UpdatedAt = DateTime.UtcNow;

        // Auto-close when document is uploaded
        if (escalation.Status != EscalationStatus.Resolved)
        {
            escalation.Status = EscalationStatus.AutoClosed;
            escalation.AutoClosedAt = DateTime.UtcNow;
        }

        await _repository.UpdateAsync(escalation);

        _logger.LogInformation("Escalation {EscalationId} document uploaded by {User}: {DocumentUrl}",
            id, uploadedBy, documentUrl);

        return escalation;
    }

    public async Task<Escalation> ResolveEscalationAsync(Guid id, string resolvedBy)
    {
        var escalation = await _repository.GetByIdAsync(id);
        if (escalation == null)
        {
            throw new KeyNotFoundException($"Escalation {id} not found");
        }

        escalation.Status = EscalationStatus.Resolved;
        escalation.ResolvedAt = DateTime.UtcNow;
        escalation.UpdatedBy = resolvedBy;
        escalation.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(escalation);

        _logger.LogInformation("Escalation {EscalationId} resolved by {User}",
            id, resolvedBy);

        return escalation;
    }

    public async Task ProcessEscalationsAsync()
    {
        var now = DateTime.UtcNow;
        var dueForEscalation = await _repository.GetDueForEscalationAsync(now);

        _logger.LogInformation("Processing {Count} escalations due for escalation", dueForEscalation.Count());

        foreach (var escalation in dueForEscalation)
        {
            try
            {
                await EscalateAsync(escalation.Id, "System");
            }
            catch (Exception ex)
            {
                _logger.LogError(ex, "Failed to process escalation {EscalationId}", escalation.Id);
            }
        }
    }
}
