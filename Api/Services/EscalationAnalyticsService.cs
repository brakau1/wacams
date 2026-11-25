using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;
using WACAMS.Core.Enums;

namespace WACAMS.Api.Services;

/// <summary>
/// Escalation analytics — escalation trend analysis, resolution time tracking, and manager response metrics.
/// </summary>
public class EscalationAnalyticsService
{
    private readonly WacamDbContext _context;
    private readonly ILogger<EscalationAnalyticsService> _logger;

    public EscalationAnalyticsService(WacamDbContext context, ILogger<EscalationAnalyticsService> logger)
    {
        _context = context;
        _logger = logger;
    }

    public async Task<EscalationAnalyticsDto> GetAnalyticsAsync(DateTime startDate, DateTime endDate, Guid? departmentId = null)
    {
        _logger.LogDebug("Calculating escalation analytics for {StartDate} to {EndDate}", startDate, endDate);

        var query = _context.Escalations
            .Include(e => e.Employee)
                .ThenInclude(e => e!.Department)
            .Where(e => e.CreatedAt >= startDate && e.CreatedAt <= endDate);

        if (departmentId.HasValue)
            query = query.Where(e => e.Employee!.DepartmentId == departmentId.Value);

        var escalations = await query.ToListAsync();

        var totalEscalations = escalations.Count;
        var openEscalations = escalations.Count(e => e.Status == EscalationStatus.Open);
        var resolvedEscalations = escalations.Count(e => e.Status == EscalationStatus.Resolved);
        var escalatedFurther = escalations.Count(e => e.Status == EscalationStatus.Escalated);

        // Average resolution time
        var resolvedWithTime = escalations
            .Where(e => e.Status == EscalationStatus.Resolved && e.ResolvedAt.HasValue)
            .ToList();

        var avgResolutionHours = resolvedWithTime.Count > 0
            ? resolvedWithTime.Average(e => (e.ResolvedAt!.Value - e.CreatedAt).TotalHours)
            : 0;

        // Escalation level distribution
        var levelDistribution = escalations
            .GroupBy(e => e.Level)
            .Select(g => new EscalationLevelStatsDto
            {
                Level = g.Key,
                Count = g.Count(),
                AvgResolutionHours = g.Where(e => e.Status == EscalationStatus.Resolved && e.ResolvedAt.HasValue)
                    .DefaultIfEmpty()
                    .Average(e => e?.ResolvedAt.HasValue == true ? (e.ResolvedAt.Value - e.CreatedAt).TotalHours : 0)
            })
            .OrderBy(l => l.Level)
            .ToList();

        // Department breakdown
        var departmentBreakdown = escalations
            .GroupBy(e => e.Employee?.DepartmentId)
            .Select(g => new DepartmentEscalationDto
            {
                DepartmentId = g.Key,
                DepartmentName = g.First().Employee?.Department?.Name ?? "Unknown",
                TotalEscalations = g.Count(),
                OpenCount = g.Count(e => e.Status == EscalationStatus.Open),
                AvgLevel = g.Average(e => e.Level)
            })
            .OrderByDescending(d => d.TotalEscalations)
            .ToList();

        // Manager response time
        var managerResponseTimes = escalations
            .Where(e => e.AssignedToManagerId != null && e.ManagerRespondedAt.HasValue)
            .GroupBy(e => e.AssignedToManagerId)
            .Select(g => new ManagerResponseDto
            {
                ManagerId = g.Key ?? "",
                TotalAssigned = g.Count(),
                AvgResponseHours = g.Average(e => (e.ManagerRespondedAt!.Value - e.EscalatedAt).TotalHours),
                Within24hCount = g.Count(e => (e.ManagerRespondedAt!.Value - e.EscalatedAt).TotalHours <= 24)
            })
            .OrderBy(m => m.AvgResponseHours)
            .ToList();

        // Overdue escalations (not responded within 24h)
        var now = DateTime.UtcNow;
        var overdueEscalations = escalations
            .Where(e => e.Status == EscalationStatus.Open && (now - e.EscalatedAt).TotalHours > 24)
            .Select(e => new OverdueEscalationDto
            {
                EscalationId = e.Id,
                EmployeeId = e.EmployeeId,
                EmployeeName = e.Employee?.FullName ?? "Unknown",
                Level = e.Level,
                HoursOverdue = (int)(now - e.EscalatedAt).TotalHours - 24
            })
            .OrderByDescending(o => o.HoursOverdue)
            .ToList();

        return new EscalationAnalyticsDto
        {
            StartDate = startDate,
            EndDate = endDate,
            TotalEscalations = totalEscalations,
            OpenEscalations = openEscalations,
            ResolvedEscalations = resolvedEscalations,
            EscalatedFurther = escalatedFurther,
            AvgResolutionHours = Math.Round(avgResolutionHours, 2),
            LevelDistribution = levelDistribution,
            DepartmentBreakdown = departmentBreakdown,
            ManagerResponseTimes = managerResponseTimes,
            OverdueEscalations = overdueEscalations
        };
    }
}

public class EscalationAnalyticsDto
{
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalEscalations { get; set; }
    public int OpenEscalations { get; set; }
    public int ResolvedEscalations { get; set; }
    public int EscalatedFurther { get; set; }
    public double AvgResolutionHours { get; set; }
    public List<EscalationLevelStatsDto> LevelDistribution { get; set; } = new();
    public List<DepartmentEscalationDto> DepartmentBreakdown { get; set; } = new();
    public List<ManagerResponseDto> ManagerResponseTimes { get; set; } = new();
    public List<OverdueEscalationDto> OverdueEscalations { get; set; } = new();
}

public class EscalationLevelStatsDto
{
    public int Level { get; set; }
    public int Count { get; set; }
    public double AvgResolutionHours { get; set; }
}

public class DepartmentEscalationDto
{
    public Guid? DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalEscalations { get; set; }
    public int OpenCount { get; set; }
    public double AvgLevel { get; set; }
}

public class ManagerResponseDto
{
    public string ManagerId { get; set; } = string.Empty;
    public int TotalAssigned { get; set; }
    public double AvgResponseHours { get; set; }
    public int Within24hCount { get; set; }
}

public class OverdueEscalationDto
{
    public int EscalationId { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string EmployeeName { get; set; } = string.Empty;
    public int Level { get; set; }
    public int HoursOverdue { get; set; }
}
