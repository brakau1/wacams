using WACAMS.Core.Disciplinary.Services;
using WACAMS.Core.Disciplinary.Models;
using WACAMS.Core.Services;
using WACAMS.Core.Exceptions.DTOs;
using WACAMS.Core.Exceptions.Interfaces;
using WACAMS.Core.Exceptions.Models;
using WACAMS.Infrastructure.Data;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Logging;

namespace WACAMS.Infrastructure.Services;

/// <summary>
/// Service implementation for managing attendance exceptions
/// </summary>
public class ExceptionService : IExceptionService
{
    private readonly IExceptionRepository _exceptionRepository;
    private readonly IDisciplinaryService _disciplinaryService;
    private readonly IEscalationService _escalationService;
    private readonly ILogger<ExceptionService> _logger;

    public ExceptionService(
        IExceptionRepository exceptionRepository,
        IDisciplinaryService disciplinaryService,
        IEscalationService escalationService,
        ILogger<ExceptionService> logger)
    {
        _exceptionRepository = exceptionRepository;
        _disciplinaryService = disciplinaryService;
        _escalationService = escalationService;
        _logger = logger;
    }

    public async Task<ExceptionResponse> CreateExceptionAsync(CreateExceptionRequest request)
    {
        _logger.LogInformation("Creating attendance exception for employee {EmployeeId} on {Date}", 
            request.EmployeeId, request.ExceptionDate);

        var exception = new AttendanceException
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            ExceptionDate = request.ExceptionDate,
            Category = request.Category,
            Description = request.Description,
            ScheduledClockIn = request.ScheduledClockIn,
            ActualClockIn = request.ActualClockIn,
            ScheduledClockOut = request.ScheduledClockOut,
            ActualClockOut = request.ActualClockOut,
            LostMinutes = request.LostMinutes,
            SupportingDocumentUrl = request.SupportingDocumentUrl,
            Status = ApprovalStatus.Pending,
            CreatedBy = request.CreatedBy,
            CreatedAt = DateTime.UtcNow
        };

        var createdException = await _exceptionRepository.CreateAsync(exception);

        _logger.LogInformation("Attendance exception {ExceptionId} created successfully", createdException.Id);

