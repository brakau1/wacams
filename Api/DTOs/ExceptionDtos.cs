using System.ComponentModel.DataAnnotations;

namespace WACAMS.Api.DTOs;

public class CreateExceptionRequest
{
    [Required]
    [StringLength(20)]
    public string EmployeeId { get; set; } = string.Empty;

    [Required]
    public DateTime Date { get; set; }

    /// <summary>
    /// Exception types: LateArrival, EarlyDeparture, UnplannedAbsenteeism, Awol,
    /// SickLeave, FamilyResponsibilityLeave, ShiftCancellation, WorkshopAttendance,
    /// Training, MissingClockIn, MissingClockOut, UnscheduledClockIn, UnscheduledClockOut
    /// </summary>
    [Required]
    [StringLength(50)]
    public string ExceptionType { get; set; } = string.Empty;

    public TimeSpan? ExpectedClockIn { get; set; }
    public TimeSpan? ActualClockIn { get; set; }
    public TimeSpan? ExpectedClockOut { get; set; }
    public TimeSpan? ActualClockOut { get; set; }
    public int MinutesLate { get; set; } = 0;
    public int MinutesEarly { get; set; } = 0;
    public int LostProductionMinutes { get; set; } = 0;
}

public class InvestigateExceptionRequest
{
    [StringLength(2000)]
    public string InvestigationNotes { get; set; } = string.Empty;
}

public class AttendanceExceptionDto
{
    public Guid Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public string DepartmentName { get; set; } = string.Empty;
    public DateTime Date { get; set; }
    public string ExceptionType { get; set; } = string.Empty;
    public TimeSpan? ExpectedClockIn { get; set; }
    public TimeSpan? ActualClockIn { get; set; }
    public TimeSpan? ExpectedClockOut { get; set; }
    public TimeSpan? ActualClockOut { get; set; }
    public int MinutesLate { get; set; }
    public int MinutesEarly { get; set; }
    public int LostProductionMinutes { get; set; }
    public string Status { get; set; } = "Pending";
    public string? InvestigationNotes { get; set; }
    public string? InvestigatedBy { get; set; }
    public DateTime? InvestigatedAt { get; set; }
    public DateTime CreatedAt { get; set; }
}
