using WACAMS.Core.Exceptions.DTOs;
using WACAMS.Core.Exceptions.Models;

namespace WACAMS.Core.Exceptions.Interfaces;

/// <summary>
/// Service interface for managing attendance exceptions
/// </summary>
public interface IExceptionService
{
    Task<ExceptionResponse> CreateExceptionAsync(CreateExceptionRequest request);
    Task<ExceptionResponse> GetExceptionByIdAsync(Guid id);
    Task<IEnumerable<ExceptionResponse>> GetExceptionsByEmployeeAsync(string employeeId, DateTime? startDate = null, DateTime? endDate = null);
    Task<IEnumerable<ExceptionResponse>> GetPendingExceptionsAsync();
    Task<ExceptionResponse> ApproveExceptionAsync(Guid exceptionId, string approvedBy, string? comments);
    Task<ExceptionResponse> RejectExceptionAsync(Guid exceptionId, string rejectedBy, string? comments);
    Task<bool> DeleteExceptionAsync(Guid id);
}
