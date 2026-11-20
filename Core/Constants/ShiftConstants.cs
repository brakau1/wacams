namespace WACAMS.Core.Constants;

/// <summary>
/// Contains shift time definitions, codes, and grace period configuration used across the WACAMS system.
/// All shift times are expressed as TimeSpan values for easy arithmetic operations.
/// </summary>
public static class ShiftConstants
{
    /// <summary>
    /// Grace period in minutes before a late arrival is counted.
    /// Employees clocking in within this window of their expected start time are considered on time.
    /// Set to 10 minutes per organizational policy.
    /// </summary>
    public const int GracePeriodMinutes = 10;

    /// <summary>
    /// Early/Morning shift definition.
    /// Start: 06:00, End: 14:00 (8 hours).
    /// Code "ES" matches iWork scheduling module convention.
    /// </summary>
    public static readonly (string Code, string Name, TimeSpan StartTime, TimeSpan EndTime) Morning =
        ("ES", "Early Shift / Morning", new TimeSpan(6, 0, 0), new TimeSpan(14, 0, 0));

    /// <summary>
    /// Middle/Afternoon shift definition.
    /// Start: 14:00, End: 22:00 (8 hours).
    /// Code "MS" matches iWork scheduling module convention.
    /// </summary>
    public static readonly (string Code, string Name, TimeSpan StartTime, TimeSpan EndTime) Afternoon =
        ("MS", "Middle Shift / Afternoon", new TimeSpan(14, 0, 0), new TimeSpan(22, 0, 0));

    /// <summary>
    /// Night shift definition.
    /// Start: 22:00, End: 06:00 next day (8 hours, crosses midnight).
    /// Code "NS" matches iWork scheduling module convention.
    /// Note: EndTime is earlier than StartTime because the shift spans two calendar days.
    /// </summary>
    public static readonly (string Code, string Name, TimeSpan StartTime, TimeSpan EndTime) Night =
        ("NS", "Night Shift", new TimeSpan(22, 0, 0), new TimeSpan(6, 0, 0));

    /// <summary>
    /// Long Day shift definition.
    /// Start: 06:00, End: 18:00 (12 hours, typically used for weekends or special operations).
    /// Code "LDS" matches iWork scheduling module convention.
    /// </summary>
    public static readonly (string Code, string Name, TimeSpan StartTime, TimeSpan EndTime) LongDay =
        ("LDS", "Long Day Shift", new TimeSpan(6, 0, 0), new TimeSpan(18, 0, 0));

    /// <summary>
    /// Standard Day shift definition.
    /// Start: 08:00, End: 17:00 (9 hours including 1-hour unpaid break, 8 paid hours).
    /// Code "DS" matches iWork scheduling module convention.
    /// Typically used for office and administrative staff.
    /// </summary>
    public static readonly (string Code, string Name, TimeSpan StartTime, TimeSpan EndTime) Day =
        ("DS", "Day Shift", new TimeSpan(8, 0, 0), new TimeSpan(17, 0, 0));

    /// <summary>
    /// Custom/Dynamic shift — variable start and end times set per schedule entry.
    /// Code "zdy" matches iWork scheduling module convention.
    /// Used when no predefined shift template applies.
    /// </summary>
    public const string CustomDynamic = "zdy";

    /// <summary>
    /// Returns the shift definition tuple by its code string.
    /// Useful for resolving shift codes from iWork/GIS data into structured objects.
    /// </summary>
    /// <param name="code">The shift code string (e.g., "ES", "MS", "NS", "LDS", "DS", "zdy").</param>
    /// <returns>A tuple containing code, name, start time, and end time.</returns>
    /// <exception cref="ArgumentException">Thrown when the code does not match any known shift.</exception>
    public static (string Code, string Name, TimeSpan StartTime, TimeSpan EndTime) FromCode(string code)
    {
        return code.ToUpperInvariant() switch
        {
            "ES" => Morning,
            "MS" => Afternoon,
            "NS" => Night,
            "LDS" => LongDay,
            "DS" => Day,
            _ => throw new ArgumentException($"Unknown shift code: {code}", nameof(code))
        };
    }

    /// <summary>
    /// All valid shift codes recognized by the system.
    /// Used for validation when creating or updating ShiftSchedule entries.
    /// </summary>
    public static readonly string[] ValidCodes = ["ES", "MS", "NS", "LDS", "DS", "zdy"];

    /// <summary>
    /// Standard daily working hours in minutes (8 hours × 60).
    /// Used as baseline for calculating overtime and lost production percentages.
    /// </summary>
    public const int StandardDailyWorkingMinutes = 480;

    /// <summary>
    /// Standard daily working hours (8 hours).
    /// </summary>
    public const double StandardDailyWorkingHours = 8.0;
}
