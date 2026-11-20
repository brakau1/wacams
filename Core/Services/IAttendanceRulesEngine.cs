using WACAMS.Core.RulesEngine.Models;
using WACAMS.Core.RulesEngine.Rules;

namespace WACAMS.Core.Services;

/// <summary>
/// Interface for the attendance rules engine service
/// </summary>
public interface IAttendanceRulesEngine
{
    /// <summary>
    /// Calculate attendance metrics for an employee
    /// </summary>
    Task<AttendanceCalculationResult> CalculateAttendanceAsync(AttendanceCalculationContext context);

    /// <summary>
    /// Detect all exceptions for a given date
    /// </summary>
    Task<List<AttendanceException>> DetectExceptionsAsync(DateTime date, string? departmentId = null);

    /// <summary>
    /// Process exception workflow (investigate, approve/reject)
    /// </summary>
    Task<AttendanceException> ProcessExceptionAsync(
        Guid exceptionId,
        string investigatorId,
        string decision,
        string? notes = null);

    /// <summary>
    /// Generate corrective action for an approved exception
    /// </summary>
    Task<CorrectiveAction> GenerateCorrectiveActionAsync(Guid exceptionId, string issuedBy);

    /// <summary>
    /// Calculate department attendance percentage
    /// </summary>
    Task<decimal> CalculateDepartmentAttendanceAsync(string departmentId, DateTime date);

    /// <summary>
    /// Calculate total lost production for a department
    /// </summary>
    Task<(int minutes, decimal hours)> CalculateLostProductionAsync(string departmentId, DateTime date);
}
