using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Entities;
using WACAMS.Core.Interfaces;
using WACAMS.Infrastructure.Data;

namespace WACAMS.Infrastructure.Services;

/// <summary>
/// Domain service implementing IAuditLogService.
/// Provides explicit logging and retrieval of audit trail entries.
/// Used by services that need to capture domain changes outside of repository CRUD operations.
/// </summary>
public class AuditLogService : IAuditLogService
{
    private readonly WacamContext _context;

    public AuditLogService(WacamContext context)
    {
        _context = context;
    }

    /// <inheritdoc />
    public async Task LogChangeAsync(
        string entityType,
        string entityId,
        string action,
        string? oldValue,
        string? newValue,
        string performedBy,
        string? ipAddress = null,
        CancellationToken cancellationToken = default)
    {
        var auditEntry = new AuditLog
        {
            Id = Guid.NewGuid(),
            EntityType = entityType,
            EntityId = entityId,
            Action = action,
            OldValue = oldValue,
            NewValue = newValue,
            PerformedBy = performedBy,
            IpAddress = ipAddress,
            PerformedAt = DateTime.UtcNow
        };

        await _context.AuditLogs.AddAsync(auditEntry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetAuditTrailAsync(
        string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        return await _context.AuditLogs.AsNoTracking()
            .Where(al => al.EntityType == entityType && al.EntityId == entityId)
            .OrderBy(al => al.PerformedAt)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetFilteredAsync(
        DateTime startDate,
        DateTime endDate,
        string? performedBy = null,
        string? action = null,
        CancellationToken cancellationToken = default)
    {
        var query = _context.AuditLogs.AsNoTracking()
            .Where(al => al.PerformedAt >= startDate && al.PerformedAt <= endDate);

        if (!string.IsNullOrWhiteSpace(performedBy))
            query = query.Where(al => al.PerformedBy == performedBy);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(al => al.Action == action);

        return await query
            .OrderByDescending(al => al.PerformedAt)
            .ToListAsync(cancellationToken);
    }

    /// <summary>
    /// Utility: Serializes an entity's public property values to JSON for audit snapshots.
    /// Excludes navigation properties (ICollection, reference types that are entities).
    /// </summary>
    internal static string SerializeForAudit(object entity)
    {
        var type = entity.GetType();
        var props = type.GetProperties()
            .Where(p => p.CanRead && !p.PropertyType.IsGenericType || p.PropertyType.Name != "ICollection`1")
            .Where(p => !typeof(System.Collections.IEnumerable).IsAssignableFrom(p.PropertyType) || p.PropertyType == typeof(string))
            .ToDictionary(
                p => p.Name,
                p => p.GetValue(entity)?.ToString() ?? "null");

        return JsonSerializer.Serialize(props);
    }
}
