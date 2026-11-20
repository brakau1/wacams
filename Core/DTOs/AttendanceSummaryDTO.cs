namespace WACAMS.Core.DTOs;

/// <summary>
/// Aggregated attendance summary for an employee over a specific period.
/// Used in dashboard KPIs, monthly reports, and performance evaluations.
/// </summary>
public record AttendanceSummaryDTO(
    string EmployeeId,
    Guid DepartmentId,
    int TotalDays,
    int PresentDays,
    int LateDays,
    int AbsentDays,
    int ExceptionsCount,
    int LostMinutes,
    decimal AttendancePercentage
);
