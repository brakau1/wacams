namespace WACAMS.Infrastructure.Audit;

/// <summary>
/// Interface for audit logging
/// </summary>
public interface IAuditService
{
    Task LogAsync(string action, string entityType, string entityId, string userId, string? details = null);
    Task<IEnumerable<AuditLog>> GetLogsAsync(string entityType, string entityId);
    Task<IEnumerable<AuditLog>> GetLogsByUserAsync(string userId);
}
