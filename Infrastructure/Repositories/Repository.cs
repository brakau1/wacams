using System.Linq.Expressions;
using System.Text.Json;
using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Entities;
using WACAMS.Core.Interfaces;
using WACAMS.Infrastructure.Data;

namespace WACAMS.Infrastructure.Repositories;

/// <summary>
/// Generic CRUD implementation of IRepository&lt;T&gt; using EF Core.
/// All mutations automatically capture audit log entries via the DbContext's SaveChanges override.
/// The performedBy and ipAddress are stored on AuditLog entries added after each mutation.
/// </summary>
public class Repository<T> : IRepository<T> where T : class
{
    protected readonly WacamContext _context;
    protected readonly DbSet<T> _dbSet;

    public Repository(WacamContext context)
    {
        _context = context;
        _dbSet = context.Set<T>();
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking().ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<T?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.FindAsync([id], cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<T> AddAsync(
        T entity, string performedBy, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        await _dbSet.AddAsync(entity, cancellationToken);
        await _context.SaveChangesAsync(cancellationToken);

        // Patch the auto-generated audit entry with performedBy/ipAddress
        PatchAuditEntry(entity, "Create", performedBy, ipAddress);

        return entity;
    }

    /// <inheritdoc />
    public virtual async Task<T> UpdateAsync(
        T entity, string performedBy, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        _dbSet.Update(entity);
        await _context.SaveChangesAsync(cancellationToken);

        PatchAuditEntry(entity, "Update", performedBy, ipAddress);

        return entity;
    }

    /// <inheritdoc />
    public virtual async Task<bool> DeleteAsync(
        Guid id, string performedBy, string? ipAddress = null, CancellationToken cancellationToken = default)
    {
        var entity = await _dbSet.FindAsync([id], cancellationToken);
        if (entity == null)
            return false;

        _dbSet.Remove(entity);
        await _context.SaveChangesAsync(cancellationToken);

        PatchAuditEntry(entity, "Delete", performedBy, ipAddress);

        return true;
    }

    /// <inheritdoc />
    public virtual async Task<IEnumerable<T>> GetByExpressionAsync(
        Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.Where(predicate).AsNoTracking().ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public virtual async Task<bool> ExistsAsync(
        Expression<Func<T, bool>> predicate, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AnyAsync(predicate, cancellationToken);
    }

    /// <summary>
    /// Patches the auto-generated audit entry for the given entity with the correct performedBy and ipAddress.
    /// Called immediately after SaveChanges in AddAsync/UpdateAsync/DeleteAsync.
    /// </summary>
    private void PatchAuditEntry(T entity, string action, string performedBy, string? ipAddress)
    {
        var entityType = entity.GetType().Name;
        var entityId = GetBusinessKey(entity);

        // Find the most recent audit entry created by this operation
        var auditEntry = _context.AuditLogs
            .OrderByDescending(a => a.PerformedAt)
            .FirstOrDefault(a => a.EntityType == entityType && a.EntityId == entityId && a.Action == action);

        if (auditEntry != null)
        {
            auditEntry.PerformedBy = performedBy;
            auditEntry.IpAddress = ipAddress;
        }
    }

    private static string GetBusinessKey(T entity)
    {
        return entity switch
        {
            Employee emp => emp.EmployeeId,
            User user => user.EmployeeId,
            AttendanceRecord ar => $"{ar.EmployeeId}_{ar.Date:yyyy-MM-dd}",
            AttendanceException ae => $"{ae.EmployeeId}_{ae.Date:yyyy-MM-dd}",
            ShiftSchedule ss => $"{ss.EmployeeId}_{ss.Date:yyyy-MM-dd}",
            WarningLetter wl => $"{wl.EmployeeId}_{wl.Id}",
            Escalation esc => esc.Id.ToString(),
            Department dept => dept.Id.ToString(),
            AuditLog log => log.Id.ToString(),
            _ => entity.GetType().GetProperty("Id")?.GetValue(entity)?.ToString() ?? string.Empty
        };
    }
}
