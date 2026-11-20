using WACAMS.Core.Entities;

namespace WACAMS.Tests.TestData.Scenarios;

/// <summary>
/// Test scenarios for attendance anomaly detection patterns.
/// Covers perfect attendance, chronic lateness, AWOL patterns, and low department attendance.
/// </summary>
public static class AttendanceAnomalyScenario
{
    /// <summary>
    /// Scenario: Employee with PERFECT attendance (100% on-time, 30 days).
    /// 
    /// Used as a baseline/positive test case for anomaly detection algorithms.
    /// Every day: Present, clock-in within grace period, clock-out at or after expected time.
    /// </summary>
    public static List<AttendanceRecord> CreatePerfectAttendanceEmployee()
    {
        var records = new List<AttendanceRecord>();
        var empId = "EMP006"; // Clean operator
        var startDate = new DateTime(2026, 10, 1);

        for (int i = 0; i < 30; i++)
        {
            var date = startDate.AddDays(i);
            var dayOfWeek = date.DayOfWeek;

            // Skip weekends
            if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                continue;

            string shiftCode = i % 3 switch
            {
                0 => "ES",
                1 => "MS",
                _ => "NS"
            };

            TimeSpan expectedIn, expectedOut;
            switch (shiftCode)
            {
                case "ES": expectedIn = TimeSpan.FromHours(6); expectedOut = TimeSpan.FromHours(14); break;
                case "MS": expectedIn = TimeSpan.FromHours(14); expectedOut = TimeSpan.FromHours(22); break;
                default: expectedIn = TimeSpan.FromHours(22); expectedOut = TimeSpan.FromHours(6); break;
            }

            // Perfect employee: always within 5 minutes of start, stays until end
            int earlyArrivalMinutes = Random.Shared.Next(0, 5); // Arrives 0-5 min early
            int lateStayMinutes = Random.Shared.Next(0, 10); // Stays 0-10 min extra

            records.Add(new AttendanceRecord
            {
                EmployeeId = empId,
                Date = date.Date,
                ShiftCode = shiftCode,
                ClockIn = expectedIn - TimeSpan.FromMinutes(earlyArrivalMinutes),
                ClockOut = expectedOut + TimeSpan.FromMinutes(lateStayMinutes),
                ExpectedClockIn = expectedIn,
                ExpectedClockOut = expectedOut,
                IsLate = false,
                MinutesLate = 0,
                IsEarlyDeparture = false,
                MinutesEarly = 0,
                Status = "Present",
                CreatedAt = DateTime.UtcNow,
            });
        }

        return records;
    }

