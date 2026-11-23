using Microsoft.EntityFrameworkCore;
using Wacam.Api.Controllers;
using Wacam.Api.Data;
using Wacam.Api.DTOs;
using WACAMS.Core.Entities;

namespace Wacam.Api.Services;

/// <summary>
/// Aggregate reports, department rollups, percentage calculations, file export (CSV).
/// </summary>
public class ReportService
{
    private readonly WacamDbContext _context;
    private readonly ILogger<ReportService> _logger;

    public ReportService(WacamDbContext context, ILogger<ReportService> logger)
    {
        _context = context;
        _logger = logger;
    }

    /// <summary>
    /// Get attendance summary for all departments over a date range.
    /// Calculates: Department Attendance %, Employee Attendance %, Lost Production Hours.
    /// </summary>
    public async Task<List<DepartmentAttendanceSummaryDto>> GetDepartmentSummariesAsync(
        DateTime startDate, DateTime endDate)
    {
        var records = await _context.AttendanceRecords
            .Include(r => r.Employee)
                .ThenInclude(e => e!.Department)
            .Where(r => r.Date >= startDate.Date && r.Date <= endDate.Date)
            .ToListAsync();

        var departments = await _context.Departments.ToListAsync();
        var results = new List<DepartmentAttendanceSummaryDto>();

        foreach (var dept in departments)
        {
            var deptRecords = records.Where(r => r.Employee!.DepartmentId == dept.Id).ToList();
            var employees = dept.Employees?.Where(e => e.IsActive).ToList() ?? new List<Employee>();

            if (!employees.Any())
                continue; // Skip empty departments

            var totalEmployees = employees.Count;
            var present = deptRecords.Count(r => r.ClockInTime.HasValue);
            var lateTotal = deptRecords.Count(r => r.Status == WACAMS.Core.Enums.AttendanceStatus.Late);
            var earlyDepartureTotal = deptRecords.Count(r => r.Status == WACAMS.Core.Enums.AttendanceStatus.EarlyDeparture);
            var absenteeismTotal = 0;
            var awolTotal = 0;
            var missingClockInTotal = deptRecords.Count(r => !r.ClockInTime.HasValue);
            var missingClockOutTotal = deptRecords.Count(r => !r.ClockOutTime.HasValue);
            var totalLostMinutes = deptRecords.Sum(r => r.LateMinutes + r.EarlyMinutes);

            // Calculate working days
            var workingDays = 0;
            for (var d = startDate.Date; d <= endDate.Date; d = d.AddDays(1))
            {
                if (DateTimeHelper.IsWeekDay(d))
                    workingDays++;
            }

            var totalPossibleAttendances = totalEmployees * workingDays;
            var actualPct = totalPossibleAttendances > 0
                ? Math.Round((decimal)present / totalPossibleAttendances * 100, 2)
                : 0;

            results.Add(new DepartmentAttendanceSummaryDto
            {
                DepartmentId = dept.Id,
                DepartmentName = dept.Name,
                TargetAttendancePercent = dept.TargetAttendancePercent,
                ActualAttendancePercent = actualPct,
                TotalEmployees = totalEmployees,
                Present = present,
                LateTotal = lateTotal,
                EarlyDepartureTotal = earlyDepartureTotal,
                AbsenteeismTotal = absenteeismTotal,
                AwolTotal = awolTotal,
                MissingClockInTotal = missingClockInTotal,
                MissingClockOutTotal = missingClockOutTotal,
                TotalLostProductionMinutes = totalLostMinutes
            });
        }

        return results.OrderBy(d => d.DepartmentName).ToList();
    }

