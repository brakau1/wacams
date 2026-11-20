namespace WACAMS.Core.DTOs;

/// <summary>
/// Data transfer object for escalation read operations.
/// Contains escalation chain details with from/to manager names resolved.
/// </summary>
public record EscalationDTO(
    Guid Id,
    Guid WarningLetterId,
    string FromManagerName,
    string ToManagerName,
    int Level,
    Core.Enums.EscalationStatus Status,
    string ActionRequired
);
