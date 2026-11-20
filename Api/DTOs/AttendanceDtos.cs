using System.ComponentModel.DataAnnotations;

namespace Wacam.Api.DTOs;

public class ClockInRequest
{
    [Required]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; }
}

public class ClockOutRequest
{
    [Required]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; }
}

public class AttendanceRecordDto
{
    public Guid Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string? ShiftCode { get; set; }
    public TimeSpan? ClockIn { get; set; }
    public TimeSpan? ClockOut { get; set; }
    public bool IsLate { get; set; }
    public int MinutesLate { get; set; }
    public bool IsEarlyDeparture { get; set; }
    public int MinutesEarly { get; set; }
    public decimal TotalHoursWorked { get; set; }
    public DateTime CreatedAt { get; set; }
}

public class AttendanceSummaryDto
{
    public DateTime Date { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public int TotalEmployees { get; set; }
    public int Present { get; set; }
    public int Late { get; set; }
    public int EarlyDeparture { get; set; }
    public int Absenteeism { get; set; }
    public int Awol { get; set; }
    public int MissingClockIn { get; set; }
    public int MissingClockOut { get; set; }
    public decimal AttendancePercent { get; set; }
    public int TotalLostProductionMinutes { get; set; }
}

public class DepartmentAttendanceSummaryDto
{
    public Guid DepartmentId { get; set; }
    public string DepartmentName { get; set; } = string.Empty;
    public decimal TargetAttendancePercent { get; set; }
    public decimal ActualAttendancePercent { get; set; }
    public int TotalEmployees { get; set; }
    public int Present { get; set; }
    public int LateTotal { get; set; }
    public int EarlyDepartureTotal { get; set; }
    public int AbsenteeismTotal { get; set; }
    public int AwolTotal { get; set; }
    public int MissingClockInTotal { get; set; }
    public int MissingClockOutTotal { get; set; }
    public int TotalLostProductionMinutes { get; set; }
}

public class IndividualAttendanceReportDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime StartDate { get; set; }
    public DateTime EndDate { get; set; }
    public int TotalDays { get; set; }
    public int DaysPresent { get; set; }
    public int DaysAbsent { get; set; }
    public int DaysLate { get; set; }
    public int DaysEarlyDeparture { get; set; }
    public int TotalLateMinutes { get; set; }
    public int TotalEarlyMinutes { get; set; }
    public decimal AttendancePercentage { get; set; }
    public List<AttendanceRecordDto> Records { get; set; } = new();
}
