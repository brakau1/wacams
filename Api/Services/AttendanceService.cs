using Microsoft.EntityFrameworkCore;
using Wacam.Api.Data;
using Wacam.Api.DTOs;
using WACAMS.Core.Entities;
using Wacam.Api.Utils;

namespace Wacam.Api.Services;

/// <summary>
/// Clock in/out logic, late/early calculation, auto-exception detection.
/// </summary>
public class AttendanceService
{
    private readonly WacamDbContext _context;
    private readonly AuditService _auditService;
    private readonly ExceptionService _exceptionService;
    private readonly DepartmentAuthorizationService _departmentAuth;
    private readonly ILogger<AttendanceService> _logger;

    public AttendanceService(
        WacamDbContext context,
        AuditService auditService,
        ExceptionService exceptionService,
        DepartmentAuthorizationService departmentAuth,
        ILogger<AttendanceService> logger)
    {
        _context = context;
        _auditService = auditService;
        _exceptionService = exceptionService;
        _departmentAuth = departmentAuth;
        _logger = logger;
    }

    /// <summary>
    /// Clock in for an employee on a specific date.
    /// Validates shift timing and calculates lateness automatically.
    /// </summary>
    public async Task<AttendanceRecordDto> ClockInAsync(ClockInRequest request)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId && e.IsActive);

        if (employee is null)
            throw new NotFoundException($"Employee '{request.EmployeeId}' not found or inactive.");

        var now = DateTimeHelper.NowInSouthAfrica();
        var clockInTime = now.TimeOfDay;
        var date = request.Date.Date;

        // Determine shift based on date and time
        var shiftName = DateTimeHelper.GetShiftName(now);
        var expectedStart = DateTimeHelper.GetShiftStartTime(shiftName);

        // Check if already clocked in today
        var existingRecord = await _context.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == employee.EmployeeId && r.Date.Date == date);

        if (existingRecord != null && existingRecord.ClockIn.HasValue)
            throw new ConflictException("Already clocked in for this date.");

        var minutesLate = DateTimeHelper.CalculateMinutesLate(clockInTime, expectedStart);
        var isLate = minutesLate > 0;

        // Apply grace period from settings
        var gracePeriod = GetSettingValueAsInt("LateGracePeriodMinutes", 5);
        if (minutesLate <= gracePeriod)
        {
            isLate = false;
            minutesLate = 0;
        }

        var record = new AttendanceRecord
        {
            EmployeeId = employee.EmployeeId,
            Date = date,
            ShiftCode = GetShiftCodeForShiftName(shiftName),
            ClockIn = clockInTime,
            ExpectedClockIn = expectedStart,
            IsLate = isLate,
            MinutesLate = minutesLate,
            CreatedAt = DateTime.UtcNow
        };

        _context.AttendanceRecords.Add(record);
        await _context.SaveChangesAsync();

        // Auto-detect: if very late (>30 min), create LateArrival exception
        if (isLate && minutesLate > gracePeriod + 30)
        {
            try
            {
                await _exceptionService.CreateAutoExceptionAsync(new CreateExceptionRequest
                {
                    EmployeeId = employee.EmployeeId,
                    Date = date,
                    ExceptionType = "LateArrival",
                    ExpectedClockIn = expectedStart,
                    ActualClockIn = clockInTime,
                    MinutesLate = minutesLate,
                    LostProductionMinutes = minutesLate
                });
            }
            catch (Exception ex)
            {
                _logger.LogWarning(ex, "Failed to auto-create LateArrival exception for {EmployeeId} on {Date}",
                    employee.EmployeeId, date);
            }
        }

        await _auditService.LogActionAsync(
            actorId: employee.EmployeeId,
            action: "ClockIn",
            entityType: "AttendanceRecord",
            entityId: record.Id.ToString(),
            httpEndpoint: "POST /api/attendance/clockin",
            changesJson: $"{{\"Date\":\"{date:yyyy-MM-dd}\",\"ClockIn\":\"{clockInTime:HH:mm}\",\"IsLate\":{isLate},\"MinutesLate\":{minutesLate}}}");

        _logger.LogInformation("Clock in: {EmployeeId} at {Time}, Late={IsLate}, Minutes={Minutes}",
            employee.EmployeeId, clockInTime, isLate, minutesLate);

        return MapToDto(record, employee);
    }

    /// <summary>
    /// Clock out for an employee on a specific date.
    /// Calculates early departure automatically.
    /// </summary>
    public async Task<AttendanceRecordDto> ClockOutAsync(ClockOutRequest request)
    {
        var employee = await _context.Employees
            .Include(e => e.Department)
            .FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId && e.IsActive);

        if (employee is null)
            throw new NotFoundException($"Employee '{request.EmployeeId}' not found or inactive.");

        var now = DateTimeHelper.NowInSouthAfrica();
        var clockOutTime = now.TimeOfDay;
        var date = request.Date.Date;

        var record = await _context.AttendanceRecords
            .FirstOrDefaultAsync(r => r.EmployeeId == employee.EmployeeId && r.Date.Date == date);

        if (record is null)
            throw new NotFoundException($"No attendance record found for {employee.EmployeeId} on {date:yyyy-MM-dd}.");

        if (record.ClockOut.HasValue)
            throw new ConflictException("Already clocked out for this date.");

        var expectedEnd = record.ExpectedClockOut ?? DateTimeHelper.GetShiftEndTime(
            GetShiftNameFromCode(record.ShiftCode));

        var minutesEarly = DateTimeHelper.CalculateMinutesEarly(clockOutTime, expectedEnd);
        var threshold = GetSettingValueAsInt("EarlyDepartureThresholdMinutes", 10);
        var isEarly = minutesEarly >= threshold;

        record.ClockOut = clockOutTime;
        record.ExpectedClockOut = expectedEnd;
        record.IsEarlyDeparture = isEarly;
        record.MinutesEarly = minutesEarly;
        record.UpdatedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        // Auto-detect: if missing clock-out after shift end + threshold, flag it
        if (!isEarly && expectedEnd <= clockOutTime && clockOutTime - expectedEnd > TimeSpan.FromMinutes(threshold))
        {
            // Normal — clocked out after expected end, no exception
        }

        await _auditService.LogActionAsync(
            actorId: employee.EmployeeId,
            action: "ClockOut",
            entityType: "AttendanceRecord",
            entityId: record.Id.ToString(),
            httpEndpoint: "POST /api/attendance/clockout",
            changesJson: $"{{\"Date\":\"{date:yyyy-MM-dd}\",\"ClockOut\":\"{clockOutTime:HH:mm}\",\"IsEarly\":{isEarly},\"MinutesEarly\":{minutesEarly}}}");

        _logger.LogInformation("Clock out: {EmployeeId} at {Time}, Early={IsEarly}, Minutes={Minutes}",
            employee.EmployeeId, clockOutTime, isEarly, minutesEarly);

        return MapToDto(record, employee);
    }

    /// <summary>
    /// Get daily attendance records for all employees or filtered by department.
    /// </summary>
    public async Task<List<AttendanceRecordDto>> GetDailyAsync(DateTime date, int? departmentId = null, string? requestingEmployeeId = null)
    {
        // Validate department access if requesting employee is provided
        if (!string.IsNullOrEmpty(requestingEmployeeId))
        {
            await _departmentAuth.ValidateDepartmentAccessAsync(requestingEmployeeId, departmentId);
        }

        var query = _context.AttendanceRecords
            .Include(r => r.Employee)
                .ThenInclude(e => e!.Department)
            .Where(r => r.Date.Date == date.Date);

        if (departmentId.HasValue)
        {
            query = query.Where(r => r.Employee!.DepartmentId == departmentId.Value);
        }

        var records = await query.OrderBy(r => r.Employee!.DisplayName).ToListAsync();

        return records.Select(r => MapToDto(r, r.Employee!)).ToList();
    }

    /// <summary>
    /// Get aggregated attendance summary for a date range and optional department.
    /// Calculates: Department Attendance %, Employee Attendance %, Lost Production Hours.
    /// </summary>
    public async Task<AttendanceSummaryDto> GetSummaryAsync(DateTime startDate, DateTime endDate, int? departmentId = null)
    {
        var records = _context.AttendanceRecords
            .Include(r => r.Employee)
                .ThenInclude(e => e!.Department)
            .Where(r => r.Date >= startDate.Date && r.Date <= endDate.Date.Date);

        if (departmentId.HasValue)
            records = records.Where(r => r.Employee!.DepartmentId == departmentId.Value);

        var allRecords = await records.ToListAsync();

        var totalDays = (endDate.Date - startDate.Date).Days + 1;
        var totalPossibleAttendances = 0;
        var presentCount = 0;
        var lateCount = 0;
        var earlyDepartureCount = 0;
        var absenteeismCount = 0;
        var awolCount = 0;
        var missingClockInCount = 0;
        var missingClockOutCount = 0;
        var totalLostMinutes = 0;

        // Get unique employees
        var employees = allRecords.Select(r => r.Employee!).Distinct()
            .Concat(await GetTargetEmployees(departmentId))
            .Distinct()
            .ToList();

        foreach (var emp in employees)
        {
            var empRecords = allRecords.Where(r => r.EmployeeId == emp.EmployeeId).ToList();

            // Count working days in range
            var workingDays = 0;
            for (var d = startDate.Date; d <= endDate.Date; d = d.AddDays(1))
            {
                if (DateTimeHelper.IsWeekDay(d))
                    workingDays++;
            }

            totalPossibleAttendances += workingDays;

            var presentDays = empRecords.Count(r => r.ClockIn.HasValue);
            presentCount += presentDays;

            lateCount += empRecords.Count(r => r.IsLate);
            earlyDepartureCount += empRecords.Count(r => r.IsEarlyDeparture);
            totalLostMinutes += empRecords.Sum(r => r.MinutesLate + r.MinutesEarly);

            // Detect absenteeism (no record at all for a working day)
            var absentDays = workingDays - presentDays;
            absenteeismCount += absentDays;

            // AWOL: consecutive absences
            var sortedDates = empRecords.Select(r => r.Date).OrderBy(d => d).ToList();
            var maxConsecutiveAbsent = CalculateMaxConsecutiveAbsences(sortedDates, startDate, endDate);
            if (maxConsecutiveAbsent >= GetSettingValueAsInt("AWOLThresholdDays", 3))
                awolCount++;

            // Missing clock-in/out
            missingClockInCount += empRecords.Count(r => !r.ClockIn.HasValue);
            missingClockOutCount += empRecords.Count(r => !r.ClockOut.HasValue);
        }

        var attendancePercent = totalPossibleAttendances > 0
            ? Math.Round((decimal)presentCount / totalPossibleAttendances * 100, 2)
            : 0;

        // Determine department name
        string deptName = "All Departments";
        if (departmentId.HasValue)
        {
            var dept = await _context.Departments.FindAsync(departmentId.Value);
            deptName = dept?.Name ?? "Unknown";
        }

        return new AttendanceSummaryDto
        {
            Date = startDate,
            DepartmentName = deptName,
            TotalEmployees = employees.Count,
            Present = presentCount,
            Late = lateCount,
            EarlyDeparture = earlyDepartureCount,
            Absenteeism = absenteeismCount,
            Awol = awolCount,
            MissingClockIn = missingClockInCount,
            MissingClockOut = missingClockOutCount,
            AttendancePercent = attendancePercent,
            TotalLostProductionMinutes = totalLostMinutes
        };
    }

    /// <summary>
    /// Get individual employee attendance report for a date range.
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

        var daysPresent = records.Count(r => r.ClockIn.HasValue);
        var daysAbsent = workingDays - daysPresent;
        var daysLate = records.Count(r => r.IsLate);
        var daysEarly = records.Count(r => r.IsEarlyDeparture);
        var totalLateMins = records.Sum(r => r.MinutesLate);
        var totalEarlyMins = records.Sum(r => r.MinutesEarly);

        var attendancePct = workingDays > 0
            ? Math.Round((decimal)daysPresent / workingDays * 100, 2)
            : 0;

        var dto = new IndividualAttendanceReportDto
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
            Records = records.Select(r => MapToDto(r, employee)).ToList()
        };

        return dto;
    }

    /// <summary>
    /// Scan for missing clock-ins: employees with no record by 06:30 for morning shift.
    /// Creates UnplannedAbsenteeism exceptions automatically.
    /// </summary>
    public async Task ScanMissingClockInsAsync(DateTime date)
    {
        var activeEmployees = await _context.Employees.Where(e => e.IsActive).ToListAsync();
        var threshold = DateTimeHelper.StartOfDayInSast(date).AddHours(6).AddMinutes(30);
        var currentSast = DateTimeHelper.NowInSouthAfrica();

        foreach (var emp in activeEmployees)
        {
            var hasRecord = await _context.AttendanceRecords.AnyAsync(
                r => r.EmployeeId == emp.EmployeeId && r.Date.Date == date.Date);

            if (!hasRecord && currentSast >= threshold)
            {
                try
                {
                    await _exceptionService.CreateAutoExceptionAsync(new CreateExceptionRequest
                    {
                        EmployeeId = emp.EmployeeId,
                        Date = date,
                        ExceptionType = "UnplannedAbsenteeism"
                    });
                }
                catch (Exception ex)
                {
                    _logger.LogWarning(ex, "Failed to auto-scan missing clock-in for {EmployeeId} on {Date}",
                        emp.EmployeeId, date);
                }
            }
        }
    }

    /// <summary>
    /// Scan for missing clock-outs: employees who haven't clocked out after shift end + 10 min.
    /// Creates MissingClockOut exceptions automatically.
    /// </summary>
    public async Task ScanMissingClockOutsAsync(DateTime date)
    {
        var activeEmployees = await _context.Employees.Where(e => e.IsActive).ToListAsync();

        foreach (var emp in activeEmployees)
        {
            var record = await _context.AttendanceRecords
                .FirstOrDefaultAsync(r => r.EmployeeId == emp.EmployeeId && r.Date.Date == date.Date);

            if (record != null && !record.ClockOut.HasValue && record.ClockIn.HasValue)
            {
                var expectedEnd = record.ExpectedClockOut ?? new TimeSpan(14, 0, 0);
                var cutoff = DateTimeHelper.StartOfDayInSast(date).Add(expectedEnd).AddMinutes(10);

                if (DateTimeHelper.NowInSouthAfrica() >= cutoff)
                {
                    try
                    {
                        await _exceptionService.CreateAutoExceptionAsync(new CreateExceptionRequest
                        {
                            EmployeeId = emp.EmployeeId,
                            Date = date,
                            ExceptionType = "MissingClockOut",
                            ExpectedClockOut = expectedEnd
                        });
                    }
                    catch (Exception ex)
                    {
                        _logger.LogWarning(ex, "Failed to auto-scan missing clock-out for {EmployeeId} on {Date}",
                            emp.EmployeeId, date);
                    }
                }
            }
        }
    }

    private static string GetShiftCodeForShiftName(string shiftName)
    {
        return shiftName.ToLowerInvariant() switch
        {
            "morning" => "ES",
            "afternoon" => "MS",
            "night" => "NS",
            _ => "DS"
        };
    }

    private static string GetShiftNameFromCode(string? code)
    {
        return code?.ToLowerInvariant() switch
        {
            "es" => "Morning",
            "ms" => "Afternoon",
            "ns" => "Night",
            _ => "Morning"
        };
    }

    private async Task<IEnumerable<Employee>> GetTargetEmployees(int? departmentId)
    {
        if (departmentId.HasValue)
        {
            return await _context.Employees.Where(e => e.DepartmentId == departmentId.Value && e.IsActive).ToListAsync();
        }
        return await _context.Employees.Where(e => e.IsActive).ToListAsync();
    }

    private int CalculateMaxConsecutiveAbsences(List<DateTime> presentDates, DateTime startDate, DateTime endDate)
    {
        if (!presentDates.Any())
            return (endDate.Date - startDate.Date).Days + 1;

        var allDates = Enumerable.Range(0, (endDate.Date - startDate.Date).Days + 1)
            .Select(i => startDate.Date.AddDays(i))
            .Where(d => DateTimeHelper.IsWeekDay(d))
            .ToHashSet();

        var presentSet = new HashSet<DateTime>(presentDates);
        var maxStreak = 0;
        var currentStreak = 0;

        foreach (var date in allDates.OrderBy(d => d))
        {
            if (presentSet.Contains(date))
            {
                currentStreak = 0;
            }
            else
            {
                currentStreak++;
                maxStreak = Math.Max(maxStreak, currentStreak);
            }
        }

        return maxStreak;
    }

    private static int GetSettingValueAsInt(string key, int defaultValue)
    {
        // In production, read from SystemSettings table via ReportService
        // For now, use defaults — the DB seed data contains these values
        return key switch
        {
            "LateGracePeriodMinutes" => 5,
            "EarlyDepartureThresholdMinutes" => 10,
            "AWOLThresholdDays" => 3,
            _ => defaultValue
        };
    }

    private static AttendanceRecordDto MapToDto(AttendanceRecord record, Employee employee)
    {
        return new AttendanceRecordDto
        {
            Id = record.Id,
            EmployeeId = record.EmployeeId,
            DisplayName = employee.DisplayName,
            Date = record.Date,
            ShiftCode = record.ShiftCode,
            ClockIn = record.ClockIn,
            ClockOut = record.ClockOut,
            IsLate = record.IsLate,
            MinutesLate = record.MinutesLate,
            IsEarlyDeparture = record.IsEarlyDeparture,
            MinutesEarly = record.MinutesEarly,
            TotalHoursWorked = record.TotalHoursWorked,
            CreatedAt = record.CreatedAt
        };
    }
}
