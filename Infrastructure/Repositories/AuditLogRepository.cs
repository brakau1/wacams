using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Entities;
using WACAMS.Core.Interfaces;
using WACAMS.Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// Specialized repository for AuditLog operations.
/// Provides methods for logging changes and retrieving audit trails with pagination support.
/// </summary>
public class AuditLogRepository : Repository<AuditLog>, IAuditLogService
{
    public AuditLogRepository(WacamContext context) : base(context) { }

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

        await _dbSet.AddAsync(auditEntry, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<AuditLog>> GetAuditTrailAsync(
        string entityType, string entityId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
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
        var query = _dbSet.AsNoTracking()
            .Where(al => al.PerformedAt >= startDate && al.PerformedAt <= endDate);

        if (!string.IsNullOrWhiteSpace(performedBy))
            query = query.Where(al => al.PerformedBy == performedBy);

        if (!string.IsNullOrWhiteSpace(action))
            query = query.Where(al => al.Action == action);

        return await query
            .OrderByDescending(al => al.PerformedAt)
            .ToListAsync(cancellationToken);
    }
}
