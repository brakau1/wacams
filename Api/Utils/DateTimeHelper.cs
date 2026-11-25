namespace WACAMS.Api.Utils;

/// <summary>
/// Timezone-aware date calculations and shift time boundary helpers.
/// </summary>
public static class DateTimeHelper
{
    private const string DefaultTimeZoneId = "Africa/Johannesburg";

    /// <summary>
    /// Get current time in South Africa timezone (SAST).
    /// </summary>
    public static DateTime NowInSouthAfrica()
    {
        var tz = TimeZoneInfo.FindSystemTimeZoneById(DefaultTimeZoneId);
        return TimeZoneInfo.ConvertTimeFromUtc(DateTime.UtcNow, tz);
    }

    /// <summary>
    /// Get the start of a given day in SAST.
    /// </summary>
    public static DateTime StartOfDayInSast(DateTime date)
    {
        return date.Date; // Already in SAST from NowInSouthAfrica()
    }

    /// <summary>
    /// Determine which shift a given datetime falls into.
    /// Returns: "Morning" (06:00-14:00), "Afternoon" (14:00-22:00), "Night" (22:00-06:00).
    /// </summary>
    public static string GetShiftName(DateTime dateTime)
    {
        var hour = dateTime.Hour;
        if (hour >= 6 && hour < 14)
            return "Morning";
        if (hour >= 14 && hour < 22)
            return "Afternoon";
        return "Night";
    }

    /// <summary>
    /// Get expected clock-in time for a given shift name.
    /// </summary>
    public static TimeSpan GetShiftStartTime(string shiftName)
    {
        return shiftName.ToLowerInvariant() switch
        {
            "morning" => new TimeSpan(6, 0, 0),
            "afternoon" => new TimeSpan(14, 0, 0),
            "night" => new TimeSpan(22, 0, 0),
            _ => new TimeSpan(6, 0, 0) // Default to morning
        };
    }

    /// <summary>
    /// Get expected clock-out time for a given shift name.
    /// </summary>
    public static TimeSpan GetShiftEndTime(string shiftName)
    {
        return shiftName.ToLowerInvariant() switch
        {
            "morning" => new TimeSpan(14, 0, 0),
            "afternoon" => new TimeSpan(22, 0, 0),
            "night" => new TimeSpan(6, 0, 0),
            _ => new TimeSpan(14, 0, 0)
        };
    }

    /// <summary>
    /// Calculate minutes late based on actual clock-in vs expected shift start.
    /// Handles overnight shifts correctly.
    /// </summary>
    public static int CalculateMinutesLate(TimeSpan? actualClockIn, TimeSpan expectedStart)
    {
        if (!actualClockIn.HasValue)
            return 0;

        var actual = actualClockIn.Value;

        // Night shift special handling
        if (expectedStart == new TimeSpan(22, 0, 0))
        {
            // If clocked in between 22:00-08:00, calculate against 22:00 start
            if (actual >= new TimeSpan(22, 0, 0))
                return Math.Max(0, (int)(actual - expectedStart).TotalMinutes);
            if (actual <= new TimeSpan(8, 0, 0))
                return Math.Max(0, (int)((actual + TimeSpan.FromDays(1)) - expectedStart).TotalMinutes);
            return 0;
        }

        return Math.Max(0, (int)(actual - expectedStart).TotalMinutes);
    }

    /// <summary>
    /// Calculate minutes early departure based on actual clock-out vs expected shift end.
    /// </summary>
    public static int CalculateMinutesEarly(TimeSpan? actualClockOut, TimeSpan expectedEnd)
    {
        if (!actualClockOut.HasValue)
            return 0;

        var actual = actualClockOut.Value;

        // Night shift special handling — end is 06:00 next day
        if (expectedEnd == new TimeSpan(6, 0, 0))
        {
            if (actual <= new TimeSpan(8, 0, 0))
                return Math.Max(0, (int)((expectedEnd + TimeSpan.FromDays(1)) - actual).TotalMinutes);
            return 0;
        }

        return Math.Max(0, (int)(expectedEnd - actual).TotalMinutes);
    }

    /// <summary>
    /// Get all dates in a date range (inclusive).
    /// </summary>
    public static IEnumerable<DateTime> GetDatesInRange(DateTime startDate, DateTime endDate)
    {
        var current = startDate.Date;
        while (current <= endDate.Date)
        {
            yield return current;
            current = current.AddDays(1);
        }
    }

    /// <summary>
    /// Check if a date is a weekday (Mon-Fri).
    /// </summary>
    public static bool IsWeekDay(DateTime date)
    {
        return date.DayOfWeek != DayOfWeek.Saturday && date.DayOfWeek != DayOfWeek.Sunday;
    }

    /// <summary>
    /// Add business days to a date (skip weekends).
    /// </summary>
    public static DateTime AddBusinessDays(DateTime startDate, int days)
    {
        var result = startDate;
        var direction = days >= 0 ? 1 : -1;
        var absDays = Math.Abs(days);

        for (int i = 0; i < absDays; i++)
        {
            do
            {
                result = result.AddDays(direction);
            } while (!IsWeekDay(result));
        }

        return result;
    }
}
