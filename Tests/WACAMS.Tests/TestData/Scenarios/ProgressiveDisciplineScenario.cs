using WACAMS.Core.Entities;

namespace WACAMS.Tests.TestData.Scenarios;

/// <summary>
/// Test scenarios for progressive discipline escalation through all warning levels.
/// Each scenario models an employee accumulating exceptions that trigger escalating warnings.
/// </summary>
public static class ProgressiveDisciplineScenario
{
    /// <summary>
    /// Scenario: Employee with 3 late arrivals → Counselling
    /// 
    /// Expected outcome: First counselling session issued. No further disciplinary action.
    /// This is the initial step — informal corrective action.
    /// </summary>
    public static ScenarioResult CreateFirstCounsellingScenario()
    {
        var emp = EmployeeFixtures.GetCleanOperator(); // EMP006 — no prior warnings

        var exceptions = new List<AttendanceException>
        {
            new()
            {
                Id = 1,
                EmployeeId = emp.EmployeeId,
                Date = DateTime.UtcNow.AddDays(-10),
                ExceptionType = "LateArrival",
                ExpectedClockIn = TimeSpan.FromHours(6),
                ActualClockIn = TimeSpan.Parse("06:20"),
                ExpectedClockOut = TimeSpan.FromHours(14),
                ActualClockOut = TimeSpan.FromHours(14),
                MinutesLate = 20,
                MinutesEarly = 0,
                LostProductionMinutes = 20,
                Status = "Investigated",
                InvestigationNotes = "Traffic delay on N1 highway.",
                InvestigatedBy = "EMP002",
                InvestigatedAt = DateTime.UtcNow.AddDays(-9),
            },
            new()
            {
                Id = 2,
                EmployeeId = emp.EmployeeId,
                Date = DateTime.UtcNow.AddDays(-8),
                ExceptionType = "LateArrival",
                ExpectedClockIn = TimeSpan.FromHours(6),
                ActualClockIn = TimeSpan.Parse("06:15"),
                ExpectedClockOut = TimeSpan.FromHours(14),
                ActualClockOut = TimeSpan.FromHours(14),
                MinutesLate = 15,
                MinutesEarly = 0,
                LostProductionMinutes = 15,
                Status = "Investigated",
                InvestigationNotes = "Bus was delayed.",
                InvestigatedBy = "EMP002",
                InvestigatedAt = DateTime.UtcNow.AddDays(-7),
            },
            new()
            {
                Id = 3,
                EmployeeId = emp.EmployeeId,
                Date = DateTime.UtcNow.AddDays(-5),
                ExceptionType = "LateArrival",
                ExpectedClockIn = TimeSpan.FromHours(6),
                ActualClockIn = TimeSpan.Parse("06:30"),
                ExpectedClockOut = TimeSpan.FromHours(14),
                ActualClockOut = TimeSpan.FromHours(14),
                MinutesLate = 30,
                MinutesEarly = 0,
                LostProductionMinutes = 30,
                Status = "Closed",
                InvestigationNotes = "Third late arrival within 30 days.",
                InvestigatedBy = "EMP002",
                InvestigatedAt = DateTime.UtcNow.AddDays(-4),
            },
        };

        var expectedWarning = new WarningLetter
        {
            EmployeeId = emp.EmployeeId,
            WarningType = "Counselling",
            IssueDate = DateTime.UtcNow,
            Description = "Third late arrival within 30 days — Counselling session required.",
            IssuedBy = "EMP002",
            Acknowledged = false,
        };

        return new ScenarioResult
        {
            Employee = emp,
            Exceptions = exceptions,
            ExpectedWarning = expectedWarning,
            ExpectedNextStep = "After Counselling, next offence should trigger VerbalWarning",
            ValidationChecks = new List<string>
            {
                "Only 1 active warning (Counselling)",
                "No VerbalWarning issued yet",
                "Employee has exactly 3 investigated exceptions",
            }
        };
    }

