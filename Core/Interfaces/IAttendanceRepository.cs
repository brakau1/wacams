using WACAMS.Core.Entities;

namespace WACAMS.Core.Interfaces;

/// <summary>
/// Specialized repository for AttendanceRecord operations with analytics capabilities.
/// Provides methods for date-range queries, daily statistics, and lost-time calculations.
/// </summary>
public interface IAttendanceRepository : IRepository<AttendanceRecord>
{
    /// <summary>
    /// Retrieves all attendance records for a specific employee within a date range.
    /// Includes shift schedule data for expected clock-in/out comparison.
    /// </summary>
    /// <param name="employeeId">The employee's unique identifier.</param>
    /// <param name="startDate">Start of the date range (inclusive).</param>
    /// <param name="endDate">End of the date range (inclusive).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>All attendance records for the employee in the specified range.</returns>
    Task<IEnumerable<AttendanceRecord>> GetByDateRangeAsync(
        string employeeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves daily attendance statistics across all employees for a specific date.
    /// Aggregates counts by attendance status (Present, Late, Absent, etc.).
    /// </summary>
    /// <param name="date">The work date to aggregate statistics for.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A dictionary mapping each AttendanceStatus to its count.</returns>
    Task<Dictionary<Enums.AttendanceStatus, int>> GetDailyStatsAsync(
        DateTime date, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates total lost production minutes for an employee within a date range.
    /// Lost minutes = sum of (LateMinutes + EarlyMinutes) across all records in range.
    /// Results are grouped by date for daily breakdown reporting.
    /// </summary>
    /// <param name="employeeId">The employee's unique identifier.</param>
    /// <param name="startDate">Start of the date range (inclusive).</param>
    /// <param name="endDate">End of the date range (inclusive).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>A dictionary mapping each date to its total lost minutes.</returns>
    Task<Dictionary<DateTime, int>> CalculateLostMinutesAsync(
        string employeeId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Retrieves all attendance exceptions recorded for a specific employee.
    /// Ordered by date descending (most recent first).
    /// </summary>
    /// <param name="employeeId">The employee's unique identifier.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>All exceptions for the employee, ordered by date descending.</returns>
    Task<IEnumerable<AttendanceException>> GetExceptionsByEmployeeAsync(
        string employeeId, CancellationToken cancellationToken = default);

    /// <summary>
    /// Calculates department-wide attendance percentage for a given date range.
    /// Formula: (Sum of PresentDays across all department employees / Total working days × number of employees) × 100
    /// </summary>
    /// <param name="departmentId">The GUID of the department.</param>
    /// <param name="startDate">Start of the date range.</param>
    /// <param name="endDate">End of the date range.</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The department attendance percentage (0-100).</returns>
    Task<decimal> CalculateDepartmentAttendancePercentageAsync(
        Guid departmentId, DateTime startDate, DateTime endDate, CancellationToken cancellationToken = default);

    /// <summary>
    /// Counts the number of attendance exceptions for an employee within a month.
    /// Used as input for warning generation thresholds.
    /// </summary>
    /// <param name="employeeId">The employee's unique identifier.</param>
    /// <param name="year">The year to check.</param>
    /// <param name="month">The month to check (1-12).</param>
    /// <param name="cancellationToken">Token to cancel the operation.</param>
    /// <returns>The count of exceptions in the specified month.</returns>
    Task<int> CountExceptionsInMonthAsync(
        string employeeId, int year, int month, CancellationToken cancellationToken = default);
}
