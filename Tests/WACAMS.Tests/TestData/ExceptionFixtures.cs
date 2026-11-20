using WACAMS.Core.Entities;

namespace WACAMS.Tests.TestData;

/// <summary>
/// Test data factory for AttendanceException entities.
/// Covers LateArrival, UnplannedAbsenteeism, MissingClockOut types with various statuses
/// (Reported/Pending, Investigated, Closed) and exceptions with/without linked warnings.
/// </summary>
public static class ExceptionFixtures
{
    /// <summary>
    /// Creates a set of attendance exceptions for progressive discipline testing.
    /// Simulates an employee accumulating exceptions that trigger escalating warnings.
    /// </summary>
    public static List<AttendanceException> CreateProgressiveExceptions(string employeeId)
    {
        var now = DateTime.UtcNow;

        return new List<AttendanceException>
        {
            // Exception 1 — Late Arrival (3rd offence → should trigger WrittenWarning)
            new()
            {
                Id = 1,
                EmployeeId = employeeId,
                Date = now.AddDays(-30),
                ExceptionType = "LateArrival",
                ExpectedClockIn = TimeSpan.FromHours(6),
                ActualClockIn = TimeSpan.Parse("06:25"),
                ExpectedClockOut = TimeSpan.FromHours(14),
                ActualClockOut = TimeSpan.FromHours(14),
                MinutesLate = 25,
                MinutesEarly = 0,
                LostProductionMinutes = 25,
                Status = "Closed",
                InvestigationNotes = "Employee confirmed late arrival due to traffic delay on N1 highway.",
                InvestigatedBy = "EMP002",
                InvestigatedAt = now.AddDays(-28),
            },

            // Exception 2 — Late Arrival (4th offence → should trigger FinalWrittenWarning)
            new()
            {
                Id = 2,
                EmployeeId = employeeId,
                Date = now.AddDays(-15),
                ExceptionType = "LateArrival",
                ExpectedClockIn = TimeSpan.FromHours(6),
                ActualClockIn = TimeSpan.Parse("07:10"),
                ExpectedClockOut = TimeSpan.FromHours(14),
                ActualClockOut = TimeSpan.FromHours(14),
                MinutesLate = 70,
                MinutesEarly = 0,
                LostProductionMinutes = 70,
                Status = "Investigated",
                InvestigationNotes = "Second late arrival within 90 days despite previous counselling and verbal warning.",
                InvestigatedBy = "EMP002",
                InvestigatedAt = now.AddDays(-14),
            },

            // Exception 3 — Missing Clock Out (pending investigation)
            new()
            {
                Id = 3,
                EmployeeId = employeeId,
                Date = now.AddDays(-3),
                ExceptionType = "MissingClockOut",
                ExpectedClockIn = TimeSpan.FromHours(6),
                ActualClockIn = TimeSpan.Parse("05:55"),
                ExpectedClockOut = TimeSpan.FromHours(14),
                ActualClockOut = null,
                MinutesLate = 0,
                MinutesEarly = 0,
                LostProductionMinutes = 0,
                Status = "Pending",
                InvestigationNotes = null,
                InvestigatedBy = null,
                InvestigatedAt = null,
            },
        };
    }

    /// <summary>
    /// Creates exceptions that are NOT yet investigated — used to test blocking of warning generation.
    /// </summary>
    public static List<AttendanceException> CreateUninvestigatedExceptions(string employeeId)
    {
        var now = DateTime.UtcNow;

        return new List<AttendanceException>
        {
            // Pending exception — cannot generate warning until investigated
            new()
            {
                Id = 100,
                EmployeeId = employeeId,
                Date = now.AddDays(-5),
                ExceptionType = "UnplannedAbsenteeism",
                ExpectedClockIn = TimeSpan.FromHours(14),
                ActualClockIn = null,
                ExpectedClockOut = TimeSpan.FromHours(22),
                ActualClockOut = null,
                MinutesLate = 0,
                MinutesEarly = 0,
                LostProductionMinutes = 480,
                Status = "Pending",
                InvestigationNotes = null,
                InvestigatedBy = null,
                InvestigatedAt = null,
            },

            // Open exception — also cannot generate warning
            new()
            {
                Id = 101,
                EmployeeId = employeeId,
                Date = now.AddDays(-2),
                ExceptionType = "LateArrival",
                ExpectedClockIn = TimeSpan.FromHours(6),
                ActualClockIn = TimeSpan.Parse("06:45"),
                ExpectedClockOut = TimeSpan.FromHours(14),
                ActualClockOut = TimeSpan.Parse("14:10"),
                MinutesLate = 45,
                MinutesEarly = 0,
                LostProductionMinutes = 45,
                Status = "Open",
                InvestigationNotes = null,
                InvestigatedBy = null,
                InvestigatedAt = null,
            },
        };
    }

    /// <summary>
    /// Creates a set of AWOL exceptions for pattern detection testing.
    /// </summary>
    public static List<AttendanceException> CreateAwolPatternExceptions(string employeeId)
    {
        var exceptions = new List<AttendanceException>();

        for (int i = 1; i <= 5; i++)
        {
            exceptions.Add(new AttendanceException
            {
                Id = 200 + i,
                EmployeeId = employeeId,
                Date = DateTime.UtcNow.AddDays(-(5 - i)),
                ExceptionType = "AWOL",
                ExpectedClockIn = TimeSpan.FromHours(6),
                ActualClockIn = null,
                ExpectedClockOut = TimeSpan.FromHours(14),
                ActualClockOut = null,
                MinutesLate = 0,
                MinutesEarly = 0,
                LostProductionMinutes = 480,
                Status = i >= 3 ? "Investigated" : "Pending",
                InvestigationNotes = i >= 3 ? $"Consecutive AWOL day {i}. No contact received from employee." : null,
                InvestigatedBy = i >= 3 ? "EMP002" : null,
                InvestigatedAt = i >= 3 ? DateTime.UtcNow.AddDays(-(5 - i) + 1) : null,
            });
        }

        return exceptions;
    }

    /// <summary>
    /// Creates a complete set of exceptions covering all test scenarios.
    /// </summary>
    public static Dictionary<string, List<AttendanceException>> CreateAllExceptions()
    {
        var all = new Dictionary<string, List<AttendanceException>>();

        // Progressive discipline chain (EMP005)
        all["EMP005"] = CreateProgressiveExceptions("EMP005");

        // Uninvestigated exceptions (EMP010) — should block warning generation
        all["EMP010"] = CreateUninvestigatedExceptions("EMP010");

        // AWOL pattern (EMP020) — 5 consecutive absences
        all["EMP020"] = CreateAwolPatternExceptions("EMP020");

        // Clean employee (no exceptions)
        all["EMP006"] = new List<AttendanceException>();

        return all;
    }
}