    /// <summary>
    /// Scenario: Employee with CHRONIC lateness (20 late arrivals out of 30 workdays).
    /// 
    /// Pattern: Arrives 15-90 minutes late consistently, sometimes early departure too.
    /// This should trigger progressive discipline workflow.
    /// </summary>
    public static List<AttendanceRecord> CreateChronicLateEmployee()
    {
        var records = new List<AttendanceRecord>();
        var empId = "EMP005"; // Operator with warnings
        var startDate = new DateTime(2026, 10, 1);
        var rng = new Random(123);

        for (int i = 0; i < 30; i++)
        {
            var date = startDate.AddDays(i);
            var dayOfWeek = date.DayOfWeek;

            if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                continue;

            string shiftCode = i % 3 switch
            {
                0 => "ES",
                1 => "MS",
                _ => "NS"
            };

            TimeSpan expectedIn, expectedOut;
            switch (shiftCode)
            {
                case "ES": expectedIn = TimeSpan.FromHours(6); expectedOut = TimeSpan.FromHours(14); break;
                case "MS": expectedIn = TimeSpan.FromHours(14); expectedOut = TimeSpan.FromHours(22); break;
                default: expectedIn = TimeSpan.FromHours(22); expectedOut = TimeSpan.FromHours(6); break;
            }

            bool isLateDay = rng.Next(30) < 20; // 20/30 days are late
            bool isEarlyDay = !isLateDay && rng.Next(4) == 0; // 1 in 4 non-late days is early departure

            AttendanceRecord record;

            if (isLateDay)
            {
                int lateMinutes = rng.Next(15, 90);
                record = new AttendanceRecord
                {
                    EmployeeId = empId,
                    Date = date.Date,
                    ShiftCode = shiftCode,
                    ClockIn = expectedIn + TimeSpan.FromMinutes(lateMinutes),
                    ClockOut = expectedOut + TimeSpan.FromMinutes(rng.Next(-5, 5)),
                    ExpectedClockIn = expectedIn,
                    ExpectedClockOut = expectedOut,
                    IsLate = true,
                    MinutesLate = lateMinutes,
                    IsEarlyDeparture = false,
                    MinutesEarly = 0,
                    Status = "Late",
                    CreatedAt = DateTime.UtcNow,
                };
            }
            else if (isEarlyDay)
            {
                int earlyMinutes = rng.Next(15, 60);
                record = new AttendanceRecord
                {
                    EmployeeId = empId,
                    Date = date.Date,
                    ShiftCode = shiftCode,
                    ClockIn = expectedIn + TimeSpan.FromMinutes(rng.Next(0, 5)),
                    ClockOut = expectedOut - TimeSpan.FromMinutes(earlyMinutes),
                    ExpectedClockIn = expectedIn,
                    ExpectedClockOut = expectedOut,
                    IsLate = false,
                    MinutesLate = 0,
                    IsEarlyDeparture = true,
                    MinutesEarly = earlyMinutes,
                    Status = "EarlyDeparture",
                    CreatedAt = DateTime.UtcNow,
                };
            }
            else
            {
                record = new AttendanceRecord
                {
                    EmployeeId = empId,
                    Date = date.Date,
                    ShiftCode = shiftCode,
                    ClockIn = expectedIn + TimeSpan.FromMinutes(rng.Next(0, 8)),
                    ClockOut = expectedOut + TimeSpan.FromMinutes(rng.Next(-3, 5)),
                    ExpectedClockIn = expectedIn,
                    ExpectedClockOut = expectedOut,
                    IsLate = false,
                    MinutesLate = 0,
                    IsEarlyDeparture = false,
                    MinutesEarly = 0,
                    Status = "Present",
                    CreatedAt = DateTime.UtcNow,
                };
            }

            records.Add(record);
        }

        return records;
    }

    /// <summary>
    /// Scenario: Employee with AWOL pattern (5 consecutive absences without notice).
    /// 
    /// This is a critical pattern that should trigger immediate escalation to HR.
    /// All 5 days show Absent/AWOL status with no clock-in/clock-out data.
    /// </summary>
    public static List<AttendanceRecord> CreateAwolPatternEmployee()
    {
        var records = new List<AttendanceRecord>();
        var empId = "EMP020";
        var startDate = new DateTime(2026, 11, 2); // Monday

        // First 3 days: normal attendance (to establish baseline)
        for (int i = 0; i < 3; i++)
        {
            var date = startDate.AddDays(i);
            records.Add(new AttendanceRecord
            {
                EmployeeId = empId,
                Date = date.Date,
                ShiftCode = i % 3 == 0 ? "ES" : "MS",
                ClockIn = TimeSpan.FromHours(6),
                ClockOut = TimeSpan.FromHours(14),
                ExpectedClockIn = TimeSpan.FromHours(6),
                ExpectedClockOut = TimeSpan.FromHours(14),
                IsLate = false,
                MinutesLate = 0,
                IsEarlyDeparture = false,
                MinutesEarly = 0,
                Status = "Present",
                CreatedAt = DateTime.UtcNow,
            });
        }

        // Next 5 days: AWOL — no clock-in, no clock-out
        for (int i = 3; i < 8; i++)
        {
            var date = startDate.AddDays(i);
            records.Add(new AttendanceRecord
            {
                EmployeeId = empId,
                Date = date.Date,
                ShiftCode = (i + 1) % 3 == 0 ? "ES" : "NS",
                ClockIn = null,
                ClockOut = null,
                ExpectedClockIn = i >= 5 ? TimeSpan.FromHours(6) : TimeSpan.FromHours(14),
                ExpectedClockOut = i >= 5 ? TimeSpan.FromHours(14) : TimeSpan.FromHours(22),
                IsLate = false,
                MinutesLate = 0,
                IsEarlyDeparture = false,
                MinutesEarly = 0,
                Status = i == 6 ? "AWOL" : "Absent", // Day 6: AWOL (no contact), Days 3-5: Absent
                CreatedAt = DateTime.UtcNow,
            });
        }

        // Remaining days: back to work (after manager intervention)
        for (int i = 8; i < 22; i++) // ~14 more work days
        {
            var date = startDate.AddDays(i);
            var dayOfWeek = date.DayOfWeek;
            if (dayOfWeek == DayOfWeek.Saturday || dayOfWeek == DayOfWeek.Sunday)
                continue;

            records.Add(new AttendanceRecord
            {
                EmployeeId = empId,
                Date = date.Date,
                ShiftCode = i % 3 == 0 ? "ES" : "MS",
                ClockIn = TimeSpan.FromHours(6),
                ClockOut = TimeSpan.FromHours(14),
                ExpectedClockIn = TimeSpan.FromHours(6),
                ExpectedClockOut = TimeSpan.FromHours(14),
                IsLate = false,
                MinutesLate = 0,
                IsEarlyDeparture = false,
                MinutesEarly = 0,
                Status = "Present",
                CreatedAt = DateTime.UtcNow,
            });
        }

        return records;
    }