    /// <summary>
    /// Scenario: Employee with 5 exceptions → Verbal Warning
    /// 
    /// Path: Counselling (at 3rd) → VerbalWarning (at 5th)
    /// The 4th exception is noted after counselling but before verbal threshold.
    /// </summary>
    public static ScenarioResult CreateVerbalWarningScenario()
    {
        var emp = EmployeeFixtures.GetOperatorWithWarnings(); // EMP005

        var existingChain = WarningFixtures.CreateProgressiveDisciplineChain(emp.EmployeeId);
        var existingWarnings = existingChain.Where(w => w.WarningType != "FinalWrittenWarning").ToList();

        var exceptions = new List<AttendanceException>
        {
            // Existing 3 exceptions that led to Counselling + VerbalWarning
            new() { Id = 1, EmployeeId = emp.EmployeeId, Date = DateTime.UtcNow.AddDays(-150), ExceptionType = "LateArrival",
                Status = "Closed", MinutesLate = 25, InvestigationNotes = "First late.", InvestigatedBy = "EMP002",
                InvestigatedAt = DateTime.UtcNow.AddDays(-148), ExpectedClockIn = TimeSpan.FromHours(6), ActualClockIn = TimeSpan.Parse("06:25"),
                ExpectedClockOut = TimeSpan.FromHours(14), ActualClockOut = TimeSpan.FromHours(14), MinutesEarly = 0, LostProductionMinutes = 25 },
            new() { Id = 2, EmployeeId = emp.EmployeeId, Date = DateTime.UtcNow.AddDays(-120), ExceptionType = "LateArrival",
                Status = "Closed", MinutesLate = 30, InvestigationNotes = "Second late.", InvestigatedBy = "EMP002",
                InvestigatedAt = DateTime.UtcNow.AddDays(-118), ExpectedClockIn = TimeSpan.FromHours(6), ActualClockIn = TimeSpan.Parse("06:30"),
                ExpectedClockOut = TimeSpan.FromHours(14), ActualClockOut = TimeSpan.FromHours(14), MinutesEarly = 0, LostProductionMinutes = 30 },
            new() { Id = 3, EmployeeId = emp.EmployeeId, Date = DateTime.UtcNow.AddDays(-90), ExceptionType = "LateArrival",
                Status = "Closed", MinutesLate = 20, InvestigationNotes = "Third late – Counselling.", InvestigatedBy = "EMP002",
                InvestigatedAt = DateTime.UtcNow.AddDays(-88), ExpectedClockIn = TimeSpan.FromHours(6), ActualClockIn = TimeSpan.Parse("06:20"),
                ExpectedClockOut = TimeSpan.FromHours(14), ActualClockOut = TimeSpan.FromHours(14), MinutesEarly = 0, LostProductionMinutes = 20 },
            // Fourth exception — still under Counselling validity
            new() { Id = 4, EmployeeId = emp.EmployeeId, Date = DateTime.UtcNow.AddDays(-75), ExceptionType = "EarlyDeparture",
                Status = "Closed", MinutesLate = 0, InvestigationNotes = "Left 30 min early.", InvestigatedBy = "EMP002",
                InvestigatedAt = DateTime.UtcNow.AddDays(-73), ExpectedClockIn = TimeSpan.FromHours(14), ActualClockIn = TimeSpan.Parse("13:55"),
                ExpectedClockOut = TimeSpan.FromHours(22), ActualClockOut = TimeSpan.Parse("21:30"), MinutesEarly = 30, LostProductionMinutes = 30 },
            // Fifth exception — triggers VerbalWarning
            new() { Id = 5, EmployeeId = emp.EmployeeId, Date = DateTime.UtcNow.AddDays(-60), ExceptionType = "LateArrival",
                Status = "Investigated", MinutesLate = 45, InvestigationNotes = "Fourth exception – VerbalWarning triggered.", InvestigatedBy = "EMP002",
                InvestigatedAt = DateTime.UtcNow.AddDays(-58), ExpectedClockIn = TimeSpan.FromHours(6), ActualClockIn = TimeSpan.Parse("06:45"),
                ExpectedClockOut = TimeSpan.FromHours(14), ActualClockOut = TimeSpan.FromHours(14), MinutesEarly = 0, LostProductionMinutes = 45 },
        };

        return new ScenarioResult
        {
            Employee = emp,
            Exceptions = exceptions,
            ExistingWarnings = existingWarnings,
            ExpectedWarning = new WarningLetter
            {
                EmployeeId = emp.EmployeeId,
                WarningType = "VerbalWarning",
                IssueDate = DateTime.UtcNow,
                Description = "Fifth attendance exception within validity period — VerbalWarning issued.",
                IssuedBy = "EMP002",
                Acknowledged = false,
            },
            ExpectedNextStep = "After VerbalWarning, next offence should trigger WrittenWarning",
            ValidationChecks = new List<string>
            {
                "Active warnings: Counselling + VerbalWarning",
                "No WrittenWarning yet",
                "Total exceptions in validity window: 5",
                "Counselling warning still within 90-day validity",
            }
        };
    }

