using Microsoft.AspNetCore.Authorization;
using Microsoft.AspNetCore.Mvc;
using Microsoft.EntityFrameworkCore;
using WACAMS.Core.Enums;
using WACAMS.Infrastructure.Data;

namespace WACAMS.Api.Controllers;

/// <summary>
/// Executive Dashboard — real-time KPIs, department heatmap, and leaderboard.
/// Aggregates data across all departments for C-suite visibility.
/// </summary>
[ApiController]
[Route("api/executive-dashboard")]
[Authorize]
public class ExecutiveDashboardController : ControllerBase
{
    private readonly WacamContext _db;
    private readonly ILogger<ExecutiveDashboardController> _logger;

    public ExecutiveDashboardController(WacamContext db, ILogger<ExecutiveDashboardController> logger)
    {
        _db = db;
        _logger = logger;
    }

    /// <summary>
    /// GET /api/executive-dashboard/summary
    /// Returns all 8 real-time KPIs in a single call.
    /// </summary>
    [HttpGet("summary")]
    public async Task<IActionResult> GetSummary(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;

        // ── 1. Employees Present ──
        var employeesPresent = await _db.AttendanceRecords
            .Where(ar => ar.Date == today && ar.Status == AttendanceStatus.Present)
            .Select(ar => ar.EmployeeId)
            .Distinct()
            .CountAsync(ct);

        // ── 2. Current Absenteeism (Absent + AWOL today) ──
        var currentAbsenteeism = await _db.AttendanceRecords
            .Where(ar => ar.Date == today &&
                (ar.Status == AttendanceStatus.Absent || ar.Status == AttendanceStatus.AWOL))
            .Select(ar => ar.EmployeeId)
            .Distinct()
            .CountAsync(ct);

        // ── 3. Attendance % ──
        var totalActiveEmployees = await _db.Employees
            .Where(e => e.Status == EmployeeStatus.Active)
            .CountAsync(ct);

        var attendancePercentage = totalActiveEmployees > 0
            ? Math.Round((decimal)employeesPresent / totalActiveEmployees * 100, 1)
            : 0m;

        // ── 4 & 5. Lost Minutes / Lost Hours (today) ──
        var lostMinutesData = await _db.AttendanceRecords
            .Where(ar => ar.Date == today)
            .GroupBy(_ => 1)
            .Select(g => new
            {
                LateMinutes = g.Sum(ar => ar.LateMinutes),
                EarlyMinutes = g.Sum(ar => ar.EarlyMinutes)
            })
            .FirstOrDefaultAsync(ct);

        var totalLostMinutes = (lostMinutesData?.LateMinutes ?? 0) + (lostMinutesData?.EarlyMinutes ?? 0);
        var totalLostHours = Math.Round(totalLostMinutes / 60.0, 1);

        // ── 6. Open Escalations ──
        var openEscalations = await _db.Escalations
            .Where(e => e.Status == EscalationStatus.Pending || e.Status == EscalationStatus.Accepted)
            .CountAsync(ct);

        // ── 7. Pending Investigations ──
        var pendingInvestigations = await _db.AttendanceExceptions
            .Where(ex => ex.InvestigationStatus == InvestigationStatus.Open ||
                         ex.InvestigationStatus == InvestigationStatus.UnderReview)
            .CountAsync(ct);

        // ── 8. Corrective Actions (warnings issued this month) ──
        var monthStart = new DateTime(today.Year, today.Month, 1);
        var correctiveActions = await _db.WarningLetters
            .Where(w => w.DateIssued >= monthStart)
            .CountAsync(ct);

        return Ok(new
        {
            EmployeesPresent = employeesPresent,
            CurrentAbsenteeism = currentAbsenteeism,
            AttendancePercentage = attendancePercentage,
            LostMinutes = totalLostMinutes,
            LostHours = totalLostHours,
            OpenEscalations = openEscalations,
            PendingInvestigations = pendingInvestigations,
            CorrectiveActions = correctiveActions,
            TotalActiveEmployees = totalActiveEmployees,
            AsOf = DateTime.UtcNow
        });
    }

