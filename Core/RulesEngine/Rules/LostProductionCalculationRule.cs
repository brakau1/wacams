using WACAMS.Core.RulesEngine.Models;

namespace WACAMS.Core.RulesEngine.Rules;

/// <summary>
/// Rule for calculating lost production time
/// </summary>
public class LostProductionCalculationRule : IAttendanceCalculationRule
{
    public string RuleName => "LostProductionCalculation";
    public string Description => "Calculates lost production minutes and hours from attendance exceptions";

    public bool CanCalculate(AttendanceCalculationContext context)
    {
        return context.ScheduledStart.HasValue && context.ScheduledEnd.HasValue;
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

        int lostMinutes = 0;

        // Calculate lost time from late arrival
        if (context.ActualStart.HasValue && context.ActualStart > context.ScheduledStart)
        {
            var lateTime = context.ActualStart.Value - context.ScheduledStart.Value;
            lostMinutes += (int)lateTime.TotalMinutes;
            result.LateMinutes = (int)lateTime.TotalMinutes;
            result.IsLate = result.LateMinutes > context.LateGraceMinutes;
        }

        // Calculate lost time from early departure
        if (context.ActualEnd.HasValue && context.ActualEnd < context.ScheduledEnd)
        {
            var earlyTime = context.ScheduledEnd.Value - context.ActualEnd.Value;
            lostMinutes += (int)earlyTime.TotalMinutes;
            result.EarlyMinutes = (int)earlyTime.TotalMinutes;
            result.IsEarlyDeparture = result.EarlyMinutes > context.EarlyGraceMinutes;
        }

        // Calculate lost time from missing clock in
        if (!context.ActualStart.HasValue && context.ScheduledStart.HasValue)
        {
            // Assume entire shift is lost if no clock in
            var shiftDuration = context.ScheduledEnd.Value - context.ScheduledStart.Value;
            lostMinutes = (int)shiftDuration.TotalMinutes;
            result.ExceptionType = "MissingClockIn";
            result.ExceptionDescription = "No clock-in record found";
            result.HasException = true;
        }

        // Calculate lost time from missing clock out
        if (context.ActualStart.HasValue && !context.ActualEnd.HasValue && context.ScheduledEnd.HasValue)
        {
            // Calculate from actual start to scheduled end
            var partialShift = context.ScheduledEnd.Value - context.ActualStart.Value;
            lostMinutes = (int)partialShift.TotalMinutes;
            result.ExceptionType = "MissingClockOut";
            result.ExceptionDescription = "No clock-out record found";
            result.HasException = true;
        }

        result.LostMinutes = lostMinutes;
        
        // Determine if this is an exception
        if (result.IsLate || result.IsEarlyDeparture || lostMinutes > 0)
        {
            result.HasException = true;
            if (string.IsNullOrEmpty(result.ExceptionType))
            {
                result.ExceptionType = result.IsLate ? "LateArrival" : "EarlyDeparture";
                result.ExceptionDescription = result.IsLate 
                    ? $"Employee arrived {result.LateMinutes} minutes late"
                    : $"Employee left {result.EarlyMinutes} minutes early";
            }
        }

        return result;
    }
}