    /// <summary>
    /// Scenario: Department with LOW attendance rate (~70%).
    /// 
    /// Simulates a department where 30 out of ~40 employees are present each day,
    /// with high rates of lateness and absenteeism. Used for testing department-level KPIs
    /// and risk level calculations (should be flagged as "high" or "critical").
    /// </summary>
    public static Dictionary<string, DepartmentAttendanceStats> CreateLowAttendanceDepartment()
    {
        var stats = new Dictionary<string, DepartmentAttendanceStats>();
        var rng = new Random(456);
        var startDate = new DateTime(2026, 11, 1);

        // Production department: 40 active employees
        for (int empIdx = 1; empIdx <= 40; empIdx++)
        {
            var empId = $"EMP{empIdx:D3}";
            int presentDays = 0;
            int lateDays = 0;
            int absentDays = 0;
            int totalLateMinutes = 0;
            int totalEarlyMinutes = 0;

            for (int day = 0; day < 22; day++) // ~22 work days in November
            {
                double roll = rng.NextDouble();
                if (roll < 0.65)
                {
                    // 65% — Present (some late, some on time)
                    presentDays++;
                    if (rng.NextDouble() < 0.3)
                    {
                        lateDays++;
                        int lateMin = rng.Next(10, 60);
                        totalLateMinutes += lateMin;
                    }
                }
                else if (roll < 0.80)
                {
                    // 15% — Late only
                    presentDays++;
                    lateDays++;
                    totalLateMinutes += rng.Next(15, 90);
                }
                else if (roll < 0.90)
                {
                    // 10% — Absent
                    absentDays++;
                }
                else if (roll < 0.95)
                {
                    // 5% — AWOL
                    absentDays++;
                }
                else
                {
                    // 5% — Sick leave / approved leave
                    presentDays++; // counts as present for attendance % purposes
                }
            }

            stats[empId] = new DepartmentAttendanceStats
            {
                EmployeeId = empId,
                TotalWorkDays = 22,
                PresentDays = presentDays,
                LateDays = lateDays,
                AbsentDays = absentDays,
                AttendanceRate = Math.Round(presentDays / (double)22 * 100, 1),
                TotalLateMinutes = totalLateMinutes,
                TotalEarlyMinutes = totalEarlyMinutes,
                AvgLateMinutes = lateDays > 0 ? Math.Round(totalLateMinutes / (double)lateDays, 1) : 0,
            };
        }

        return stats;
    }
}

/// <summary>
/// Per-employee attendance statistics for a given period.
/// </summary>
public record DepartmentAttendanceStats
{
    public string EmployeeId { get; init; } = null!;
    public int TotalWorkDays { get; init; }
    public int PresentDays { get; init; }
    public int LateDays { get; init; }
    public int AbsentDays { get; init; }
    public decimal AttendanceRate { get; init; }
    public int TotalLateMinutes { get; init; }
    public int TotalEarlyMinutes { get; init; }
    public decimal AvgLateMinutes { get; init; }
}
