using WACAMS.Core.Entities;

namespace WACAMS.Tests.TestData;

/// <summary>
/// Test data factory for AttendanceRecord — 30 days of records per employee.
/// Covers Present, Late, Early Leave, Absent, AWOL statuses with various shift assignments
/// and edge cases including midnight crossings and weekend work.
/// </summary>
public static class AttendanceFixtures
{
    /// <summary>
    /// Creates 30 days of attendance records for a single employee.
    /// </summary>
    public static List<AttendanceRecord> CreateEmployeeRecords(string employeeId, DateTime startDate)
    {
        var records = new List<AttendanceRecord>();
        var rng = new Random(42); // Fixed seed for reproducibility

        for (int dayOffset = 0; dayOffset < 30; dayOffset++)
        {
            var date = startDate.AddDays(dayOffset);
            var dayOfWeek = date.DayOfWeek;
            bool isWeekend = dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday;

            // Shift code assignment (rotating pattern)
            string shiftCode = dayOffset % 3 switch
            {
                0 => "ES", // Morning 06:00-14:00
                1 => "MS", // Afternoon 14:00-22:00
                2 => "NS", // Night 22:00-06:00
                _ => "ES"
            };

            var record = CreateRecordForShift(employeeId, date, shiftCode, rng, isWeekend);
            records.Add(record);
        }

        return records;
    }

    /// <summary>
    /// Creates a single attendance record based on shift type.
    /// </summary>
    private static AttendanceRecord CreateRecordForShift(
        string employeeId, DateTime date, string shiftCode, Random rng, bool isWeekend)
    {
        TimeSpan expectedClockIn, expectedClockOut;

        switch (shiftCode)
        {
            case "ES": // Morning 06:00-14:00
                expectedClockIn = TimeSpan.FromHours(6);
                expectedClockOut = TimeSpan.FromHours(14);
                break;
            case "MS": // Afternoon 14:00-22:00
                expectedClockIn = TimeSpan.FromHours(14);
                expectedClockOut = TimeSpan.FromHours(22);
                break;
            case "NS": // Night 22:00-06:00 (crosses midnight)
                expectedClockIn = TimeSpan.FromHours(22);
                expectedClockOut = TimeSpan.FromHours(6);
                break;
            default: // LDS - Long Day Shift 06:00-18:00
                expectedClockIn = TimeSpan.FromHours(6);
                expectedClockOut = TimeSpan.FromHours(18);
                break;
        }

        // Determine status distribution (weighted random)
        double roll = rng.NextDouble();
        AttendanceRecord record = new()
        {
            EmployeeId = employeeId,
            Date = date.Date,
            ShiftCode = isWeekend ? null : shiftCode,
            ExpectedClockIn = isWeekend ? null : expectedClockIn,
            ExpectedClockOut = isWeekend ? null : expectedClockOut,
            IsLate = false,
            MinutesLate = 0,
            IsEarlyDeparture = false,
            MinutesEarly = 0,
            CreatedAt = DateTime.UtcNow,
        };

        if (isWeekend)
        {
            // Weekend: mostly rest days, some work
            if (rng.NextDouble() > 0.7)
            {
                // Working weekend — normal present
                record.ClockIn = expectedClockIn + TimeSpan.FromMinutes(rng.Next(-5, 5));
                record.ClockOut = expectedClockOut + TimeSpan.FromMinutes(rng.Next(-5, 5));
                record.IsLate = record.ClockIn.Value > expectedClockIn.Add(TimeSpan.FromMinutes(10));
                record.MinutesLate = record.IsLate
                    ? (int)(record.ClockIn.Value - expectedClockIn.Add(TimeSpan.FromMinutes(10))).TotalMinutes
                    : 0;
                record.IsEarlyDeparture = record.ClockOut.Value < expectedClockOut;
                record.MinutesEarly = record.IsEarlyDeparture
                    ? (int)(expectedClockOut - record.ClockOut.Value).TotalMinutes
                    : 0;
            }
            else
            {
                record.Status = "Rest";
                record.ClockIn = null;
                record.ClockOut = null;
            }
        }
        else if (roll < 0.70)
        {
            // 70% — Present (on time or slight variance within grace period)
            int varianceSec = rng.Next(-300, 600); // -5min to +10min
            record.ClockIn = expectedClockIn + TimeSpan.FromSeconds(varianceSec);
            record.ClockOut = expectedClockOut + TimeSpan.FromMinutes(rng.Next(-2, 5));
            record.Status = "Present";
            record.IsLate = varianceSec > 600;
            record.MinutesLate = record.IsLate
                ? (int)(TimeSpan.FromSeconds(varianceSec) - TimeSpan.FromMinutes(1)).TotalMinutes
                : 0;
            record.IsEarlyDeparture = false;
            record.MinutesEarly = 0;
        }
        else if (roll < 0.82)
        {
            // 12% — Late arrival (15min to 90min late)
            int minutesLate = rng.Next(15, 90);
            record.ClockIn = expectedClockIn + TimeSpan.FromMinutes(minutesLate);
            record.ClockOut = expectedClockOut + TimeSpan.FromMinutes(rng.Next(-5, 5));
            record.Status = "Late";
            record.IsLate = true;
            record.MinutesLate = minutesLate;
            record.IsEarlyDeparture = false;
            record.MinutesEarly = 0;
        }
        else if (roll < 0.90)
        {
            // 8% — Early departure (stayed on time but left early)
            int minutesEarly = rng.Next(15, 60);
            record.ClockIn = expectedClockIn + TimeSpan.FromMinutes(rng.Next(-5, 5));
            record.ClockOut = expectedClockOut - TimeSpan.FromMinutes(minutesEarly);
            record.Status = "EarlyDeparture";
            record.IsLate = false;
            record.MinutesLate = 0;
            record.IsEarlyDeparture = true;
            record.MinutesEarly = minutesEarly;
        }
        else if (roll < 0.95)
        {
            // 5% — Absent (no clock in/out)
            record.ClockIn = null;
            record.ClockOut = null;
            record.Status = "Absent";
        }
        else if (roll < 0.98)
        {
            // 3% — AWOL (absent without notice)
            record.ClockIn = null;
            record.ClockOut = null;
            record.Status = "AWOL";
        }
        else
        {
            // 2% — Sick leave (with note)
            record.ClockIn = null;
            record.ClockOut = null;
            record.Status = "SickLeave";
        }

        return record;
    }

