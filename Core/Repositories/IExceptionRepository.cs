using WACAMS.Core.Exceptions.Models;

namespace WACAMS.Core.Repositories;

/// <summary>
/// Repository interface for attendance exceptions
/// </summary>
public interface IExceptionRepository
{
    Task<AttendanceException?> GetByIdAsync(Guid id);
    Task<IEnumerable<AttendanceException>> GetByEmployeeIdAsync(string employeeId, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<AttendanceException>> GetPendingExceptionsAsync();
    Task<IEnumerable<AttendanceException>> GetByDateRangeAsync(DateTime startDate, DateTime endDate);
    Task<AttendanceException> CreateAsync(AttendanceException exception);
    Task<AttendanceException> UpdateAsync(AttendanceException exception);
    Task<bool> DeleteAsync(Guid id);
}