    /// <summary>
    /// GET /api/executive-dashboard/heatmap
    /// Returns department-level attendance data for the heatmap visualization.
    /// </summary>
    [HttpGet("heatmap")]
    public async Task<IActionResult> GetHeatmap(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;

        var departments = await _db.Departments
            .Select(d => new { d.Id, d.Name, d.Code, d.AttendanceTargetPercentage })
            .ToListAsync(ct);

        var results = new List<object>();

        foreach (var dept in departments)
        {
            var deptEmployeeIds = await _db.Employees
                .Where(e => e.DepartmentId == dept.Id && e.Status == EmployeeStatus.Active)
                .Select(e => e.EmployeeId)
                .ToListAsync(ct);

            var totalEmployees = deptEmployeeIds.Count;
            if (totalEmployees == 0) continue;

            var presentCount = await _db.AttendanceRecords
                .Where(ar => ar.Date == today &&
                    deptEmployeeIds.Contains(ar.EmployeeId) &&
                    (ar.Status == AttendanceStatus.Present || ar.Status == AttendanceStatus.Late))
                .CountAsync(ct);

            var lateCount = await _db.AttendanceRecords
                .Where(ar => ar.Date == today &&
                    deptEmployeeIds.Contains(ar.EmployeeId) &&
                    ar.Status == AttendanceStatus.Late)
                .CountAsync(ct);

            var absentCount = await _db.AttendanceRecords
                .Where(ar => ar.Date == today &&
                    deptEmployeeIds.Contains(ar.EmployeeId) &&
                    (ar.Status == AttendanceStatus.Absent || ar.Status == AttendanceStatus.AWOL))
                .CountAsync(ct);

            var lostMinutes = await _db.AttendanceRecords
                .Where(ar => ar.Date == today &&
                    deptEmployeeIds.Contains(ar.EmployeeId))
                .SumAsync(ar => ar.LateMinutes + ar.EarlyMinutes, ct);

            var attendancePct = Math.Round((decimal)presentCount / totalEmployees * 100, 1);
            var gap = attendancePct - dept.AttendanceTargetPercentage;

            string heatLevel = gap switch
            {
                >= 0 => "green",
                >= -5 => "yellow",
                >= -10 => "orange",
                _ => "red"
            };

            results.Add(new
            {
                departmentId = dept.Id.ToString(),
                departmentName = dept.Name,
                departmentCode = dept.Code,
                totalEmployees,
                presentCount,
                lateCount,
                absentCount,
                lostMinutes,
                attendancePercentage = attendancePct,
                targetPercentage = dept.AttendanceTargetPercentage,
                gapToTarget = Math.Round(gap, 1),
                heatLevel
            });
        }

        return Ok(results.OrderByDescending(r => (decimal)r.GetType().GetProperty("attendancePercentage")!.GetValue(r)!));
    }

    /// <summary>
    /// GET /api/executive-dashboard/leaderboard
    /// Returns department ranking by attendance performance.
    /// </summary>
    [HttpGet("leaderboard")]
    public async Task<IActionResult> GetLeaderboard(CancellationToken ct)
    {
        var today = DateTime.UtcNow.Date;
        var last7Days = today.AddDays(-6);

        var departments = await _db.Departments
            .Select(d => new { d.Id, d.Name, d.Code, d.AttendanceTargetPercentage })
            .ToListAsync(ct);

        var leaderboard = new List<object>();

        foreach (var dept in departments)
        {
            var deptEmployeeIds = await _db.Employees
                .Where(e => e.DepartmentId == dept.Id && e.Status == EmployeeStatus.Active)
                .Select(e => e.EmployeeId)
                .ToListAsync(ct);

            var totalEmployees = deptEmployeeIds.Count;
            if (totalEmployees == 0) continue;

            // 7-day average attendance
            var records = await _db.AttendanceRecords
                .Where(ar => ar.Date >= last7Days && ar.Date <= today &&
                    deptEmployeeIds.Contains(ar.EmployeeId))
                .ToListAsync(ct);

            var totalRecords = records.Count;
            var presentOrLate = records.Count(r =>
                r.Status == AttendanceStatus.Present || r.Status == AttendanceStatus.Late);

            var avgAttendance = totalRecords > 0
                ? Math.Round((decimal)presentOrLate / totalRecords * 100, 1)
                : 0m;

            var totalLostMin = records.Sum(r => r.LateMinutes + r.EarlyMinutes);
            var totalLostHrs = Math.Round(totalLostMin / 60.0, 1);

            var exceptionsCount = await _db.AttendanceExceptions
                .Where(ex => ex.Date >= last7Days && ex.Date <= today &&
                    deptEmployeeIds.Contains(ex.EmployeeId))
                .CountAsync(ct);

            int rank = 0; // Will be assigned after sorting
            string trend = avgAttendance >= dept.AttendanceTargetPercentage ? "up" : "down";

            leaderboard.Add(new
            {
                departmentId = dept.Id.ToString(),
                departmentName = dept.Name,
                departmentCode = dept.Code,
                totalEmployees,
                attendancePercentage = avgAttendance,
                targetPercentage = dept.AttendanceTargetPercentage,
                totalLostHours = totalLostHrs,
                exceptionsCount,
                trend,
                rank
            });
        }

        // Sort by attendance % descending and assign ranks
        var sorted = leaderboard
            .OrderByDescending(x => (decimal)x.GetType().GetProperty("attendancePercentage")!.GetValue(x)!)
            .ToList();

        for (int i = 0; i < sorted.Count; i++)
        {
            var item = sorted[i];
            var rankProp = item.GetType().GetProperty("rank");
            // Since anonymous types are immutable, create new objects with rank
        }

        // Return sorted with index-based rank
        var result = sorted.Select((item, index) =>
        {
            var props = item.GetType().GetProperties();
            var dict = props.ToDictionary(p => p.Name, p => p.GetValue(item));
            dict["rank"] = index + 1;
            return dict;
        });

        return Ok(result);
    }
}