    /// <summary>
    /// Get individual employee attendance report with full details.
    /// </summary>
    public async Task<IndividualAttendanceReportDto> GetIndividualReportAsync(
        string employeeId, DateTime startDate, DateTime endDate)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.EmployeeId == employeeId);

        if (employee is null)
            throw new NotFoundException($"Employee '{employeeId}' not found.");

        var records = await _context.AttendanceRecords
            .Where(r => r.EmployeeId == employeeId && r.Date >= startDate.Date && r.Date <= endDate.Date)
            .OrderBy(r => r.Date)
            .ToListAsync();

        var workingDays = 0;
        for (var d = startDate.Date; d <= endDate.Date; d = d.AddDays(1))
        {
            if (DateTimeHelper.IsWeekDay(d))
                workingDays++;
        }

        var daysPresent = records.Count(r => r.ClockInTime.HasValue);
        var daysAbsent = workingDays - daysPresent;
        var daysLate = records.Count(r => r.Status == WACAMS.Core.Enums.AttendanceStatus.Late);
        var daysEarly = records.Count(r => r.Status == WACAMS.Core.Enums.AttendanceStatus.EarlyDeparture);
        var totalLateMins = records.Sum(r => r.LateMinutes);
        var totalEarlyMins = records.Sum(r => r.EarlyMinutes);

        var attendancePct = workingDays > 0
            ? Math.Round((decimal)daysPresent / workingDays * 100, 2)
            : 0;

        return new IndividualAttendanceReportDto
        {
            EmployeeId = employee.EmployeeId,
            DisplayName = employee.DisplayName,
            DepartmentName = employee.Department?.Name ?? string.Empty,
            StartDate = startDate,
            EndDate = endDate,
            TotalDays = workingDays,
            DaysPresent = daysPresent,
            DaysAbsent = daysAbsent,
            DaysLate = daysLate,
            DaysEarlyDeparture = daysEarly,
            TotalLateMinutes = totalLateMins,
            TotalEarlyMinutes = totalEarlyMins,
            AttendancePercentage = attendancePct,
            Records = records.Select(r => MapToRecordDto(r, employee)).ToList()
        };
    }

    /// <summary>
    /// Export attendance data to CSV format.
    /// Returns CSV content as a string (caller can stream to response).
    /// </summary>
    public async Task<string> ExportAttendanceCsvAsync(DateTime startDate, DateTime endDate, int? departmentId = null)
    {
        var query = _context.AttendanceRecords
            .Include(r => r.Employee)
                .ThenInclude(e => e!.Department)
            .Where(r => r.Date >= startDate.Date && r.Date <= endDate.Date);

        if (departmentId.HasValue)
            query = query.Where(r => r.Employee!.DepartmentId == departmentId.Value);

        var records = await query.OrderBy(r => r.Employee!.DisplayName).ThenBy(r => r.Date).ToListAsync();

        var csvLines = new List<string>();
        csvLines.Add("EmployeeID,DisplayName,Department,Date,ShiftCode,ClockIn,ClockOut,IsLate,MinutesLate,IsEarlyDeparture,MinutesEarly,TotalHoursWorked");

        foreach (var record in records)
        {
            var emp = record.Employee!;
            csvLines.Add($"{emp.EmployeeId},{EscapeCsv(emp.DisplayName)},{EscapeCsv(emp.Department?.Name ?? "")}," +
                $"{record.Date:yyyy-MM-dd},{record.ShiftCode ?? ""}," +
                $"{record.ClockInTime?.ToString(@"hh\:mm") ?? ""},{record.ClockOutTime?.ToString(@"hh\:mm") ?? ""}," +
                $"{record.Status == WACAMS.Core.Enums.AttendanceStatus.Late},{record.LateMinutes},{record.Status == WACAMS.Core.Enums.AttendanceStatus.EarlyDeparture},{record.EarlyMinutes},{record.TotalHoursWorked}");
        }

        return string.Join("\n", csvLines);
    }

    /// <summary>
    /// Export warnings to CSV.
    /// </summary>
    public async Task<string> ExportWarningsCsvAsync(string? employeeId = null, DateTime? startDate = null, DateTime? endDate = null)
    {
        var query = _context.WarningLetters
            .Include(wl => wl.Employee)
                .ThenInclude(e => e!.Department)
            .Include(wl => wl.Exception);

        if (!string.IsNullOrEmpty(employeeId))
            query = query.Where(wl => wl.EmployeeId == employeeId);

        if (startDate.HasValue)
            query = query.Where(wl => wl.IssueDate >= startDate.Value.Date);

        if (endDate.HasValue)
            query = query.Where(wl => wl.IssueDate <= endDate.Value.Date.AddDays(1).AddTicks(-1));

        var warnings = await query.OrderByDescending(wl => wl.IssueDate).ToListAsync();

        var csvLines = new List<string>();
        csvLines.Add("WarningID,EmployeeID,DisplayName,Department,ExceptionType,WarningType,IssueDate,Description,IssuedBy,SignedFormPath,Acknowledged");

        foreach (var warning in warnings)
        {
            csvLines.Add($"{warning.Id},{warning.EmployeeId},{EscapeCsv(warning.Employee?.DisplayName ?? "")}," +
                $"{EscapeCsv(warning.Employee?.Department?.Name ?? "")},{warning.Exception?.ExceptionType ?? ""}," +
                $"{warning.WarningType},{warning.IssueDate:yyyy-MM-dd HH:mm},{EscapeCsv(warning.Description)}," +
                $"{warning.IssuedBy},{warning.SignedFormPath ?? ""},{warning.Acknowledged}");
        }

        return string.Join("\n", csvLines);
    }

    /// <summary>
    /// Get exceptions count by type for a date range — useful for dashboard KPIs.
    /// </summary>
    public async Task<Dictionary<string, int>> GetExceptionBreakdownAsync(DateTime startDate, DateTime endDate, int? departmentId = null)
    {
        var query = _context.AttendanceExceptions
            .Include(ae => ae.Employee)
                .ThenInclude(e => e!.Department)
            .Where(ae => ae.Date >= startDate.Date && ae.Date <= endDate.Date);

        if (departmentId.HasValue)
            query = query.Where(ae => ae.Employee!.DepartmentId == departmentId.Value);

        var exceptions = await query.ToListAsync();

        return exceptions
            .GroupBy(ae => ae.ExceptionType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToDictionary(x => x.Type, x => x.Count);
    }

    /// <summary>
    /// Get progressive discipline summary for an employee.
    /// Shows how many of each warning type they have on file.
    /// </summary>
    public async Task<ProgressiveDisciplineSummaryDto> GetProgressiveDisciplineSummaryAsync(string employeeId)
    {
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == employeeId);
        if (employee is null)
            throw new NotFoundException($"Employee '{employeeId}' not found.");

        var counts = await _context.WarningLetters
            .Where(wl => wl.EmployeeId == employeeId)
            .GroupBy(wl => wl.WarningType)
            .Select(g => new { Type = g.Key, Count = g.Count() })
            .ToListAsync();

        var summary = new ProgressiveDisciplineSummaryDto
        {
            EmployeeId = employeeId,
            DisplayName = employee.DisplayName,
            DepartmentName = employee.Department?.Name ?? string.Empty,
            TotalWarnings = counts.Sum(c => c.Count),
            CounsellingCount = counts.FirstOrDefault(c => c.Type == "Counselling")?.Count ?? 0,
            VerbalWarningCount = counts.FirstOrDefault(c => c.Type == "VerbalWarning")?.Count ?? 0,
            WrittenWarningCount = counts.FirstOrDefault(c => c.Type == "WrittenWarning")?.Count ?? 0,
            FinalWrittenWarningCount = counts.FirstOrDefault(c => c.Type == "FinalWrittenWarning")?.Count ?? 0
        };

        return summary;
    }

    // ═══════════════════════════════════════════════════════════════════════════════
    // DASHBOARD METHODS — Phase 3 Live Dashboard Integration
    // ═══════════════════════════════════════════════════════════════════════════════

    /// <summary>
    /// Get department KPIs for today — used by general dashboard.
    /// </summary>
    public async Task<List<DashboardDepartmentDto>> GetDepartmentKPIsAsync(DateTime date)
    {
        var departments = await _context.Departments.ToListAsync();
        var results = new List<DashboardDepartmentDto>();

        foreach (var dept in departments)
        {
            var deptEmployees = await _context.Employees
                .Where(e => e.DepartmentId == dept.Id && e.IsActive)
                .Select(e => e.EmployeeId)
                .ToListAsync();

            var totalEmployees = deptEmployees.Count;
            if (totalEmployees == 0) continue;

            var records = await _context.AttendanceRecords
                .Where(r => deptEmployees.Contains(r.EmployeeId) && r.Date == date.Date)
                .ToListAsync();

            var presentCount = records.Count(r => r.ClockInTime.HasValue);
            var lateCount = records.Count(r => r.Status == WACAMS.Core.Enums.AttendanceStatus.Late);
            var absentCount = totalEmployees - presentCount;
            var attendancePct = Math.Round((double)presentCount / totalEmployees * 100, 1);

            results.Add(new DashboardDepartmentDto
            {
                DepartmentId = dept.Id.ToString(),
                DepartmentName = dept.Name,
                AttendancePercentage = attendancePct,
                TotalEmployees = totalEmployees,
                PresentCount = presentCount,
                LateCount = lateCount,
                AbsentCount = absentCount
            });
        }

        return results.OrderByDescending(d => d.AttendancePercentage).ToList();
    }

    /// <summary>
    /// Get 14-day trend data — used by dashboard and manager dashboard.
    /// </summary>
    public async Task<List<DashboardTrendDto>> GetTrendAsync(DateTime startDate, DateTime endDate, Guid? departmentId = null)
    {
        var trends = new List<DashboardTrendDto>();

        for (var d = startDate.Date; d <= endDate.Date; d = d.AddDays(1))
        {
            var query = _context.AttendanceRecords.Where(r => r.Date == d);

            if (departmentId.HasValue)
            {
                var deptEmployees = await _context.Employees
                    .Where(e => e.DepartmentId == departmentId.Value && e.IsActive)
                    .Select(e => e.EmployeeId)
                    .ToListAsync();
                query = query.Where(r => deptEmployees.Contains(r.EmployeeId));
            }

            var records = await query.ToListAsync();
            var total = records.Count;
            var onTime = records.Count(r => r.ClockInTime.HasValue && r.Status != WACAMS.Core.Enums.AttendanceStatus.Late);
            var lateCount = records.Count(r => r.Status == WACAMS.Core.Enums.AttendanceStatus.Late);
            var exceptionCount = await _context.AttendanceExceptions
                .Where(e => e.Date == d)
                .CountAsync();

            var onTimePct = total > 0 ? Math.Round((double)onTime / total * 100, 1) : 0;

            trends.Add(new DashboardTrendDto
            {
                Date = d.ToString("MMM d"),
                OnTimePercentage = onTimePct,
                LateCount = lateCount,
                ExceptionCount = exceptionCount
            });
        }

        return trends;
    }

    /// <summary>
    /// Get top late arrivals for a date range.
    /// </summary>
    public async Task<List<TopLateArrivalDto>> GetTopLateArrivalsAsync(DateTime startDate, DateTime endDate, int topN = 10, Guid? departmentId = null)
    {
        var query = _context.AttendanceRecords
            .Include(r => r.Employee)
                .ThenInclude(e => e!.Department)
            .Where(r => r.Date >= startDate.Date && r.Date <= endDate.Date && r.Status == WACAMS.Core.Enums.AttendanceStatus.Late);

        if (departmentId.HasValue)
            query = query.Where(r => r.Employee!.DepartmentId == departmentId.Value);

        var lateRecords = await query.ToListAsync();

        var grouped = lateRecords
            .GroupBy(r => r.EmployeeId)
            .Select(g => new TopLateArrivalDto
            {
                EmployeeId = g.Key,
                DisplayName = g.First().Employee?.DisplayName ?? g.Key,
                DepartmentName = g.First().Employee?.Department?.Name ?? "Unknown",
                LateCount = g.Count(),
                TotalLateMinutes = g.Sum(r => r.LateMinutes)
            })
            .OrderByDescending(t => t.TotalLateMinutes)
            .Take(topN)
            .ToList();

        return grouped;
    }

    /// <summary>
    /// Get recent exceptions for a date range.
    /// </summary>
    public async Task<List<DashboardExceptionDto>> GetRecentExceptionsAsync(DateTime startDate, DateTime endDate, int topN = 20, Guid? departmentId = null)
    {
        var query = _context.AttendanceExceptions
            .Include(e => e.Employee)
                .ThenInclude(e => e!.Department)
            .Where(e => e.Date >= startDate.Date && e.Date <= endDate.Date);

        if (departmentId.HasValue)
            query = query.Where(e => e.Employee!.DepartmentId == departmentId.Value);

        var exceptions = await query
            .OrderByDescending(e => e.Date)
            .Take(topN)
            .ToListAsync();

        return exceptions.Select(e => new DashboardExceptionDto
        {
            Id = e.Id.ToString(),
            EmployeeId = e.EmployeeId,
            EmployeeName = e.Employee?.DisplayName ?? e.EmployeeId,
            DisplayName = e.Employee?.DisplayName ?? e.EmployeeId,
            DepartmentId = e.Employee?.DepartmentId.ToString() ?? "",
            DepartmentName = e.Employee?.Department?.Name ?? "Unknown",
            ExceptionType = e.ExceptionType,
            Date = e.Date.ToString("yyyy-MM-dd"),
            Description = e.Description ?? "",
            LostProductionMinutes = e.LostProductionMinutes,
            Status = e.Status.ToString(),
            AssignedTo = e.AssignedToManagerId,
            AssignedToName = null,
            InvestigatorNotes = null,
            InvestigatedAt = null,
            InvestigatedBy = null,
            CreatedAt = e.CreatedAt.ToString("o"),
            UpdatedAt = e.UpdatedAt.ToString("o")
        }).ToList();
    }

    /// <summary>
    /// Get department summary for manager dashboard.
    /// </summary>
    public async Task<ManagerSummaryDto> GetDepartmentSummaryAsync(DateTime date, Guid departmentId)
    {
        var dept = await _context.Departments.FirstOrDefaultAsync(d => d.Id == departmentId);
        if (dept == null)
        {
            return new ManagerSummaryDto
            {
                DepartmentId = departmentId.ToString(),
                DepartmentName = "Unknown",
                TotalEmployees = 0,
                PresentCount = 0,
                LateCount = 0,
                AbsentCount = 0,
                AwolCount = 0,
                SickLeaveCount = 0,
                FamilyResponsibilityCount = 0,
                AnnualLeaveCount = 0,
                AttendancePercentage = 0,
                TotalLateMinutes = 0,
                TotalEarlyMinutes = 0,
                TotalLostMinutes = 0
            };
        }

        var deptEmployees = await _context.Employees
            .Where(e => e.DepartmentId == departmentId && e.IsActive)
            .Select(e => e.EmployeeId)
            .ToListAsync();

        var totalEmployees = deptEmployees.Count;
        var records = await _context.AttendanceRecords
            .Where(r => deptEmployees.Contains(r.EmployeeId) && r.Date == date.Date)
            .ToListAsync();

        var presentCount = records.Count(r => r.ClockInTime.HasValue);
        var lateCount = records.Count(r => r.Status == WACAMS.Core.Enums.AttendanceStatus.Late);
        var absentCount = totalEmployees - presentCount;
        var totalLateMinutes = records.Sum(r => r.LateMinutes);
        var totalEarlyMinutes = records.Sum(r => r.EarlyMinutes);
        var attendancePct = totalEmployees > 0 ? Math.Round((double)presentCount / totalEmployees * 100, 1) : 0;

        return new ManagerSummaryDto
        {
            DepartmentId = departmentId.ToString(),
            DepartmentName = dept.Name,
            TotalEmployees = totalEmployees,
            PresentCount = presentCount,
            LateCount = lateCount,
            AbsentCount = absentCount,
            AwolCount = 0,
            SickLeaveCount = 0,
            FamilyResponsibilityCount = 0,
            AnnualLeaveCount = 0,
            AttendancePercentage = attendancePct,
            TotalLateMinutes = totalLateMinutes,
            TotalEarlyMinutes = totalEarlyMinutes,
            TotalLostMinutes = totalLateMinutes + totalEarlyMinutes
        };
    }

    /// <summary>
    /// Get team summary (sub-department or line-level).
    /// </summary>
    public async Task<ManagerSummaryDto> GetTeamSummaryAsync(DateTime date, Guid departmentId)
    {
        // For now, return the same as department summary
        // TODO: Implement sub-department/team breakdown
        return await GetDepartmentSummaryAsync(date, departmentId);
    }

    /// <summary>
    /// Get HR Command Centre KPIs.
    /// </summary>
    public async Task<HRCommandCentreKPIsDto> GetHRCommandCentreKPIsAsync(DateTime today, DateTime monthStart)
    {
        // Plant-wide attendance for today
        var totalActiveEmployees = await _context.Employees.CountAsync(e => e.IsActive);
        var todayRecords = await _context.AttendanceRecords
            .Where(r => r.Date == today.Date)
            .ToListAsync();
        var presentToday = todayRecords.Count(r => r.ClockInTime.HasValue);
        var plantAttendancePct = totalActiveEmployees > 0
            ? Math.Round((double)presentToday / totalActiveEmployees * 100, 1)
            : 0;

        // Lost production hours (month-to-date)
        var monthRecords = await _context.AttendanceRecords
            .Where(r => r.Date >= monthStart.Date && r.Date <= today.Date)
            .ToListAsync();
        var totalLostMinutes = monthRecords.Sum(r => r.LateMinutes + r.EarlyMinutes);
        var lostProductionHours = (int)Math.Round(totalLostMinutes / 60.0);

        // Open warnings
        var openWarnings = await _context.WarningLetters
            .Where(w => w.Status == WarningStatus.Issued || w.Status == WarningStatus.Acknowledged)
            .CountAsync();

        // Final warnings
        var finalWarnings = await _context.WarningLetters
            .Where(w => w.WarningType == "FinalWrittenWarning" &&
                       (w.Status == WarningStatus.Issued || w.Status == WarningStatus.Acknowledged))
            .CountAsync();

        // AWOL cases (month-to-date)
        var awolCases = await _context.AttendanceExceptions
            .Where(e => e.Date >= monthStart.Date && e.Date <= today.Date &&
                       e.ExceptionType == "AWOL")
            .CountAsync();

        // Active escalations
        var escalations = await _context.Escalations
            .Where(e => e.Status == EscalationStatus.Pending || e.Status == EscalationStatus.Accepted)
            .CountAsync();

        // Departments at risk (attendance below 90%)
        var departments = await _context.Departments.ToListAsync();
        var departmentsAtRisk = 0;
        foreach (var dept in departments)
        {
            var deptEmployees = await _context.Employees
                .Where(e => e.DepartmentId == dept.Id && e.IsActive)
                .Select(e => e.EmployeeId)
                .ToListAsync();
            if (!deptEmployees.Any()) continue;

            var deptRecords = await _context.AttendanceRecords
                .Where(r => deptEmployees.Contains(r.EmployeeId) && r.Date == today.Date)
                .ToListAsync();
            var deptPresent = deptRecords.Count(r => r.ClockInTime.HasValue);
            var deptPct = deptEmployees.Count > 0 ? (double)deptPresent / deptEmployees.Count * 100 : 100;
            if (deptPct < 90) departmentsAtRisk++;
        }

        return new HRCommandCentreKPIsDto
        {
            PlantAttendancePercentage = plantAttendancePct,
            LostProductionHours = lostProductionHours,
            OpenWarnings = openWarnings,
            FinalWarnings = finalWarnings,
            AwolCases = awolCases,
            Escalations = escalations,
            DepartmentsAtRisk = departmentsAtRisk
        };
    }

    /// <summary>
    /// Get department rankings with risk levels for HR Command Centre.
    /// </summary>
    public async Task<List<DepartmentRankingDto>> GetDepartmentRankingsAsync(DateTime date)
    {
        var departments = await _context.Departments.ToListAsync();
        var results = new List<DepartmentRankingDto>();

        foreach (var dept in departments)
        {
            var deptEmployees = await _context.Employees
                .Where(e => e.DepartmentId == dept.Id && e.IsActive)
                .Select(e => e.EmployeeId)
                .ToListAsync();

            var totalEmployees = deptEmployees.Count;
            if (totalEmployees == 0) continue;

            var records = await _context.AttendanceRecords
                .Where(r => deptEmployees.Contains(r.EmployeeId) && r.Date == date.Date)
                .ToListAsync();

            var presentCount = records.Count(r => r.ClockInTime.HasValue);
            var lateCount = records.Count(r => r.Status == WACAMS.Core.Enums.AttendanceStatus.Late);
            var absentCount = totalEmployees - presentCount;
            var attendancePct = Math.Round((double)presentCount / totalEmployees * 100, 1);

            var exceptionCount = await _context.AttendanceExceptions
                .Where(e => deptEmployees.Contains(e.EmployeeId) && e.Date >= date.Date.AddDays(-6) && e.Date <= date.Date)
                .CountAsync();

            var riskLevel = attendancePct switch
            {
                >= 95 => "low",
                >= 90 => "medium",
                >= 85 => "high",
                _ => "critical"
            };

            results.Add(new DepartmentRankingDto
            {
                DepartmentId = dept.Id.ToString(),
                DepartmentName = dept.Name,
                AttendancePercentage = attendancePct,
                TotalEmployees = totalEmployees,
                PresentCount = presentCount,
                LateCount = lateCount,
                AbsentCount = absentCount,
                ExceptionCount = exceptionCount,
                RiskLevel = riskLevel
            });
        }

        return results.OrderByDescending(d => d.AttendancePercentage).ToList();
    }

    /// <summary>
    /// Get exception analysis by type for HR Command Centre.
    /// </summary>
    public async Task<List<ExceptionAnalysisDto>> GetExceptionAnalysisAsync(DateTime startDate, DateTime endDate)
    {
        var exceptions = await _context.AttendanceExceptions
            .Where(e => e.Date >= startDate.Date && e.Date <= endDate.Date)
            .ToListAsync();

        var totalCount = exceptions.Count;
        if (totalCount == 0)
            return new List<ExceptionAnalysisDto>();

        var grouped = exceptions
            .GroupBy(e => e.ExceptionType)
            .Select(g => new ExceptionAnalysisDto
            {
                ExceptionType = g.Key,
                Count = g.Count(),
                Percentage = Math.Round((double)g.Count() / totalCount * 100, 1),
                LostMinutes = g.Sum(e => e.LostProductionMinutes)
            })
            .OrderByDescending(e => e.Count)
            .ToList();

        return grouped;
    }

    private static string EscapeCsv(string value)
    {
        if (string.IsNullOrEmpty(value))
            return "";
        if (value.Contains(",") || value.Contains("\"") || value.Contains("\n"))
            return "\"" + value.Replace("\"", "\"\"") + "\"";
        return value;
    }

    private static DTOs.AttendanceRecordDto MapToRecordDto(AttendanceRecord record, Employee employee)
    {
        return new DTOs.AttendanceRecordDto
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            DisplayName = employee.DisplayName,
            Date = record.Date,
            ShiftCode = record.ShiftCode,
            ClockIn = record.ClockInTime,
            ClockOut = record.ClockOutTime,
            IsLate = record.Status == WACAMS.Core.Enums.AttendanceStatus.Late,
            MinutesLate = record.LateMinutes,
            IsEarlyDeparture = record.Status == WACAMS.Core.Enums.AttendanceStatus.EarlyDeparture,
            MinutesEarly = record.EarlyMinutes,
            TotalHoursWorked = record.TotalHoursWorked,
            CreatedAt = record.CreatedAt
        };
    }
}

public record ProgressiveDisciplineSummaryDto
{
    public string EmployeeId { get; init; } = string.Empty;
    public string DisplayName { get; init; } = string.Empty;
    public string DepartmentName { get; init; } = string.Empty;
    public int TotalWarnings { get; init; }
    public int CounsellingCount { get; init; }
    public int VerbalWarningCount { get; init; }
    public int WrittenWarningCount { get; init; }
    public int FinalWrittenWarningCount { get; init; }
}