    /// <summary>
    /// Creates records for all 50 employees over 30 days.
    /// </summary>
    public static List<AttendanceRecord> CreateAllEmployeeRecords(DateTime startDate)
    {
        var allRecords = new List<AttendanceRecord>();
        var employees = EmployeeFixtures.CreateAllEmployees();

        foreach (var emp in employees)
        {
            var records = CreateEmployeeRecords(emp.EmployeeId, startDate);
            allRecords.AddRange(records);
        }

        return allRecords;
    }

    /// <summary>
    /// Creates edge-case attendance records for specific test scenarios.
    /// </summary>
    public static List<AttendanceRecord> CreateEdgeCaseRecords()
    {
        var records = new List<AttendanceRecord>();
        var baseDate = new DateTime(2026, 11, 1);

        // Edge Case 1: Midnight crossing — Night shift clock-in before midnight, clock-out after midnight
        records.Add(new AttendanceRecord
        {
            EmployeeId = "EMP001",
            Date = baseDate,
            ShiftCode = "NS",
            ClockIn = DateTime.Parse("2026-11-01 21:50").TimeOfDay,
            ClockOut = DateTime.Parse("2026-11-02 06:05").TimeOfDay,
            ExpectedClockIn = TimeSpan.FromHours(22),
            ExpectedClockOut = TimeSpan.FromHours(6),
            IsLate = false,
            MinutesLate = 0,
            IsEarlyDeparture = false,
            MinutesEarly = 0,
            Status = "Present",
        });

        // Edge Case 2: Very late arrival — 120 minutes late
        records.Add(new AttendanceRecord
        {
            EmployeeId = "EMP005",
            Date = baseDate.AddDays(1),
            ShiftCode = "ES",
            ClockIn = DateTime.Parse("2026-11-02 08:00").TimeOfDay,
            ClockOut = DateTime.Parse("2026-11-02 14:05").TimeOfDay,
            ExpectedClockIn = TimeSpan.FromHours(6),
            ExpectedClockOut = TimeSpan.FromHours(14),
            IsLate = true,
            MinutesLate = 120,
            IsEarlyDeparture = false,
            MinutesEarly = 0,
            Status = "Late",
        });

        // Edge Case 3: Very early departure — 90 minutes early
        records.Add(new AttendanceRecord
        {
            EmployeeId = "EMP006",
            Date = baseDate.AddDays(2),
            ShiftCode = "MS",
            ClockIn = DateTime.Parse("2026-11-03 13:55").TimeOfDay,
            ClockOut = DateTime.Parse("2026-11-03 20:30").TimeOfDay,
            ExpectedClockIn = TimeSpan.FromHours(14),
            ExpectedClockOut = TimeSpan.FromHours(22),
            IsLate = false,
            MinutesLate = 0,
            IsEarlyDeparture = true,
            MinutesEarly = 90,
            Status = "EarlyDeparture",
        });

        // Edge Case 4: Missing clock-out only
        records.Add(new AttendanceRecord
        {
            EmployeeId = "EMP010",
            Date = baseDate.AddDays(3),
            ShiftCode = "ES",
            ClockIn = DateTime.Parse("2026-11-04 05:58").TimeOfDay,
            ClockOut = null,
            ExpectedClockIn = TimeSpan.FromHours(6),
            ExpectedClockOut = TimeSpan.FromHours(14),
            IsLate = false,
            MinutesLate = 0,
            IsEarlyDeparture = false,
            MinutesEarly = 0,
            Status = "MissingClockOut",
        });

        // Edge Case 5: Perfect attendance — exactly on time
        records.Add(new AttendanceRecord
        {
            EmployeeId = "EMP011",
            Date = baseDate.AddDays(4),
            ShiftCode = "ES",
            ClockIn = TimeSpan.FromHours(6),
            ClockOut = TimeSpan.FromHours(14),
            ExpectedClockIn = TimeSpan.FromHours(6),
            ExpectedClockOut = TimeSpan.FromHours(14),
            IsLate = false,
            MinutesLate = 0,
            IsEarlyDeparture = false,
            MinutesEarly = 0,
            Status = "Present",
        });

        // Edge Case 6: Both late AND early departure on same day
        records.Add(new AttendanceRecord
        {
            EmployeeId = "EMP012",
            Date = baseDate.AddDays(5),
            ShiftCode = "MS",
            ClockIn = DateTime.Parse("2026-11-06 14:25").TimeOfDay,
            ClockOut = DateTime.Parse("2026-11-06 21:30").TimeOfDay,
            ExpectedClockIn = TimeSpan.FromHours(14),
            ExpectedClockOut = TimeSpan.FromHours(22),
            IsLate = true,
            MinutesLate = 25,
            IsEarlyDeparture = true,
            MinutesEarly = 30,
            Status = "Late",
        });

        return records;
    }
}
