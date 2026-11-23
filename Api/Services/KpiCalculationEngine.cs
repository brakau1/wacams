using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Entities;
using WACAMS.Core.Enums;
using WACAMS.Core.Interfaces;

namespace Wacam.Api.Services;

/// <summary>
/// Engine for calculating attendance KPIs across different time periods and scopes
/// </summary>
public class KpiCalculationEngine
{
    private readonly Wacam.Api.Data.WacamDbContext _context;
    private readonly ILogger<KpiCalculationEngine> _logger;

    public KpiCalculationEngine(
        Wacam.Api.Data.WacamDbContext context,
        ILogger<KpiCalculationEngine> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Calculate attendance percentage for a given period and scope
    /// </summary>
    public async Task<decimal> CalculateAttendancePercentageAsync(
        DateTime startDate,
        DateTime endDate,
        string? departmentId = null,
        string? managerId = null)
    {
        try
        {
            var query = _context.AttendanceRecords
                .Where(ar => ar.Date >= startDate.Date && ar.Date <= endDate.Date);

            if (!string.IsNullOrEmpty(departmentId))
            {
                query = query.Where(ar => ar.DepartmentId == departmentId);
            }

            if (!string.IsNullOrEmpty(managerId))
            {
                query = query.Where(ar => ar.Employee.ManagerId == managerId);
            }

            var totalRecords = await query.CountAsync();
            if (totalRecords == 0) return 0;

            var presentRecords = await query
                .Where(ar => ar.Status == AttendanceStatus.Present || 
                            ar.Status == AttendanceStatus.Late || 
                            ar.Status == AttendanceStatus.EarlyDeparture)
                .CountAsync();

            return Math.Round((decimal)presentRecords / totalRecords * 100, 2);
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, "Error calculating attendance percentage");
            throw;
        }
    }

    /// <summary>
    /// Calculate present employees count
    /// </summary>
    public async Task<int> CalculatePresentEmployeesAsync(
        DateTime date,
        string? departmentId = null)
    {
        var query = _context.AttendanceRecords
            .Where(ar => ar.Date == date.Date)
            .Where(ar => ar.Status == AttendanceStatus.Present || 
                        ar.Status == AttendanceStatus.Late || 
                        ar.Status == AttendanceStatus.EarlyDeparture);

        if (!string.IsNullOrEmpty(departmentId))
        {
            query = query.Where(ar => ar.DepartmentId == departmentId);
        }

        return await query.Select(ar => ar.EmployeeId).Distinct().CountAsync();
    }

    /// <summary>
    /// Calculate absent employees count
    /// </summary>
    public async Task<int> CalculateAbsentEmployeesAsync(
        DateTime date,
        string? departmentId = null)
    {
        var query = _context.AttendanceRecords
            .Where(ar => ar.Date == date.Date)
            .Where(ar => ar.Status == AttendanceStatus.Absent || 
                        ar.Status == AttendanceStatus.AWOL);

        if (!string.IsNullOrEmpty(departmentId))
        {
            query = query.Where(ar => ar.DepartmentId == departmentId);
        }

        return await query.Select(ar => ar.EmployeeId).Distinct().CountAsync();
    }

    /// <summary>
    /// Calculate late arrival percentage
    /// </summary>
    public async Task<decimal> CalculateLateArrivalPercentageAsync(
        DateTime startDate,
        DateTime endDate,
        string? departmentId = null)
    {
        var query = _context.AttendanceRecords
            .Where(ar => ar.Date >= startDate.Date && ar.Date <= endDate.Date);

        if (!string.IsNullOrEmpty(departmentId))
        {
            query = query.Where(ar => ar.DepartmentId == departmentId);
        }

        var totalRecords = await query.CountAsync();
        if (totalRecords == 0) return 0;

        var lateRecords = await query
            .Where(ar => ar.Status == AttendanceStatus.Late)
            .CountAsync();

        return Math.Round((decimal)lateRecords / totalRecords * 100, 2);
    }

    /// <summary>
    /// Calculate early departure percentage
    /// </summary>
    public async Task<decimal> CalculateEarlyDeparturePercentageAsync(
        DateTime startDate,
        DateTime endDate,
        string? departmentId = null)
    {
        var query = _context.AttendanceRecords
            .Where(ar => ar.Date >= startDate.Date && ar.Date <= endDate.Date);

        if (!string.IsNullOrEmpty(departmentId))
        {
            query = query.Where(ar => ar.DepartmentId == departmentId);
        }

        var totalRecords = await query.CountAsync();
        if (totalRecords == 0) return 0;

        var earlyRecords = await query
            .Where(ar => ar.Status == AttendanceStatus.EarlyDeparture)
            .CountAsync();

        return Math.Round((decimal)earlyRecords / totalRecords * 100, 2);
    }

    /// <summary>
    /// Calculate unplanned absenteeism percentage
    /// </summary>
    public async Task<decimal> CalculateUnplannedAbsenteeismPercentageAsync(
        DateTime startDate,
        DateTime endDate,
        string? departmentId = null)
    {
        var query = _context.AttendanceRecords
            .Where(ar => ar.Date >= startDate.Date && ar.Date <= endDate.Date);

        if (!string.IsNullOrEmpty(departmentId))
        {
            query = query.Where(ar => ar.DepartmentId == departmentId);
        }

        var totalRecords = await query.CountAsync();
        if (totalRecords == 0) return 0;

        var unplannedRecords = await query
            .Where(ar => ar.Status == AttendanceStatus.Absent || 
                        ar.Status == AttendanceStatus.AWOL)
            .CountAsync();

        return Math.Round((decimal)unplannedRecords / totalRecords * 100, 2);
    }

