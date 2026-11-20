using WACAMS.Core.Entities;

namespace WACAMS.Core.Services;

/// <summary>
/// Service interface for escalation management
/// </summary>
public interface IEscalationService
{
    /// <summary>
    /// Creates a new escalation from a warning letter — assigns to the next manager in hierarchy.
    /// Called by WarningGenerationService when a Final Written Warning is issued.
    /// </summary>
    Task<Escalation> CreateEscalation(
        Guid warningLetterId,
        string employeeId,
        string toManagerId,
        int level,
        string actionRequired,
        string performedBy,
        CancellationToken cancellationToken = default);

    Task<Escalation> CreateEscalationAsync(Guid exceptionId, string createdBy);
    Task<Escalation> GetEscalationByIdAsync(Guid id);
    Task<IEnumerable<Escalation>> GetEscalationsByExceptionAsync(Guid exceptionId);
    Task<Escalation> AcknowledgeEscalationAsync(Guid id, string acknowledgedBy);
    Task<Escalation> MarkActionTakenAsync(Guid id, string actionNotes, string updatedBy);
    Task<Escalation> EscalateAsync(Guid id, string escalatedBy);
    Task<Escalation> UploadDocumentAsync(Guid id, string documentUrl, string uploadedBy);
    Task<Escalation> ResolveEscalationAsync(Guid id, string resolvedBy);
    Task ProcessEscalationsAsync();
}
