using WACAMS.Core.RulesEngine.Models;

namespace WACAMS.Infrastructure.Repositories;

/// <summary>
/// Interface for attendance exception repository
/// </summary>
public interface IAttendanceExceptionRepository
{
    Task<AttendanceException?> GetByIdAsync(Guid id);
    Task<List<AttendanceException>> GetByDateAsync(DateTime date, string? departmentId = null);
    Task<List<AttendanceException>> GetByEmployeeAsync(string employeeId, DateTime? fromDate = null, DateTime? toDate = null);
    Task<AttendanceException> CreateAsync(AttendanceException exception);
    Task<AttendanceException> UpdateAsync(AttendanceException exception);
    Task<int> GetPriorOccurrencesAsync(string employeeId, string exceptionType);
}