    /// <summary>
    /// Calculate exception rate percentage
    /// </summary>
    public async Task<decimal> CalculateExceptionRateAsync(
        DateTime startDate,
        DateTime endDate,
        string? departmentId = null)
    {
        var totalAttendance = await _context.AttendanceRecords
            .Where(ar => ar.Date >= startDate.Date && ar.Date <= endDate.Date)
            .CountAsync();

        if (totalAttendance == 0) return 0;

        var query = _context.AttendanceExceptions
            .Where(e => e.Date >= startDate.Date && e.Date <= endDate.Date);

        if (!string.IsNullOrEmpty(departmentId))
        {
            query = query.Where(e => e.DepartmentId == departmentId);
        }

        var exceptionCount = await query.CountAsync();

        return Math.Round((decimal)exceptionCount / totalAttendance * 100, 2);
    }

    /// <summary>
    /// Calculate investigation resolution percentage
    /// </summary>
    public async Task<decimal> CalculateInvestigationResolutionPercentageAsync(
        DateTime startDate,
        DateTime endDate)
    {
        var totalInvestigations = await _context.AttendanceExceptions
            .Where(e => e.Date >= startDate.Date && e.Date <= endDate.Date)
            .Where(e => e.Status == ExceptionStatus.Investigated || 
                       e.Status == ExceptionStatus.Closed)
            .CountAsync();

        if (totalInvestigations == 0) return 0;

        var resolvedInvestigations = await _context.AttendanceExceptions
            .Where(e => e.Date >= startDate.Date && e.Date <= endDate.Date)
            .Where(e => e.Status == ExceptionStatus.Closed)
            .CountAsync();

        return Math.Round((decimal)resolvedInvestigations / totalInvestigations * 100, 2);
    }

    /// <summary>
    /// Calculate lost production minutes
    /// </summary>
    public async Task<int> CalculateLostProductionMinutesAsync(
        DateTime startDate,
        DateTime endDate,
        string? departmentId = null)
    {
        var query = _context.AttendanceExceptions
            .Where(e => e.Date >= startDate.Date && e.Date <= endDate.Date);

        if (!string.IsNullOrEmpty(departmentId))
        {
            query = query.Where(e => e.DepartmentId == departmentId);
        }

        var lostMinutes = await query
            .SumAsync(e => (int?)e.LostProductionMinutes) ?? 0;

        return lostMinutes;
    }

    /// <summary>
    /// Calculate lost production hours
    /// </summary>
    public async Task<decimal> CalculateLostProductionHoursAsync(
        DateTime startDate,
        DateTime endDate,
        string? departmentId = null)
    {
        var minutes = await CalculateLostProductionMinutesAsync(startDate, endDate, departmentId);
        return Math.Round((decimal)minutes / 60, 2);
    }

    /// <summary>
    /// Calculate average investigation duration in hours
    /// </summary>
    public async Task<decimal> CalculateAverageInvestigationDurationAsync(
        DateTime startDate,
        DateTime endDate)
    {
        var investigations = await _context.AttendanceExceptions
            .Where(e => e.Date >= startDate.Date && e.Date <= endDate.Date)
            .Where(e => e.Status == ExceptionStatus.Closed && 
                       e.InvestigatedAt.HasValue)
            .Select(e => new { e.CreatedAt, e.InvestigatedAt })
            .ToListAsync();

        if (!investigations.Any()) return 0;

        var totalHours = investigations
            .Average(i => (i.InvestigatedAt!.Value - i.CreatedAt).TotalHours);

        return Math.Round((decimal)totalHours, 2);
    }

    /// <summary>
    /// Calculate repeat offender count
    /// </summary>
    public async Task<int> CalculateRepeatOffenderCountAsync(
        DateTime startDate,
        DateTime endDate,
        int minimumOffences = 3)
    {
        var offenders = await _context.AttendanceExceptions
            .Where(e => e.Date >= startDate.Date && e.Date <= endDate.Date)
            .GroupBy(e => e.EmployeeId)
            .Select(g => new { EmployeeId = g.Key, Count = g.Count() })
            .Where(g => g.Count >= minimumOffences)
            .CountAsync();

        return offenders;
    }

    /// <summary>
    /// Get open investigations count
    /// </summary>
    public async Task<int> GetOpenInvestigationsCountAsync(string? departmentId = null)
    {
        var query = _context.AttendanceExceptions
            .Where(e => e.Status == ExceptionStatus.Open || 
                       e.Status == ExceptionStatus.UnderReview);

        if (!string.IsNullOrEmpty(departmentId))
        {
            query = query.Where(e => e.DepartmentId == departmentId);
        }

        return await query.CountAsync();
    }

    /// <summary>
    /// Get open corrective actions count
    /// </summary>
    public async Task<int> GetOpenCorrectiveActionsCountAsync(string? departmentId = null)
    {
        var query = _context.CorrectiveActions
            .Where(ca => ca.Status != nameof(DisciplinaryActionResult.Dismissed) && 
                        ca.Status != nameof(DisciplinaryActionResult.Completed));

        if (!string.IsNullOrEmpty(departmentId))
        {
            query = query.Where(ca => ca.Employee.DepartmentId == departmentId);
        }

        return await query.CountAsync();
    }

    /// <summary>
    /// Get open escalations count
    /// </summary>
    public async Task<int> GetOpenEscalationsCountAsync(string? departmentId = null)
    {
        var query = _context.Escalations
            .Where(e => e.Status == EscalationStatus.Pending || 
                       e.Status == EscalationStatus.Accepted || 
                       e.Status == EscalationStatus.ActionTaken);

        if (!string.IsNullOrEmpty(departmentId))
        {
            query = query.Where(e => e.Employee!.DepartmentId == departmentId);
        }

        return await query.CountAsync();
    }
}
