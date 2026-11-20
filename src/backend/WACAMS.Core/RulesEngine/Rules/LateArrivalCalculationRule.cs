using WACAMS.Core.RulesEngine.Models;

namespace WACAMS.Core.RulesEngine.Rules;

/// <summary>
/// Rule for calculating late arrival
/// </summary>
public class LateArrivalCalculationRule : IAttendanceCalculationRule
{
    public string RuleName => "LateArrivalCalculation";
    public string Description => "Calculates late arrival time and determines if employee is late";

    public bool CanCalculate(AttendanceCalculationContext context)
    {
        return context.ScheduledStart.HasValue && context.ActualStart.HasValue;
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

        // Calculate late minutes
        if (context.ActualStart > context.ScheduledStart)
        {
            var lateTime = context.ActualStart.Value - context.ScheduledStart.Value;
            var lateMinutes = (int)lateTime.TotalMinutes;

            // Apply grace period
            if (lateMinutes > context.LateGraceMinutes)
            {
                result.IsLate = true;
                result.LateMinutes = lateMinutes;
                result.ExceptionType = "LateArrival";
                result.ExceptionDescription = $"Employee arrived {lateMinutes} minutes late";
                result.HasException = true;
            }
        }

        return result;
    }
}
