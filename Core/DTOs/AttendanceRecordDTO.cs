namespace WACAMS.Core.DTOs;

/// <summary>
/// Data transfer object for attendance record read operations.
/// Contains clock-in/out times, calculated late/early minutes, and status with employee name resolved.
/// </summary>
public record AttendanceRecordDTO(
    Guid Id,
    string EmployeeId,
    string EmployeeName,
    DateTime Date,
    DateTime? ClockInTime,
    DateTime? ClockOutTime,
    int LateMinutes,
    int EarlyMinutes,
    Core.Enums.AttendanceStatus Status
);
