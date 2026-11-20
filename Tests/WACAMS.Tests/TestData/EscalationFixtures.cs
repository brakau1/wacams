using WACAMS.Core.Entities;

namespace WACAMS.Tests.TestData;

/// <summary>
/// Test data factory for Escalation entities.
/// Covers escalations at each level (1, 2, 3), various statuses (Pending, Accepted, ActionTaken, Escalated, Closed),
/// overdue escalations (>24 hours), and complete escalation chains.
/// </summary>
public static class EscalationFixtures
{
    /// <summary>
    /// Creates a complete escalation chain across 3 levels triggered by a FinalWrittenWarning.
    /// Simulates the full lifecycle: Level 1 → Level 2 → Level 3 → Closed.
    /// </summary>
    public static List<Escalation> CreateCompleteEscalationChain(int warningLetterId)
    {
        var now = DateTime.UtcNow;

        return new List<Escalation>
        {
            // Level 1 — Direct Supervisor (Botlhale Rakau / EMP002) — Resolved
            new()
            {
                Id = 1,
                EmployeeId = "EMP005",
                WarningLetterId = warningLetterId,
                Level = 1,
                ResponsibleManagerId = "EMP002",
                Status = "ActionTaken",
                Notes = "Supervisor reviewed case with employee. Employee acknowledged pattern of lateness and committed to improvement plan.",
                CreatedAt = now.AddDays(-7),
                UpdatedAt = now.AddDays(-5),
                ResolvedAt = now.AddDays(-5),
            },

            // Level 2 — Production Manager (Jeremiah Bezuidenhout / EMP009) — Resolved (escalated because L1 didn't act in time)
            new()
            {
                Id = 2,
                EmployeeId = "EMP005",
                WarningLetterId = warningLetterId,
                Level = 2,
                ResponsibleManagerId = "EMP009",
                Status = "ActionTaken",
                Notes = "Production manager followed up after auto-escalation from Level 1. Disciplinary action completed.",
                CreatedAt = now.AddDays(-5), // Auto-created after 24h timeout from Level 1
                UpdatedAt = now.AddDays(-3),
                ResolvedAt = now.AddDays(-3),
            },

            // Level 3 — HR Director (EMP043) — Pending (for testing pending state)
            new()
            {
                Id = 3,
                EmployeeId = "EMP005",
                WarningLetterId = warningLetterId,
                Level = 3,
                ResponsibleManagerId = "EMP043",
                Status = "Pending",
                Notes = "Final escalation level — awaiting HR Director review.",
                CreatedAt = now.AddDays(-3),
                UpdatedAt = null,
                ResolvedAt = null,
            },
        };
    }

    /// <summary>
    /// Creates an overdue escalation at Level 1 (more than 24 hours since creation without acknowledgment).
    /// Should trigger auto-escalation.
    /// </summary>
    public static Escalation CreateOverdueLevel1Escalation(int warningLetterId)
    {
        return new()
        {
            Id = 100,
            EmployeeId = "EMP006",
            WarningLetterId = warningLetterId,
            Level = 1,
            ResponsibleManagerId = "EMP002",
            Status = "Pending",
            Notes = "Escalation has been pending for more than 24 hours — eligible for auto-escalation to Level 2.",
            CreatedAt = DateTime.UtcNow.AddHours(-26), // 26 hours ago — OVERDUE
            UpdatedAt = null,
            ResolvedAt = null,
        };
    }

    /// <summary>
    /// Creates an escalation that was accepted but no action taken yet (Accepted status).
    /// </summary>
    public static Escalation CreateAcceptedEscalation(int warningLetterId)
    {
        return new()
        {
            Id = 200,
            EmployeeId = "EMP010",
            WarningLetterId = warningLetterId,
            Level = 1,
            ResponsibleManagerId = "EMP002",
            Status = "Accepted",
            Notes = "Manager accepted the escalation and is gathering evidence before taking action.",
            CreatedAt = DateTime.UtcNow.AddHours(-8),
            UpdatedAt = DateTime.UtcNow.AddHours(-6),
            ResolvedAt = null,
        };
    }

    /// <summary>
    /// Creates a closed/resolved escalation (fully resolved at any level).
    /// </summary>
    public static Escalation CreateClosedEscalation(int warningLetterId)
    {
        return new()
        {
            Id = 300,
            EmployeeId = "EMP015",
            WarningLetterId = warningLetterId,
            Level = 2,
            ResponsibleManagerId = "EMP009",
            Status = "Closed",
            Notes = "Case resolved at Level 2. Employee attended disciplinary hearing. No further escalation required.",
            CreatedAt = DateTime.UtcNow.AddDays(-14),
            UpdatedAt = DateTime.UtcNow.AddDays(-10),
            ResolvedAt = DateTime.UtcNow.AddDays(-10),
        };
    }

    /// <summary>
    /// Creates an escalation that was escalated further up the chain (Escalated status).
    /// Used when current level cannot resolve the issue.
    /// </summary>
    public static Escalation CreateEscalatedStatusEscalation(int warningLetterId)
    {
        return new()
        {
            Id = 400,
            EmployeeId = "EMP020",
            WarningLetterId = warningLetterId,
            Level = 2,
            ResponsibleManagerId = "EMP009",
            Status = "Escalated",
            Notes = "Production manager could not resolve — escalated to HR Director for final decision.",
            CreatedAt = DateTime.UtcNow.AddDays(-2),
            UpdatedAt = DateTime.UtcNow.AddDays(-1),
            ResolvedAt = null,
        };
    }

    /// <summary>
    /// Creates a complete set of escalations covering all test scenarios.
    /// </summary>
    public static Dictionary<string, List<Escalation>> CreateAllEscalations()
    {
        var all = new Dictionary<string, List<Escalation>>();

        // Complete chain for EMP005
        all["EMP005"] = CreateCompleteEscalationChain(4);

        // Overdue escalation for EMP006 (should trigger auto-escalation)
        all["EMP006"] = new List<Escalation> { CreateOverdueLevel1Escalation(5) };

        // Accepted escalation for EMP010
        all["EMP010"] = new List<Escalation> { CreateAcceptedEscalation(6) };

        // Closed escalation for EMP015
        all["EMP015"] = new List<Escalation> { CreateClosedEscalation(7) };

        // Escalated status for EMP020
        all["EMP020"] = new List<Escalation> { CreateEscalatedStatusEscalation(8) };

        return all;
    }
}
