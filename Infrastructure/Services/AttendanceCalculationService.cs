using WACAMS.Core.Constants;
using WACAMS.Core.Services;

namespace Infrastructure.Services;

/// <summary>
/// Domain service implementing IAttendanceCalculationService.
/// Encapsulates business rules for late/early detection, lost time computation,
/// and attendance percentage formulas. All methods are pure functions — no side effects.
/// </summary>
public class AttendanceCalculationService : IAttendanceCalculationService
{
    private const int GracePeriodMinutes = ShiftConstants.GracePeriodMinutes;

    /// <inheritdoc />
    public int CalculateLateMinutes(TimeSpan expectedClockIn, TimeSpan actualClockIn)
    {
        if (actualClockIn <= expectedClockIn)
            return 0;

        // Night shift: expectedClockIn is e.g. 22:00, actual could be 22:30 or 00:15 next day
        if (expectedClockIn > new TimeSpan(18, 0, 0))
        {
            // Night shift crossing midnight
            var minutesSinceStart = GetMinutesSinceMidnight(actualClockIn);
            var startMinutes = GetMinutesSinceMidnight(expectedClockIn);

            int nightDiff;
            if (minutesSinceStart >= startMinutes)
                nightDiff = minutesSinceStart - startMinutes;
            else
                nightDiff = (24 * 60 - startMinutes) + minutesSinceStart;

            return Math.Max(0, nightDiff - GracePeriodMinutes);
        }

        var diff = (actualClockIn - expectedClockIn).TotalMinutes;
        return Math.Max(0, (int)Math.Round(diff) - GracePeriodMinutes);
    }

    /// <inheritdoc />
    public int CalculateEarlyMinutes(TimeSpan expectedClockOut, TimeSpan actualClockOut)
    {
        if (actualClockOut >= expectedClockOut)
            return 0;

        // Night shift: end time is earlier than start (e.g., 06:00 < 22:00)
        if (expectedClockOut < new TimeSpan(12, 0, 0))
        {
            // Expected end is early morning (06:00), actual clock-out should be compared
            // If actual is before expected on the same "day"
            var expectedMinutes = expectedClockOut.TotalMinutes;
            var actualMinutes = actualClockOut.TotalMinutes;

            // For night shift, early departure means leaving before the expected end
            var nightDiff = expectedMinutes - actualMinutes;
            if (nightDiff < 0)
                nightDiff += 24 * 60; // wrap around

            return Math.Max(0, (int)Math.Round(nightDiff));
        }

        var diff = (expectedClockOut - actualClockOut).TotalMinutes;
        return Math.Max(0, (int)Math.Round(diff));
    }

    /// <inheritdoc />
    public decimal CalculateLostHours(int lateMinutes, int earlyMinutes)
    {
        var totalMinutes = lateMinutes + earlyMinutes;
        return Math.Round(totalMinutes / 60.0m, 2);
    }

    /// <inheritdoc />
    public decimal CalculateAttendancePercentage(int presentDays, int totalDays)
    {
        if (totalDays == 0)
            return 0m;

        return Math.Round(((decimal)presentDays / totalDays) * 100m, 2);
    }

    /// <inheritdoc />
    public decimal CalculateDepartmentAttendancePercentage(
        IEnumerable<(int PresentDays, int TotalDays)> departmentRecords,
        int totalEmployees)
    {
        var recordsList = departmentRecords.ToList();
        if (!recordsList.Any())
            return 0m;

        var totalPresentDays = recordsList.Sum(r => r.PresentDays);
        var totalWorkingDays = recordsList.Sum(r => r.TotalDays);

        if (totalWorkingDays == 0)
            return 0m;

        return Math.Round(((decimal)totalPresentDays / totalWorkingDays) * 100m, 2);
    }

    /// <summary>
    /// Helper: converts a TimeSpan to total minutes since midnight.
    /// Used for night shift calculations where times cross midnight boundaries.
    /// </summary>
    private static int GetMinutesSinceMidnight(TimeSpan timeSpan)
    {
        return timeSpan.Hours * 60 + timeSpan.Minutes;
    }
}