        return await MapToResponseAsync(createdException);
    }

    public async Task<ExceptionResponse> GetExceptionByIdAsync(Guid id)
    {
        var exception = await _exceptionRepository.GetByIdAsync(id);
        if (exception == null)
        {
            throw new KeyNotFoundException($"Exception with id {id} not found");
        }

        return await MapToResponseAsync(exception);
    }

    public async Task<IEnumerable<ExceptionResponse>> GetExceptionsByEmployeeAsync(
        string employeeId, 
        DateTime? startDate = null, 
        DateTime? endDate = null)
    {
        var exceptions = await _exceptionRepository.GetByEmployeeIdAsync(employeeId, startDate, endDate);
        return await Task.WhenAll(exceptions.Select(MapToResponseAsync));
    }

    public async Task<IEnumerable<ExceptionResponse>> GetPendingExceptionsAsync()
    {
        var exceptions = await _exceptionRepository.GetPendingExceptionsAsync();
        return await Task.WhenAll(exceptions.Select(MapToResponseAsync));
    }

    public async Task<ExceptionResponse> ApproveExceptionAsync(Guid exceptionId, string approvedBy, string? comments)
    {
        _logger.LogInformation("Approving exception {ExceptionId} by {ApprovedBy}", exceptionId, approvedBy);

        var exception = await _exceptionRepository.GetByIdAsync(exceptionId);
        if (exception == null)
        {
            throw new KeyNotFoundException($"Exception with id {exceptionId} not found");
        }

        if (exception.Status != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException($"Exception {exceptionId} is not in pending status");
        }

        exception.Status = ApprovalStatus.Approved;
        exception.ApprovedBy = approvedBy;
        exception.ApprovalDate = DateTime.UtcNow;
        exception.ApprovalComments = comments;
        exception.UpdatedBy = approvedBy;
        exception.UpdatedAt = DateTime.UtcNow;

        // Cancel any related corrective action
        if (exception.CorrectiveActionId.HasValue)
        {
            _logger.LogInformation("Cancelling corrective action {ActionId} for approved exception {ExceptionId}", 
                exception.CorrectiveActionId, exceptionId);

            await _disciplinaryService.CancelCorrectiveActionAsync(
                exception.CorrectiveActionId.Value, 
                approvedBy, 
                "Exception approved - corrective action cancelled");
        }

        var updatedException = await _exceptionRepository.UpdateAsync(exception);

        _logger.LogInformation("Exception {ExceptionId} approved successfully", exceptionId);

        return await MapToResponseAsync(updatedException);
    }

    public async Task<ExceptionResponse> RejectExceptionAsync(Guid exceptionId, string rejectedBy, string? comments)
    {
        _logger.LogInformation("Rejecting exception {ExceptionId} by {RejectedBy}", exceptionId, rejectedBy);

        var exception = await _exceptionRepository.GetByIdAsync(exceptionId);
        if (exception == null)
        {
            throw new KeyNotFoundException($"Exception with id {exceptionId} not found");
        }

        if (exception.Status != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException($"Exception {exceptionId} is not in pending status");
        }

        exception.Status = ApprovalStatus.Rejected;
        exception.ApprovedBy = rejectedBy;
        exception.ApprovalDate = DateTime.UtcNow;
        exception.ApprovalComments = comments;
        exception.UpdatedBy = rejectedBy;
        exception.UpdatedAt = DateTime.UtcNow;

        // Generate corrective action
        _logger.LogInformation("Generating corrective action for rejected exception {ExceptionId}", exceptionId);

        var correctiveAction = await _disciplinaryService.GenerateCorrectiveActionAsync(
            exception.EmployeeId,
            MapCategoryToOffenceType(exception.Category),
            exception.ExceptionDate,
            rejectedBy,
            $"Exception rejected: {comments}");

        exception.CorrectiveActionId = correctiveAction.Id;

        var updatedException = await _exceptionRepository.UpdateAsync(exception);

        _logger.LogInformation("Exception {ExceptionId} rejected and corrective action {ActionId} generated", 
            exceptionId, correctiveAction.Id);

        // Create escalation for the rejected exception
        _logger.LogInformation("Creating escalation for rejected exception {ExceptionId}", exceptionId);

        var escalation = await _escalationService.CreateEscalationAsync(
            exceptionId,
            rejectedBy);

        _logger.LogInformation("Escalation {EscalationId} created for exception {ExceptionId}", 
            escalation.Id, exceptionId);

        return await MapToResponseAsync(updatedException);
    }

    public async Task<bool> DeleteExceptionAsync(Guid id)
    {
        _logger.LogInformation("Deleting exception {ExceptionId}", id);

        var exception = await _exceptionRepository.GetByIdAsync(id);
        if (exception == null)
        {
            return false;
        }

        if (exception.Status != ApprovalStatus.Pending)
        {
            throw new InvalidOperationException($"Cannot delete exception {id} - only pending exceptions can be deleted");
        }

        var result = await _exceptionRepository.DeleteAsync(id);

        _logger.LogInformation("Exception {ExceptionId} deleted successfully", id);

        return result;
    }

    private async Task<ExceptionResponse> MapToResponseAsync(AttendanceException exception)
    {
        string? approverName = null;
        if (!string.IsNullOrEmpty(exception.ApprovedBy))
        {
            // In a real implementation, fetch employee name from database
            approverName = $"Employee {exception.ApprovedBy}";
        }

        return await Task.FromResult(new ExceptionResponse
        {
            Id = exception.Id,
            EmployeeId = exception.EmployeeId,
            EmployeeName = exception.Employee?.DisplayName ?? $"Employee {exception.EmployeeId}",
            ExceptionDate = exception.ExceptionDate,
            Category = exception.Category.ToString(),
            Description = exception.Description,
            ScheduledClockIn = exception.ScheduledClockIn?.ToString(@"hh\:mm"),
            ActualClockIn = exception.ActualClockIn?.ToString(@"hh\:mm"),
            ScheduledClockOut = exception.ScheduledClockOut?.ToString(@"hh\:mm"),
            ActualClockOut = exception.ActualClockOut?.ToString(@"hh\:mm"),
            LostMinutes = exception.LostMinutes,
            LostHours = exception.LostHours,
            Status = exception.Status.ToString(),
            ApprovedBy = exception.ApprovedBy,
            ApproverName = approverName,
            ApprovalDate = exception.ApprovalDate,
            ApprovalComments = exception.ApprovalComments,
            SupportingDocumentUrl = exception.SupportingDocumentUrl,
            CorrectiveActionId = exception.CorrectiveActionId,
            CreatedBy = exception.CreatedBy,
            CreatedAt = exception.CreatedAt,
            UpdatedBy = exception.UpdatedBy,
            UpdatedAt = exception.UpdatedAt
        });
    }

    private OffenceType MapCategoryToOffenceType(ExceptionCategory category)
    {
        return category switch
        {
            ExceptionCategory.EarlyDeparture => OffenceType.EarlyDeparture,
            ExceptionCategory.UnplannedAbsenteeism => OffenceType.UnplannedAbsence,
            ExceptionCategory.MissingClockIn => OffenceType.MissingClockIn,
            ExceptionCategory.MissingClockOut => OffenceType.MissingClockOut,
            ExceptionCategory.UnscheduledClockIn => OffenceType.UnscheduledClockIn,
            ExceptionCategory.UnscheduledClockOut => OffenceType.UnscheduledClockOut,
            _ => OffenceType.UnplannedAbsence // Default fallback
        };
    }
}
