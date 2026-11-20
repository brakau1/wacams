using WACAMS.Core.RulesEngine.Models;
using WACAMS.Core.RulesEngine.Rules;
using Microsoft.Extensions.Logging;

namespace WACAMS.Core.RulesEngine.Services;

/// <summary>
/// Attendance rules engine service implementation
/// </summary>
public class AttendanceRulesEngine : IAttendanceRulesEngine
{
    private readonly ILogger<AttendanceRulesEngine> _logger;
    private readonly LateArrivalCalculationRule _lateArrivalRule;
    private readonly EarlyDepartureCalculationRule _earlyDepartureRule;
    private readonly LostProductionCalculationRule _lostProductionRule;
    private readonly UnplannedAbsenteeismCalculationRule _unplannedAbsenceRule;
    private readonly ExceptionSeverityRule _severityRule;
    private readonly CorrectiveActionDeterminationRule _correctiveActionRule;

    public AttendanceRulesEngine(ILogger<AttendanceRulesEngine> logger)
    {
        _logger = logger;
        _lateArrivalRule = new LateArrivalCalculationRule();
        _earlyDepartureRule = new EarlyDepartureCalculationRule();
        _lostProductionRule = new LostProductionCalculationRule();
        _unplannedAbsenceRule = new UnplannedAbsenteeismCalculationRule();
        _severityRule = new ExceptionSeverityRule();
        _correctiveActionRule = new CorrectiveActionDeterminationRule();
    }

    public async Task<AttendanceCalculationResult> CalculateAttendanceAsync(AttendanceCalculationContext context)
    {
        _logger.LogInformation("Calculating attendance for employee {EmployeeId} on {Date}", 
            context.EmployeeId, context.AttendanceDate);

        var result = new AttendanceCalculationResult
        {
            EmployeeId = context.EmployeeId,
            AttendanceDate = context.AttendanceDate,
            ShiftType = context.ShiftType,
            ScheduledStart = context.ScheduledStart,
            ActualStart = context.ActualStart,
            ScheduledEnd = context.ScheduledEnd,
            ActualEnd = context.ActualEnd
        };

        // Apply unplanned absence rule first
        if (_unplannedAbsenceRule.CanCalculate(context))
        {
            var absenceResult = _unplannedAbsenceRule.Calculate(context);
            if (absenceResult.IsAbsent)
            {
                return absenceResult;
            }
        }

        // Apply late arrival rule
        if (_lateArrivalRule.CanCalculate(context))
        {
            var lateResult = _lateArrivalRule.Calculate(context);
            result.IsLate = lateResult.IsLate;
            result.LateMinutes = lateResult.LateMinutes;
            if (lateResult.HasException && !result.HasException)
            {
                result.ExceptionType = lateResult.ExceptionType;
                result.ExceptionDescription = lateResult.ExceptionDescription;
                result.HasException = true;
            }
        }

        // Apply early departure rule
        if (_earlyDepartureRule.CanCalculate(context))
        {
            var earlyResult = _earlyDepartureRule.Calculate(context);
            result.IsEarlyDeparture = earlyResult.IsEarlyDeparture;
            result.EarlyMinutes = earlyResult.EarlyMinutes;
            if (earlyResult.HasException && !result.HasException)
            {
                result.ExceptionType = earlyResult.ExceptionType;
                result.ExceptionDescription = earlyResult.ExceptionDescription;
                result.HasException = true;
            }
        }

        // Apply lost production rule
        if (_lostProductionRule.CanCalculate(context))
        {
            var lostResult = _lostProductionRule.Calculate(context);
            result.LostMinutes = lostResult.LostMinutes;
            if (lostResult.HasException && !result.HasException)
            {
                result.ExceptionType = lostResult.ExceptionType;
                result.ExceptionDescription = lostResult.ExceptionDescription;
                result.HasException = true;
            }
        }

        // Determine if present
        result.IsPresent = !result.IsAbsent && (result.ActualStart.HasValue || result.ActualEnd.HasValue);

        _logger.LogInformation("Attendance calculation complete for {EmployeeId}. Late: {IsLate}, Early: {IsEarly}, Absent: {IsAbsent}, LostMinutes: {LostMinutes}",
            context.EmployeeId, result.IsLate, result.IsEarlyDeparture, result.IsAbsent, result.LostMinutes);

        return await Task.FromResult(result);
    }

    public async Task<List<AttendanceException>> DetectExceptionsAsync(DateTime date, string? departmentId = null)
    {
        _logger.LogInformation("Detecting exceptions for date {Date}, department {DepartmentId}", 
            date, departmentId ?? "all");

        // This would typically query attendance records from database
        // For now, returning empty list as we don't have database access in Core layer
        // Implementation would be in Infrastructure layer
        await Task.CompletedTask;
        
        return new List<AttendanceException>();
    }

    public async Task<AttendanceException> ProcessExceptionAsync(
        Guid exceptionId,
        string investigatorId,
        string decision,
        string? notes = null)
    {
        _logger.LogInformation("Processing exception {ExceptionId} by investigator {InvestigatorId} with decision {Decision}",
            exceptionId, investigatorId, decision);

        // This would typically update database
        // For now, returning a placeholder
        await Task.CompletedTask;

        var exception = new AttendanceException
        {
            Id = exceptionId,
            InvestigatedBy = investigatorId,
            InvestigatedAt = DateTime.UtcNow,
            InvestigationNotes = notes
        };

        if (decision.ToLower() == "approve")
        {
            exception.Status = ExceptionStatus.Approved;
            exception.IsApproved = true;
            exception.ApprovedBy = investigatorId;
            exception.ApprovedAt = DateTime.UtcNow;
        }
        else if (decision.ToLower() == "reject")
        {
            exception.Status = ExceptionStatus.Rejected;
            exception.IsApproved = false;
            exception.RejectionReason = notes;
        }

        return exception;
    }

    public async Task<CorrectiveAction> GenerateCorrectiveActionAsync(Guid exceptionId, string issuedBy)
    {
        _logger.LogInformation("Generating corrective action for exception {ExceptionId} issued by {IssuedBy}",
            exceptionId, issuedBy);

        // This would typically query exception and prior occurrences from database
        // For now, returning a placeholder
        await Task.CompletedTask;

        var exception = new AttendanceException
        {
            Id = exceptionId,
            ExceptionType = "LateArrival",
            Severity = "Medium"
        };

        var action = _correctiveActionRule.DetermineAction(exception, 0);
        action.IssuedBy = issuedBy;

        return action;
    }

    public async Task<decimal> CalculateDepartmentAttendanceAsync(string departmentId, DateTime date)
    {
        _logger.LogInformation("Calculating department attendance for {DepartmentId} on {Date}", 
            departmentId, date);

        // This would typically query database for department employees and their attendance
        // For now, returning placeholder
        await Task.CompletedTask;

        return 0m;
    }

    public async Task<(int minutes, decimal hours)> CalculateLostProductionAsync(string departmentId, DateTime date)
    {
        _logger.LogInformation("Calculating lost production for {DepartmentId} on {Date}", 
            departmentId, date);

        // This would typically query database for department exceptions
        // For now, returning placeholder
        await Task.CompletedTask;

        return (0, 0m);
    }
}
