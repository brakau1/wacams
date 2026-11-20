using WACAMS.Core.Escalations.Models;

namespace WACAMS.Core.Escalations.Interfaces;

/// <summary>
/// Service interface for escalation management
/// </summary>
public interface IEscalationService
{
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
