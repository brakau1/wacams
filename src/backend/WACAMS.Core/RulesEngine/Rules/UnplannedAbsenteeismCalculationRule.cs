using WACAMS.Core.RulesEngine.Models;

namespace WACAMS.Core.RulesEngine.Rules;

/// <summary>
/// Rule for detecting unplanned absenteeism
/// </summary>
public class UnplannedAbsenteeismCalculationRule : IAttendanceCalculationRule
{
    public string RuleName => "UnplannedAbsenteeismCalculation";
    public string Description => "Detects unplanned absences and calculates lost production time";

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
            ScheduledEnd = context.ScheduledEnd
        };

        if (!CanCalculate(context))
        {
            return result;
        }

        // Check if shift was cancelled
        if (context.IsShiftCancelled)
        {
            result.IsAbsent = false;
            result.ExceptionType = "ShiftCancellation";
            result.ExceptionDescription = "Shift was officially cancelled";
            result.HasException = false; // Not an exception if cancelled
            return result;
        }

        // Check if employee is on approved leave
        if (context.IsOnLeave)
        {
            result.IsAbsent = false;
            result.ExceptionType = $"Leave-{context.LeaveType}";
            result.ExceptionDescription = $"Employee on approved {context.LeaveType}";
            result.HasException = false; // Not an exception if on approved leave
            return result;
        }

        // Check for unplanned absence (no clock in and no clock out)
        if (!context.ActualStart.HasValue && !context.ActualEnd.HasValue)
        {
            result.IsAbsent = true;
            
            // Calculate full shift duration as lost time
            var shiftDuration = context.ScheduledEnd.Value - context.ScheduledStart.Value;
            result.LostMinutes = (int)shiftDuration.TotalMinutes;
            
            result.ExceptionType = "UnplannedAbsenteeism";
            result.ExceptionDescription = $"Employee was absent without approval. Lost {result.LostMinutes} minutes of production time";
            result.HasException = true;
        }

        return result;
    }
}
