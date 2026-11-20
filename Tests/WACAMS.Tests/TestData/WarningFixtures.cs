using WACAMS.Core.Entities;

namespace WACAMS.Tests.TestData;

/// <summary>
/// Test data factory for WarningLetter entities.
/// Covers all stages of progressive discipline (Counselling → Verbal → Written → FinalWritten),
/// acknowledged/unacknowledged states, expired/active warnings (90-day window),
/// and warnings with/without signed forms.
/// </summary>
public static class WarningFixtures
{
    /// <summary>
    /// Creates a complete progressive discipline chain for a single employee.
    /// Simulates 4 warnings escalating through all levels.
    /// </summary>
    public static List<WarningLetter> CreateProgressiveDisciplineChain(string employeeId)
    {
        var now = DateTime.UtcNow;

        return new List<WarningLetter>
        {
            // 1st offence — Counselling (within validity period, active)
            new()
            {
                Id = 1,
                EmployeeId = employeeId,
                ExceptionId = 1,
                WarningType = "Counselling",
                IssueDate = now.AddDays(-30),
                Description = "First late arrival incident — arrived 25 minutes past scheduled time without prior notice.",
                IssuedBy = "EMP002",
                SignedFormPath = "/signatures/warning_emp005_001.pdf",
                Acknowledged = true,
                AcknowledgedAt = now.AddDays(-28),
            },

            // 2nd offence — VerbalWarning (within validity period, active)
            new()
            {
                Id = 2,
                EmployeeId = employeeId,
                ExceptionId = 2,
                WarningType = "VerbalWarning",
                IssueDate = now.AddDays(-60),
                Description = "Repeat late arrival — 2nd occurrence within 90 days. Arrived 45 minutes late after counselling session.",
                IssuedBy = "EMP002",
                SignedFormPath = "/signatures/warning_emp005_002.pdf",
                Acknowledged = true,
                AcknowledgedAt = now.AddDays(-58),
            },

            // 3rd offence — WrittenWarning (within validity period, active)
            new()
            {
                Id = 3,
                EmployeeId = employeeId,
                ExceptionId = 3,
                WarningType = "WrittenWarning",
                IssueDate = now.AddDays(-120),
                Description = "Third disciplinary action — pattern of lateness continues despite verbal warning. Arrived 60 minutes late.",
                IssuedBy = "EMP002",
                SignedFormPath = "/signatures/warning_emp005_003.pdf",
                Acknowledged = false,
                AcknowledgedAt = null,
            },

            // 4th offence — FinalWrittenWarning (active)
            new()
            {
                Id = 4,
                EmployeeId = employeeId,
                ExceptionId = 4,
                WarningType = "FinalWrittenWarning",
                IssueDate = now.AddDays(-180),
                Description = "Fourth escalation — final written warning issued due to continued attendance violations. Next offence may result in termination hearing.",
                IssuedBy = "EMP002",
                SignedFormPath = "/signatures/warning_emp005_004.pdf",
                Acknowledged = true,
                AcknowledgedAt = now.AddDays(-178),
            },
        };
    }

    /// <summary>
    /// Creates an expired warning (beyond 90-day validity period).
    /// Should NOT count toward progressive discipline anymore.
    /// </summary>
    public static WarningLetter CreateExpiredWarning(string employeeId)
    {
        return new()
        {
            Id = 100,
            EmployeeId = employeeId,
            ExceptionId = 50,
            WarningType = "Counselling",
            IssueDate = DateTime.UtcNow.AddDays(-100), // Beyond 90-day validity
            Description = "Old counselling record that has expired per validity period policy.",
            IssuedBy = "EMP002",
            SignedFormPath = "/signatures/warning_expired_001.pdf",
            Acknowledged = true,
            AcknowledgedAt = DateTime.UtcNow.AddDays(-98),
        };
    }

    /// <summary>
    /// Creates an unacknowledged warning (employee has not signed/received).
    /// </summary>
    public static WarningLetter CreateUnacknowledgedWarning(string employeeId)
    {
        return new()
        {
            Id = 200,
            EmployeeId = employeeId,
            ExceptionId = 60,
            WarningType = "VerbalWarning",
            IssueDate = DateTime.UtcNow.AddDays(-15),
            Description = "Employee refused to acknowledge receipt of this warning letter.",
            IssuedBy = "EMP002",
            SignedFormPath = null, // No signed form — employee did not sign
            Acknowledged = false,
            AcknowledgedAt = null,
        };
    }

    /// <summary>
    /// Creates a warning without a signed PDF form.
    /// </summary>
    public static WarningLetter CreateWarningWithoutSignedForm(string employeeId)
    {
        return new()
        {
            Id = 300,
            EmployeeId = employeeId,
            ExceptionId = 70,
            WarningType = "WrittenWarning",
            IssueDate = DateTime.UtcNow.AddDays(-45),
            Description = "Written warning issued but signed form pending signature from HR.",
            IssuedBy = "EMP002",
            SignedFormPath = null, // Pending signature
            Acknowledged = false,
            AcknowledgedAt = null,
        };
    }

    /// <summary>
    /// Creates a complete set of warnings covering all test scenarios.
    /// </summary>
    public static Dictionary<string, List<WarningLetter>> CreateAllWarnings()
    {
        var all = new Dictionary<string, List<WarningLetter>>();

        // Employee with full progressive discipline chain (EMP005)
        all["EMP005"] = CreateProgressiveDisciplineChain("EMP005");

        // Employee with only expired warnings (should be clean for discipline purposes)
        var expiredWarnings = new List<WarningLetter>
        {
            CreateExpiredWarning("EMP010"),
            CreateExpiredWarning("EMP010"),
        };
        all["EMP010"] = expiredWarnings;

        // Employee with unacknowledged warning (EMP015)
        var unackWarnings = new List<WarningLetter>
        {
            CreateUnacknowledgedWarning("EMP015"),
        };
        all["EMP015"] = unackWarnings;

        // Clean employee with no warnings (EMP006)
        all["EMP006"] = new List<WarningLetter>();

        return all;
    }
}
