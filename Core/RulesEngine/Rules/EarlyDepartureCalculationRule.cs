using WACAMS.Core.RulesEngine.Models;

namespace WACAMS.Core.RulesEngine.Rules;

/// <summary>
/// Rule for calculating early departure
/// </summary>
public class EarlyDepartureCalculationRule : IAttendanceCalculationRule
{
    public string RuleName => "EarlyDepartureCalculation";
    public string Description => "Calculates early departure time and determines if employee left early";

    public bool CanCalculate(AttendanceCalculationContext context)
    {
        return context.ScheduledEnd.HasValue && context.ActualEnd.HasValue;
    }

    public AttendanceCalculationResult Calculate(AttendanceCalculationContext context)
    {
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

        if (!CanCalculate(context))
        {
            return result;
        }

        // Calculate early minutes
        if (context.ActualEnd < context.ScheduledEnd)
        {
            var earlyTime = context.ScheduledEnd.Value - context.ActualEnd.Value;
            var earlyMinutes = (int)earlyTime.TotalMinutes;

            // Apply grace period
            if (earlyMinutes > context.EarlyGraceMinutes)
            {
                result.IsEarlyDeparture = true;
                result.EarlyMinutes = earlyMinutes;
                result.ExceptionType = "EarlyDeparture";
                result.ExceptionDescription = $"Employee left {earlyMinutes} minutes early";
                result.HasException = true;
            }
        }

        return result;
    }
}
