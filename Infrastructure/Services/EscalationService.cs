using Microsoft.Extensions.Logging;
using WACAMS.Core.Constants;
using WACAMS.Core.Entities;
using WACAMS.Core.Enums;
using WACAMS.Core.Escalations.Interfaces;
using WACAMS.Core.Escalations.Rules;
using WACAMS.Core.Interfaces;

namespace WACAMS.Infrastructure.Services;

/// <summary>
/// Service implementation for escalation workflow management.
/// Implements both Core.Services.IEscalationService and Core.Escalations.Interfaces.IEscalationService.
/// </summary>
public class EscalationService : Core.Services.IEscalationService, Core.Escalations.Interfaces.IEscalationService
{
    private readonly IEscalationRepository _repository;
    private readonly IEmployeeRepository _employeeRepository;
    private readonly ILogger<EscalationService> _logger;
    private readonly EscalationRules _rules;

    public EscalationService(
        IEscalationRepository repository,
        IEmployeeRepository employeeRepository,
        ILogger<EscalationService> logger,
        EscalationRules rules)
    {
        _repository = repository;
        _employeeRepository = employeeRepository;
        _logger = logger;
        _rules = rules;
    }

    /// <summary>
    /// Creates a new escalation from a warning letter — assigns to the next manager in hierarchy.
    /// Called by WarningGenerationService when a Final Written Warning is issued.
    /// </summary>
    public async Task<Escalation> CreateEscalation(
        Guid warningLetterId,
        string employeeId,
        string toManagerId,
        int level,
        string actionRequired,
        string performedBy,
        CancellationToken cancellationToken = default)
    {
        _logger.LogInformation("Creating escalation for warning {WarningId}, employee {EmployeeId}, level {Level}",
            warningLetterId, employeeId, level);

        var escalation = new Escalation
        {
            Id = Guid.NewGuid(),
            WarningLetterId = warningLetterId,
            ExceptionId = Guid.Empty, // Will be linked later if needed
            FromManagerId = performedBy,
            ToManagerId = toManagerId,
            AssignedTo = toManagerId,
            Level = level,
            Status = EscalationStatus.Pending,
            ActionRequired = actionRequired,
            EmployeeId = employeeId,
            NextEscalationTime = DateTime.UtcNow.AddHours(WarningThresholds.EscalationActionWindowHours),
            DocumentUploaded = false,
            CreatedBy = performedBy,
            CreatedAt = DateTime.UtcNow
        };

        var created = await _repository.CreateAsync(escalation);

        _logger.LogInformation("Escalation {EscalationId} created at level {Level}, assigned to {AssignedTo}",
            created.Id, created.Level, created.AssignedTo);

        return created;
    }

    /// <summary>
    /// Creates a new escalation from an exception ID (simplified path).
    /// </summary>
    public async Task<Escalation> CreateEscalationAsync(Guid exceptionId, string createdBy)
    {
        _logger.LogInformation("Creating escalation for exception {ExceptionId}, created by {CreatedBy}",
            exceptionId, createdBy);

        var escalation = new Escalation
        {
            Id = Guid.NewGuid(),
            ExceptionId = exceptionId,
            WarningLetterId = Guid.Empty,
            FromManagerId = createdBy,
            ToManagerId = "ManagerLevel1",
            AssignedTo = "ManagerLevel1",
            Level = 1,
            Status = EscalationStatus.Pending,
            ActionRequired = "Review and act on attendance exception",
            EmployeeId = string.Empty,
            CreatedAt = DateTime.UtcNow,
            NextEscalationTime = DateTime.UtcNow.AddHours(WarningThresholds.EscalationActionWindowHours),
            DocumentUploaded = false,
            CreatedBy = createdBy
        };

        var created = await _repository.CreateAsync(escalation);

        _logger.LogInformation("Escalation {EscalationId} created at level {Level}, assigned to {AssignedTo}",
            created.Id, created.Level, created.AssignedTo);

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

        escalation.Status = EscalationStatus.Accepted;
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

        var nextLevel = escalation.Level + 1;
        if (nextLevel > WarningThresholds.MaxEscalationLevel)
        {
            _logger.LogWarning("Max escalation level reached for escalation {EscalationId}", id);
            escalation.Status = EscalationStatus.Closed;
            escalation.AutoClosedAt = DateTime.UtcNow;
            escalation.UpdatedBy = escalatedBy;
            escalation.UpdatedAt = DateTime.UtcNow;
            await _repository.UpdateAsync(escalation);
            return escalation;
        }

        escalation.Level = nextLevel;
        escalation.Status = EscalationStatus.Escalated;
        escalation.EscalatedAt = DateTime.UtcNow;
        escalation.NextEscalationTime = DateTime.UtcNow.AddHours(WarningThresholds.EscalationActionWindowHours);
        escalation.UpdatedBy = escalatedBy;
        escalation.UpdatedAt = DateTime.UtcNow;

        await _repository.UpdateAsync(escalation);

        _logger.LogInformation("Escalation {EscalationId} escalated to level {Level}",
            id, nextLevel);

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
        if (escalation.Status != EscalationStatus.ActionTaken)
        {
            escalation.Status = EscalationStatus.Closed;
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

        escalation.Status = EscalationStatus.ActionTaken;
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
