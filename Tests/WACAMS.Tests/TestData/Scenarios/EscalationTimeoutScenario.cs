using WACAMS.Core.Entities;

namespace WACAMS.Tests.TestData.Scenarios;

/// <summary>
/// Test scenarios for the 24-hour escalation timeout mechanism.
/// Models the full lifecycle of auto-escalation when managers fail to act within 24 hours.
/// </summary>
public static class EscalationTimeoutScenario
{
    /// <summary>
    /// Scenario: Complete auto-escalation chain from Level 1 → Level 2 → Level 3 → Auto-resolve
    /// 
    /// Timeline:
    /// - T+0h: FinalWrittenWarning issued → Escalation created at Level 1 (EMP002)
    /// - T+24h: Level 1仍未响应 → Auto-escalate to Level 2 (EMP009)
    /// - T+48h: Level 2仍未响应 → Auto-escalate to Level 3 (EMP043)
    /// - T+72h: Level 3仍未响应 → Max level reached → Auto-resolve + HR notification
    /// </summary>
    public static List<EscalationTimelineEvent> CreateFullAutoEscalationChain()
    {
        var now = DateTime.UtcNow;
        var empId = "EMP005";
        var warningLetterId = 4;

        return new List<EscalationTimelineEvent>
        {
            // T+0h: Initial escalation created at Level 1
            new()
            {
                Timestamp = now,
                Event = "EscalationCreated",
                Description = $"FinalWrittenWarning generated for {empId}. Escalation #{1} created at Level 1.",
                Escalation = new Escalation
                {
                    Id = 1,
                    EmployeeId = empId,
                    WarningLetterId = warningLetterId,
                    Level = 1,
                    ResponsibleManagerId = "EMP002", // Production Supervisor
                    Status = "Pending",
                    Notes = "Initial escalation — awaiting supervisor acknowledgment.",
                    CreatedAt = now,
                },
                ExpectedAction = "Supervisor (EMP002) must acknowledge or take action within 24 hours.",
            },

            // T+24h: Level 1 timeout → Auto-escalate to Level 2
            new()
            {
                Timestamp = now.AddHours(24),
                Event = "AutoEscalated_L1toL2",
                Description = "Level 1 supervisor did not respond within 24 hours. Auto-escalating to Level 2.",
                Escalation = new Escalation
                {
                    Id = 2,
                    EmployeeId = empId,
                    WarningLetterId = warningLetterId,
                    Level = 2,
                    ResponsibleManagerId = "EMP009", // Production Manager
                    Status = "Pending",
                    Notes = "Auto-escalated from Level 1 due to 24-hour timeout. Original escalation #1 status: Escalated.",
                    CreatedAt = now.AddHours(24),
                },
                PreviousEscalationStatus = "Escalated",
                PreviousEscalationId = 1,
                ExpectedAction = "Production Manager (EMP009) must acknowledge or take action within 24 hours.",
            },

            // T+48h: Level 2 timeout → Auto-escalate to Level 3
            new()
            {
                Timestamp = now.AddHours(48),
                Event = "AutoEscalated_L2toL3",
                Description = "Level 2 manager did not respond within 24 hours. Auto-escalating to Level 3.",
                Escalation = new Escalation
                {
                    Id = 3,
                    EmployeeId = empId,
                    WarningLetterId = warningLetterId,
                    Level = 3,
                    ResponsibleManagerId = "EMP043", // HR Director
                    Status = "Pending",
                    Notes = "Auto-escalated from Level 2 due to 24-hour timeout. Maximum escalation level reached.",
                    CreatedAt = now.AddHours(48),
                },
                PreviousEscalationStatus = "Escalated",
                PreviousEscalationId = 2,
                ExpectedAction = "HR Director (EMP043) must acknowledge or take action within 24 hours. This is the maximum escalation level.",
            },

            // T+72h: Level 3 timeout → Max level reached → Auto-resolve
            new()
            {
                Timestamp = now.AddHours(72),
                Event = "MaxLevelReached_AutoResolve",
                Description = "Level 3 (max) did not respond within 24 hours. Escalation auto-resolved. HR notified.",
                Escalation = new Escalation
                {
                    Id = 3,
                    EmployeeId = empId,
                    WarningLetterId = warningLetterId,
                    Level = 3,
                    ResponsibleManagerId = "EMP043",
                    Status = "Closed",
                    Notes = "Auto-resolved after max level (3) timeout. Case closed pending HR review.",
                    CreatedAt = now.AddHours(48),
                    UpdatedAt = now.AddHours(72),
                    ResolvedAt = now.AddHours(72),
                },
                ExpectedAction = "Case auto-resolved. HR team must conduct manual review and document final disposition.",
                IsAutoResolved = true,
            },
        };
    }

