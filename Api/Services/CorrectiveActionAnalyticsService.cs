using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;

namespace WACAMS.Api.Services;

/// <summary>
/// Corrective action analytics — disciplinary trend analysis, threshold tracking, and warning pattern detection.
/// </summary>
public class CorrectiveActionAnalyticsService
{
    private readonly WacamDbContext _context;
    private readonly ILogger<CorrectiveActionAnalyticsService> _logger;

    public CorrectiveActionAnalyticsService(WacamDbContext context, ILogger<CorrectiveActionAnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<CorrectiveActionAnalyticsDto> GetAnalyticsAsync(DateTime startDate, DateTime endDate, Guid? departmentId = null)
    {
        _logger.LogDebug("Calculating corrective action analytics for {StartDate} to {EndDate}", startDate, endDate);

        var warnings = await _context.WarningLetters
            .Include(w => w.Employee)
                .ThenInclude(e => e!.Department)
            .Where(w => w.DateIssued >= startDate && w.DateIssued <= endDate)
            .ToListAsync();

        if (departmentId.HasValue)
            warnings = warnings.Where(w => w.Employee!.DepartmentId == departmentId.Value).ToList();

        var correctiveActions = await _context.CorrectiveActions
            .Include(ca => ca.Employee)
                .ThenInclude(e => e!.Department)
            .Where(ca => ca.CreatedAt >= startDate && ca.CreatedAt <= endDate)
            .ToListAsync();

        var totalWarnings = warnings.Count;
        var totalCorrectiveActions = correctiveActions.Count;
        var activeActions = correctiveActions.Count(ca => ca.IsActive);
        var expiredActions = correctiveActions.Count(ca => !ca.IsActive && ca.Status == "Expired");

        // Warning type breakdown
        var warningTypeBreakdown = warnings
            .GroupBy(w => w.Type)
            .Select(g => new WarningTypeBreakdownDto
            {
                WarningType = g.Key.ToString(),
                Count = g.Count(),
                Percentage = totalWarnings > 0 ? Math.Round((double)g.Count() / totalWarnings * 100, 2) : 0
            })
            .OrderByDescending(w => w.Count)
            .ToList();

        // Department breakdown
        var departmentBreakdown = warnings
            .GroupBy(w => w.Employee?.DepartmentId)
            .Select(g => new DepartmentWarningDto
            {
                DepartmentId = g.Key,
                DepartmentName = g.First().Employee?.Department?.Name ?? "Unknown",
                WarningCount = g.Count(),
                AvgEscalationLevel = g.Average(w => w.EscalationLevel)
            })
            .OrderByDescending(d => d.WarningCount)
            .ToList();

        // Escalation level distribution
        var escalationDistribution = warnings
            .GroupBy(w => w.EscalationLevel)
            .Select(g => new EscalationLevelDto
            {
                Level = g.Key,
                Count = g.Count()
            })
            .OrderBy(e => e.Level)
            .ToList();

        // Repeat offenders
        var repeatOffenders = warnings
            .GroupBy(w => w.EmployeeId)
            .Where(g => g.Count() > 1)
            .Select(g => new RepeatOffenderDto
            {
                EmployeeId = g.Key,
                EmployeeName = g.First().Employee?.DisplayName ?? "Unknown",
                DepartmentName = g.First().Employee?.Department?.Name ?? "Unknown",
                WarningCount = g.Count(),
                MaxEscalationLevel = g.Max(w => w.EscalationLevel),
                LastWarningDate = g.Max(w => w.DateIssued)
            })
            .OrderByDescending(o => o.WarningCount)
            .Take(20)
            .ToList();

        return new CorrectiveActionAnalyticsDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalWarnings = totalWarnings,
            TotalCorrectiveActions = totalCorrectiveActions,
            ActiveActions = activeActions,
            ExpiredActions = expiredActions,
            WarningTypeBreakdown = warningTypeBreakdown,
            DepartmentBreakdown = departmentBreakdown,
            EscalationDistribution = escalationDistribution,
            RepeatOffenders = repeatOffenders
        };
    }
}

public class CorrectiveActionAnalyticsDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalWarnings { get; set; }
    public int TotalCorrectiveActions { get; set; }
    public int ActiveActions { get; set; }
    public int ExpiredActions { get; set; }
    public List<WarningTypeBreakdownDto> WarningTypeBreakdown { get; set; } = new();
    public List<DepartmentWarningDto> DepartmentBreakdown { get; set; } = new();
    public List<EscalationLevelDto> EscalationDistribution { get; set; } = new();
    public List<RepeatOffenderDto> RepeatOffenders { get; set; } = new();
}

public class WarningTypeBreakdownDto
{
    public string WarningType { get; set; } = string.Empty;
    public int Count { get; set; }
    public double Percentage { get; set; }
}

public class DepartmentWarningDto
{
    public Guid? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int WarningCount { get; set; }
    public double AvgEscalationLevel { get; set; }
}

public class EscalationLevelDto
{
    public int Level { get; set; }
    public int Count { get; set; }
}

public class RepeatOffenderDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public int WarningCount { get; set; }
    public int MaxEscalationLevel { get; set; }
    public DateTime LastWarningDate { get; set; }
}
