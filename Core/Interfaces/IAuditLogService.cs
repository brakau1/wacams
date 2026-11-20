namespace WACAMS.Core.Interfaces;

/// <summary>
/// Service interface for creating and retrieving audit log entries.
/// Every Create, Update, and Delete operation on core entities must call this service
/// to maintain an immutable audit trail for compliance and troubleshooting.
/// </summary>
public interface IAuditLogService
{
    /// <summary>
    /// Creates a new audit log entry recording a domain change.
    /// Must be called after every entity modification through the application layer.
    /// The old value should contain a JSON snapshot of the entity state before the change.
    /// The new value should contain a JSON snapshot of the entity state after the change.
    /// For Create operations: OldValue is null, NewValue contains the full entity JSON.
    /// For Delete operations: OldValue contains the full entity JSON, NewValue is null.
    /// </summary>
    /// <param name="entityType">The domain entity type (e.g., "Employee", "AttendanceRecord").</param>
    /// <param name="entityId">The business key of the affected record (e.g., EmployeeId).</param>
    /// <param name="action">The action performed ("Create", "Update", or "Delete").</param>
    /// <param name="oldValue">JSON-serialized previous state (null for Create).</param>
    /// <param name="newValue">JSON-serialized new state (null for Delete).</param>
    /// <param name="performedBy">Employee ID of the user who made the change.</param>
    /// <param name="ipAddress">IP address from which the action was initiated.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    Task LogChangeAsync(
        string entityType,
        string entityId,
        string action,
        string? oldValue,
        string? newValue,
        string performedBy,
        string? ipAddress = null,
        CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves the complete audit trail for a specific entity.
    /// Returns all changes ordered chronologically (oldest first).
    /// Used for compliance audits, investigation reviews, and debugging data issues.
    /// </summary>
    /// <param name="entityType">The domain entity type to filter by.</param>
    /// <param name="entityId">The business key of the specific record.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>All audit log entries for the entity, ordered by PerformedAt ascending.</returns>
    Task<IEnumerable<Entities.AuditLog>> GetAuditTrailAsync(
        string entityType, string entityId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves audit log entries filtered by date range, performer, and optional action type.
    /// Used for compliance reporting and supervisor review of team member activities.
    /// </summary>
    /// <param name="startDate">Start of the audit period (inclusive).</param>
    /// <param name="endDate">End of the audit period (inclusive).</param>
    /// <param name="performedBy">Optional employee ID to filter by actor.</param>
    /// <param name="action">Optional action type to filter ("Create", "Update", "Delete").</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>Audit log entries matching all filters, ordered by PerformedAt descending.</returns>
    Task<IEnumerable<Entities.AuditLog>> GetFilteredAsync(
        DateTime startDate,
        DateTime endDate,
        string? performedBy = null,
        string? action = null,
        CancellationToken cancellationToken = default);
}