    /// <summary>
    /// Scenario: Successful resolution at Level 1 (no auto-escalation needed).
    /// Demonstrates the happy path where the first-level manager acts in time.
    /// </summary>
    public static List<EscalationTimelineEvent> CreateSuccessfulLevel1Resolution()
    {
        var now = DateTime.UtcNow;
        var empId = "EMP006";
        var warningLetterId = 5;

        return new List<EscalationTimelineEvent>
        {
            // T+0h: Escalation created at Level 1
            new()
            {
                Timestamp = now,
                Event = "EscalationCreated",
                Description = $"FinalWrittenWarning generated for {empId}. Escalation created at Level 1.",
                Escalation = new Escalation
                {
                    Id = 10,
                    EmployeeId = empId,
                    WarningLetterId = warningLetterId,
                    Level = 1,
                    ResponsibleManagerId = "EMP002",
                    Status = "Pending",
                    Notes = "Awaiting supervisor acknowledgment.",
                    CreatedAt = now,
                },
                ExpectedAction = "Supervisor (EMP002) must acknowledge within 24 hours.",
            },

            // T+2h: Supervisor acknowledges
            new()
            {
                Timestamp = now.AddHours(2),
                Event = "Acknowledged_By_Supervisor",
                Description = "Supervisor (EMP002) acknowledged the escalation and began investigation.",
                Escalation = new Escalation
                {
                    Id = 10,
                    EmployeeId = empId,
                    WarningLetterId = warningLetterId,
                    Level = 1,
                    ResponsibleManagerId = "EMP002",
                    Status = "Accepted",
                    Notes = "Supervisor accepted responsibility and scheduled disciplinary hearing.",
                    CreatedAt = now,
                    UpdatedAt = now.AddHours(2),
                },
                ExpectedAction = "Supervisor conducts investigation and schedules hearing.",
            },

            // T+18h: Action taken — hearing completed
            new()
            {
                Timestamp = now.AddHours(18),
                Event = "ActionTaken_HearingCompleted",
                Description = "Disciplinary hearing completed. Employee counseled and signed corrective action form.",
                Escalation = new Escalation
                {
                    Id = 10,
                    EmployeeId = empId,
                    WarningLetterId = warningLetterId,
                    Level = 1,
                    ResponsibleManagerId = "EMP002",
                    Status = "ActionTaken",
                    Notes = "Hearing held on schedule. Employee acknowledged fault and signed improvement plan.",
                    CreatedAt = now,
                    UpdatedAt = now.AddHours(18),
                    ResolvedAt = now.AddHours(18),
                },
                ExpectedAction = "Escalation resolved at Level 1. No further escalation required.",
                IsResolved = true,
            },
        };
    }

    /// <summary>
    /// Scenario: Partial escalation chain — Level 1 resolves but Level 2 times out.
    /// Tests edge case where a lower level succeeds but a higher level fails.
    /// </summary>
    public static List<EscalationTimelineEvent> CreatePartialEscalationChain()
    {
        var now = DateTime.UtcNow;
        var empId = "EMP010";
        var warningLetterId = 6;

        return new List<EscalationTimelineEvent>
        {
            // T+0h: Level 1 created
            new()
            {
                Timestamp = now,
                Event = "EscalationCreated",
                Description = "Escalation created at Level 1.",
                Escalation = new Escalation
                {
                    Id = 20,
                    EmployeeId = empId,
                    WarningLetterId = warningLetterId,
                    Level = 1,
                    ResponsibleManagerId = "EMP002",
                    Status = "Pending",
                    Notes = "Awaiting supervisor.",
                    CreatedAt = now,
                },
                ExpectedAction = "Supervisor (EMP002) must respond within 24 hours.",
            },

            // T+6h: Level 1 resolves quickly
            new()
            {
                Timestamp = now.AddHours(6),
                Event = "ActionTaken_Level1",
                Description = "Level 1 supervisor resolved the case.",
                Escalation = new Escalation
                {
                    Id = 20,
                    EmployeeId = empId,
                    WarningLetterId = warningLetterId,
                    Level = 1,
                    ResponsibleManagerId = "EMP002",
                    Status = "ActionTaken",
                    Notes = "Resolved by supervisor — employee attended counselling session.",
                    CreatedAt = now,
                    UpdatedAt = now.AddHours(6),
                    ResolvedAt = now.AddHours(6),
                },
                IsResolved = true,
                ExpectedAction = "If fully resolved, no escalation to Level 2 needed.",
            },

            // However, if this was a multi-part case requiring Level 2 involvement:
            // T+30h: Level 2 auto-created because Level 1 resolution was insufficient
            new()
            {
                Timestamp = now.AddHours(30),
                Event = "SecondaryEscalationToLevel2",
                Description = "Level 1 resolution deemed insufficient. Secondary escalation to Level 2.",
                Escalation = new Escalation
                {
                    Id = 21,
                    EmployeeId = empId,
                    WarningLetterId = warningLetterId,
                    Level = 2,
                    ResponsibleManagerId = "EMP009",
                    Status = "Pending",
                    Notes = "Level 1 resolution incomplete — secondary escalation required.",
                    CreatedAt = now.AddHours(30),
                },
                ExpectedAction = "Production Manager (EMP009) must respond within 24 hours.",
            },
        };
    }
}

/// <summary>
/// Represents a single event in an escalation timeline.
/// Used for testing the chronological progression of escalations.
/// </summary>
public record EscalationTimelineEvent
{
    public DateTime Timestamp { get; init; }
    public string Event { get; init; } = null!;
    public string Description { get; init; } = null!;
    public Escalation Escalation { get; init; } = null!;
    public string? ExpectedAction { get; init; }
    public string? PreviousEscalationStatus { get; init; }
    public int? PreviousEscalationId { get; init; }
    public bool IsResolved { get; init; }
    public bool IsAutoResolved { get; init; }
}
