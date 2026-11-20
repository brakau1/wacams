using System.ComponentModel.DataAnnotations;

namespace Wacam.Api.DTOs;

public class EscalationDto
{
    public Guid Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public Guid WarningLetterId { get; set; }
    public string WarningType { get; set; } = string.Empty;
    public int Level { get; set; }
    public string ResponsibleManagerId { get; set; } = string.Empty;
    public string ResponsibleManagerName { get; set; } = string.Empty;
    public string Status { get; set; } = "Pending";
    public string? Notes { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? UpdatedAt { get; set; }
    public DateTime? ResolvedAt { get; set; }
}

public class ProgressEscalationRequest
{
    [StringLength(2000)]
    public string Notes { get; set; } = string.Empty;
}

public class EscalationChainResponse
{
    public string EmployeeId { get; set; } = string.Empty;
    public string DisplayName { get; set; } = string.Empty;
    public List<EscalationLevelDto> Chain { get; set; } = new();
}

public class EscalationLevelDto
{
    public int Level { get; set; }
    public string ManagerId { get; set; } = string.Empty;
    public string ManagerName { get; set; } = string.Empty;
    public string Role { get; set; } = string.Empty;
}
