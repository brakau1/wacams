using WACAMS.Core.RulesEngine.Rules;

namespace WACAMS.Core.RulesEngine.Validation;

/// <summary>
/// Attendance validator implementation
/// </summary>
public class AttendanceValidator : IAttendanceValidator
{
    public ValidationResult Validate(AttendanceCalculationContext context)
    {
        var result = new ValidationResult();

        // Validate required fields
        if (string.IsNullOrWhiteSpace(context.EmployeeId))
        {
            result.AddError("EmployeeId is required");
        }

        if (context.AttendanceDate == default)
        {
            result.AddError("AttendanceDate is required");
        }

        if (string.IsNullOrWhiteSpace(context.ShiftType))
        {
            result.AddError("ShiftType is required");
        }

        // Validate time ranges
        if (context.ScheduledStart.HasValue && context.ScheduledEnd.HasValue)
        {
            if (context.ScheduledStart.Value >= context.ScheduledEnd.Value)
            {
                result.AddError("ScheduledStart must be before ScheduledEnd");
            }
        }

        // Validate actual times are within reasonable bounds
        if (context.ActualStart.HasValue && context.ScheduledStart.HasValue)
        {
            var lateMinutes = (context.ActualStart.Value - context.ScheduledStart.Value).TotalMinutes;
            if (Math.Abs(lateMinutes) > 720) // 12 hours
            {
                result.AddError("ActualStart is outside reasonable bounds (more than 12 hours from scheduled start)");
            }
        }

        if (context.ActualEnd.HasValue && context.ScheduledEnd.HasValue)
        {
            var earlyMinutes = (context.ScheduledEnd.Value - context.ActualEnd.Value).TotalMinutes;
            if (Math.Abs(earlyMinutes) > 720) // 12 hours
            {
                result.AddError("ActualEnd is outside reasonable bounds (more than 12 hours from scheduled end)");
            }
        }

        // Validate grace periods are non-negative
        if (context.LateGraceMinutes < 0)
        {
            result.AddError("LateGraceMinutes cannot be negative");
        }

        if (context.EarlyGraceMinutes < 0)
        {
            result.AddError("EarlyGraceMinutes cannot be negative");
        }

        return result;
    }

    public ValidationResult ValidateExceptionProcessing(Guid exceptionId, string investigatorId, string decision)
    {
        var result = new ValidationResult();

        if (exceptionId == Guid.Empty)
        {
            result.AddError("ExceptionId is required");
        }

        if (string.IsNullOrWhiteSpace(investigatorId))
        {
            result.AddError("InvestigatorId is required");
        }

        if (string.IsNullOrWhiteSpace(decision))
        {
            result.AddError("Decision is required");
        }

        var validDecisions = new[] { "approve", "reject" };
        if (!validDecisions.Contains(decision.ToLower()))
        {
            result.AddError($"Decision must be one of: {string.Join(", ", validDecisions)}");
        }

        return result;
    }

    public ValidationResult ValidateCorrectiveActionGeneration(Guid exceptionId, string issuedBy)
    {
        var result = new ValidationResult();

        if (exceptionId == Guid.Empty)
        {
            result.AddError("ExceptionId is required");
        }

        if (string.IsNullOrWhiteSpace(issuedBy))
        {
            result.AddError("IssuedBy is required");
        }

        return result;
    }
}
