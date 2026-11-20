using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Constants;
using WACAMS.Core.Entities;
using WACAMS.Core.Enums;
using WACAMS.Core.Interfaces;
using WACAMS.Infrastructure.Data;

namespace Infrastructure.Repositories;

/// <summary>
/// Specialized repository for WarningLetter and Escalation operations.
/// Provides methods for retrieving warnings by employee, finding overdue reviews,
/// and managing escalation chains.
/// </summary>
public class WarningRepository : Repository<WarningLetter>, IWarningRepository
{
    public WarningRepository(WacamContext context) : base(context) { }

    /// <inheritdoc />
    public override async Task<IEnumerable<WarningLetter>> GetAllAsync(CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Include(wl => wl.Employee)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public override async Task<WarningLetter?> GetByIdAsync(Guid id, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Include(wl => wl.Employee)
            .FirstOrDefaultAsync(wl => wl.Id == id, cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WarningLetter>> GetByEmployeeAsync(
        string employeeId, CancellationToken cancellationToken = default)
    {
        return await _dbSet.AsNoTracking()
            .Where(wl => wl.EmployeeId == employeeId)
            .OrderByDescending(wl => wl.DateIssued)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<WarningLetter>> GetOverdueReviewsAsync(CancellationToken cancellationToken = default)
    {
        var now = DateTime.UtcNow.Date;
        return await _dbSet.AsNoTracking()
            .Where(wl => wl.NextReviewDate.HasValue && wl.NextReviewDate.Value <= now)
            .OrderBy(wl => wl.NextReviewDate)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<IEnumerable<Escalation>> GetEscalationsAsync(
        Guid warningLetterId, CancellationToken cancellationToken = default)
    {
        return await _context.Escalations.AsNoTracking()
            .Where(e => e.WarningLetterId == warningLetterId)
            .OrderBy(e => e.Level)
            .ToListAsync(cancellationToken);
    }

    /// <inheritdoc />
    public async Task<Dictionary<WarningType, int>> CountActiveWarningsAsync(
        string employeeId, CancellationToken cancellationToken = default)
    {
        var cutoffDate = DateTime.UtcNow.Date.AddDays(-Core.Constants.WarningThresholds.ActiveWarningValidityDays);

        var warnings = await _dbSet.AsNoTracking()
            .Where(wl => wl.EmployeeId == employeeId && wl.DateIssued >= cutoffDate)
            .GroupBy(wl => wl.Type)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync(cancellationToken);

        return warnings.ToDictionary(w => w.Type, w => w.Count);
    }
}
