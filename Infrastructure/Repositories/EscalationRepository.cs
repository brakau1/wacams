using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Enums;
using WACAMS.Core.Entities;
using WACAMS.Core.Escalations.Interfaces;
using WACAMS.Infrastructure.Data;

namespace WACAMS.Infrastructure.Repositories;

/// <summary>
/// Repository implementation for escalation data access
/// </summary>
public class EscalationRepository : IEscalationRepository
{
    private readonly WacamContext _context;

    public EscalationRepository(WacamContext context)
    {
        _context = context;
    }

    public async Task<Escalation?> GetByIdAsync(Guid id)
    {
        return await _context.Escalations
            .Include(e => e.History)
            .Include(e => e.Exception)
            .FirstOrDefaultAsync(e => e.Id == id);
    }

    public async Task<IEnumerable<Escalation>> GetByExceptionIdAsync(Guid exceptionId)
    {
        return await _context.Escalations
            .Include(e => e.History)
            .Where(e => e.ExceptionId == exceptionId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Escalation>> GetByEmployeeIdAsync(string employeeId)
    {
        return await _context.Escalations
            .Include(e => e.History)
            .Where(e => e.EmployeeId == employeeId)
            .OrderByDescending(e => e.CreatedAt)
            .ToListAsync();
    }

    public async Task<IEnumerable<Escalation>> GetPendingEscalationsAsync()
    {
        return await _context.Escalations
            .Include(e => e.History)
            .Where(e => e.Status == EscalationStatus.Pending || 
                        e.Status == EscalationStatus.Accepted)
            .OrderBy(e => e.NextEscalationTime)
            .ToListAsync();
    }

    public async Task<IEnumerable<Escalation>> GetDueForEscalationAsync(DateTime cutoffTime)
    {
        return await _context.Escalations
            .Include(e => e.History)
            .Where(e => (e.Status == EscalationStatus.Pending || 
                        e.Status == EscalationStatus.Accepted) &&
                        e.NextEscalationTime <= cutoffTime &&
                        !e.DocumentUploaded)
            .ToListAsync();
    }

    public async Task<Escalation> CreateAsync(Escalation escalation)
    {
        _context.Escalations.Add(escalation);
        await _context.SaveChangesAsync();
        return escalation;
    }

    public async Task<Escalation> UpdateAsync(Escalation escalation)
    {
        _context.Escalations.Update(escalation);
        await _context.SaveChangesAsync();
        return escalation;
    }

    public async Task<bool> DeleteAsync(Guid id)
    {
        var escalation = await _context.Escalations.FindAsync(id);
        if (escalation == null)
        {
            return false;
        }

        _context.Escalations.Remove(escalation);
        await _context.SaveChangesAsync();
        return true;
    }
}