    /// <summary>
    /// Scenario: Employee with 7 exceptions → Written Warning
    /// 
    /// Path: Counselling (3rd) → VerbalWarning (5th) → WrittenWarning (7th)
    /// </summary>
    public static ScenarioResult CreateWrittenWarningScenario()
    {
        var emp = EmployeeFixtures.GetOperatorWithWarnings();

        var existingChain = WarningFixtures.CreateProgressiveDisciplineChain(emp.EmployeeId);
        var existingWarnings = existingChain.Where(w => w.WarningType == "WrittenWarning").ToList();

        var exceptions = new List<AttendanceException>();
        // Generate 7 exceptions across different types
        for (int i = 1; i <= 7; i++)
        {
            var isLate = i % 2 == 0;
            exceptions.Add(new AttendanceException
            {
                Id = 100 + i,
                EmployeeId = emp.EmployeeId,
                Date = DateTime.UtcNow.AddDays(-(8 - i)),
                ExceptionType = isLate ? "LateArrival" : "EarlyDeparture",
                Status = "Closed",
                MinutesLate = isLate ? 25 * i : 0,
                MinutesEarly = !isLate ? 20 * i : 0,
                InvestigationNotes = $"Exception #{i} — pattern of repeated violations.",
                InvestigatedBy = "EMP002",
                InvestigatedAt = DateTime.UtcNow.AddDays(-(7 - i)),
                ExpectedClockIn = TimeSpan.FromHours(6),
                ActualClockIn = isLate ? TimeSpan.Parse($"06:{25 * i:D2}") : TimeSpan.FromHours(6),
                ExpectedClockOut = TimeSpan.FromHours(14),
                ActualClockOut = !isLate ? TimeSpan.FromHours(14 - (20 * i / 60)) : TimeSpan.FromHours(14),
                LostProductionMinutes = 20 * i,
            });
        }

        return new ScenarioResult
        {
            Employee = emp,
            Exceptions = exceptions,
            ExistingWarnings = existingWarnings,
            ExpectedWarning = new WarningLetter
            {
                EmployeeId = emp.EmployeeId,
                WarningType = "WrittenWarning",
                IssueDate = DateTime.UtcNow,
                Description = "Seventh attendance exception — WrittenWarning issued per progressive discipline policy.",
                IssuedBy = "EMP002",
                Acknowledged = false,
            },
            ExpectedNextStep = "After WrittenWarning, next offence should trigger FinalWrittenWarning",
            ValidationChecks = new List<string>
            {
                "Active warnings: Counselling + VerbalWarning + WrittenWarning",
                "No FinalWrittenWarning yet",
                "Total exceptions in validity window: 7",
                "All previous warnings within their respective validity periods",
            }
        };
    }

    /// <summary>
    /// Scenario: Employee with 9 exceptions → Final Warning + Auto-Escalation
    /// 
    /// Path: Counselling (3rd) → VerbalWarning (5th) → WrittenWarning (7th) → FinalWrittenWarning (9th)
    /// FinalWrittenWarning triggers automatic escalation to Level 1 supervisor.
    /// </summary>
    public static ScenarioResult CreateFinalWarningAndEscalationScenario()
    {
        var emp = EmployeeFixtures.GetOperatorWithWarnings();

        var existingChain = WarningFixtures.CreateProgressiveDisciplineChain(emp.EmployeeId);

        var exceptions = new List<AttendanceException>();
        for (int i = 1; i <= 9; i++)
        {
            var isLate = i % 2 == 0;
            exceptions.Add(new AttendanceException
            {
                Id = 200 + i,
                EmployeeId = emp.EmployeeId,
                Date = DateTime.UtcNow.AddDays(-(10 - i)),
                ExceptionType = isLate ? "LateArrival" : "UnplannedAbsenteeism",
                Status = i >= 9 ? "Investigated" : "Closed",
                MinutesLate = isLate ? 30 * i : 0,
                MinutesEarly = !isLate && !new[] { 3, 7 }.Contains(i) ? 25 * i : 0,
                InvestigationNotes = $"Exception #{i} — ",
                InvestigatedBy = "EMP002",
                InvestigatedAt = DateTime.UtcNow.AddDays(-(9 - i)),
                ExpectedClockIn = TimeSpan.FromHours(6),
                ActualClockIn = isLate ? TimeSpan.Parse("06:30") : null,
                ExpectedClockOut = TimeSpan.FromHours(14),
                ActualClockOut = !isLate ? TimeSpan.FromHours(13) : TimeSpan.FromHours(14),
                LostProductionMinutes = 30 * i,
            });
        }

        return new ScenarioResult
        {
            Employee = emp,
            Exceptions = exceptions,
            ExistingWarnings = existingChain,
            ExpectedWarning = new WarningLetter
            {
                EmployeeId = emp.EmployeeId,
                WarningType = "FinalWrittenWarning",
                IssueDate = DateTime.UtcNow,
                Description = "Ninth attendance exception — Final Written Warning issued. Next offence may result in termination hearing.",
                IssuedBy = "EMP002",
                Acknowledged = false,
            },
            ExpectedEscalation = new Escalation
            {
                EmployeeId = emp.EmployeeId,
                WarningLetterId = 4, // The FinalWrittenWarning ID
                Level = 1,
                ResponsibleManagerId = "EMP002",
                Status = "Pending",
                Notes = "Auto-created by FinalWrittenWarning generation — awaiting supervisor acknowledgment.",
            },
            ExpectedNextStep = "Escalation created at Level 1. If unresolved in 24h, auto-escalates to Level 2.",
            ValidationChecks = new List<string>
            {
                "All 4 warning types present: Counselling, VerbalWarning, WrittenWarning, FinalWrittenWarning",
                "Escalation auto-created at Level 1",
                "Escalation status is Pending",
                "Employee has 9 total investigated exceptions",
            }
        };
    }
}

/// <summary>
/// Result object representing the expected state after running a progressive discipline scenario.
/// </summary>
public record ScenarioResult
{
    public Employee Employee { get; init; } = null!;
    public List<AttendanceException> Exceptions { get; init; } = null!;
    public List<WarningLetter> ExistingWarnings { get; init; } = new();
    public WarningLetter? ExpectedWarning { get; init; }
    public Escalation? ExpectedEscalation { get; init; }
    public string? ExpectedNextStep { get; init; }
    public List<string> ValidationChecks { get; init; } = new();
}
