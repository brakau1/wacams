using System.ComponentModel.DataAnnotations;

namespace Wacam.Api.DTOs;

// ===== Permission Template DTOs =====

public class PermissionTemplateDto
{
    public Guid Id { get; set; }
    public string Name { get; set; } = string.Empty;
    public string? Description { get; set; }
    public int Version { get; set; }
    public bool IsActive { get; set; }
    public string? TargetRole { get; set; }
    public Guid? TargetDepartmentId { get; set; }
    public string? TargetDepartmentName { get; set; }
    public Dictionary<string, Dictionary<string, bool>> Permissions { get; set; } = new();
    public string CreatedBy { get; set; } = string.Empty;
    public string? ModifiedBy { get; set; }
    public DateTime CreatedAt { get; set; }
    public DateTime? ModifiedAt { get; set; }
    public int AssignmentCount { get; set; }
}

public class CreatePermissionTemplateRequest
{
    [Required, MaxLength(100)]
    public string Name { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Description { get; set; }
    [MaxLength(50)]
    public string? TargetRole { get; set; }
    public Guid? TargetDepartmentId { get; set; }
    [Required]
    public Dictionary<string, Dictionary<string, bool>> Permissions { get; set; } = new();
}

public class UpdatePermissionTemplateRequest
{
    [MaxLength(500)]
    public string? Description { get; set; }
    [MaxLength(50)]
    public string? TargetRole { get; set; }
    public Guid? TargetDepartmentId { get; set; }
    public bool? IsActive { get; set; }
    public Dictionary<string, Dictionary<string, bool>>? Permissions { get; set; }
}

public class ClonePermissionTemplateRequest
{
    [Required, MaxLength(100)]
    public string NewName { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Description { get; set; }
}

public class AssignTemplateRequest
{
    [Required]
    public List<string> EmployeeIds { get; set; } = new();
    public DateTime? ExpiresAt { get; set; }
    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class PermissionTemplateVersionDto
{
    public Guid Id { get; set; }
    public Guid TemplateId { get; set; }
    public int VersionNumber { get; set; }
    public Dictionary<string, Dictionary<string, bool>> Permissions { get; set; } = new();
    public string? ChangeDescription { get; set; }
    public string CreatedBy { get; set; } = string.Empty;
    public DateTime CreatedAt { get; set; }
}

// ===== Bulk Administration DTOs =====

public class BulkImportRequest
{
    [Required]
    public string ImportType { get; set; } = string.Empty;
    [Required]
    public IFormFile File { get; set; } = null!;
}

public class BulkImportResultDto
{
    public Guid ImportId { get; set; }
    public string ImportType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public List<BulkImportError> Errors { get; set; } = new();
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
}

public class BulkImportError
{
    public int Row { get; set; }
    public string? Field { get; set; }
    public string Message { get; set; } = string.Empty;
}

public class BulkOperationRequest
{
    [Required]
    public List<string> EmployeeIds { get; set; } = new();
    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class BulkRoleAssignmentRequest
{
    [Required]
    public List<string> EmployeeIds { get; set; } = new();
    [Required, MaxLength(50)]
    public string Role { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class BulkDepartmentAssignmentRequest
{
    [Required]
    public List<string> EmployeeIds { get; set; } = new();
    [Required]
    public Guid DepartmentId { get; set; }
    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class BulkOverrideRequest
{
    [Required]
    public List<string> EmployeeIds { get; set; } = new();
    [Required]
    public List<string> OverridePermissions { get; set; } = new();
    public DateTime EffectiveFrom { get; set; }
    public DateTime EffectiveTo { get; set; }
    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class BulkImportHistoryDto
{
    public Guid Id { get; set; }
    public string ImportType { get; set; } = string.Empty;
    public string FileName { get; set; } = string.Empty;
    public int TotalRows { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public string Status { get; set; } = string.Empty;
    public string InitiatedBy { get; set; } = string.Empty;
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public bool IsRolledBack { get; set; }
}

// ===== Security Operations DTOs =====

public class SecurityEventDto
{
    public Guid Id { get; set; }
    public string EventType { get; set; } = string.Empty;
    public string Severity { get; set; } = string.Empty;
    public string? EmployeeId { get; set; }
    public string? EmployeeName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public string? RequestPath { get; set; }
    public string? Details { get; set; }
    public bool IsAcknowledged { get; set; }
    public string? AcknowledgedBy { get; set; }
    public DateTime? AcknowledgedAt { get; set; }
    public DateTime Timestamp { get; set; }
}

public class SecurityEventQuery
{
    public string? EventType { get; set; }
    public string? Severity { get; set; }
    public string? EmployeeId { get; set; }
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
    public bool? IsAcknowledged { get; set; }
    public int Page { get; set; } = 1;
    public int PageSize { get; set; } = 50;
}

public class AcknowledgeSecurityEventRequest
{
    [MaxLength(500)]
    public string? Notes { get; set; }
}

public class SecurityLockdownDto
{
    public Guid Id { get; set; }
    public string LockdownType { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    public string? DepartmentName { get; set; }
    public string? TargetRole { get; set; }
    public string Reason { get; set; } = string.Empty;
    public bool IsActive { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? EndedAt { get; set; }
    public string InitiatedBy { get; set; } = string.Empty;
    public string? EndedBy { get; set; }
}

public class InitiateLockdownRequest
{
    [Required, MaxLength(50)]
    public string LockdownType { get; set; } = string.Empty;
    public Guid? DepartmentId { get; set; }
    [MaxLength(50)]
    public string? TargetRole { get; set; }
    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class ForceLogoutRequest
{
    [Required]
    public string EmployeeId { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Reason { get; set; }
}

public class UserSessionDto
{
    public Guid Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string? EmployeeName { get; set; }
    public string? IpAddress { get; set; }
    public string? UserAgent { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime LastActivityAt { get; set; }
    public DateTime? ExpiresAt { get; set; }
    public bool IsActive { get; set; }
}

// ===== Access Review DTOs =====

public class AccessReviewSnapshotDto
{
    public Guid Id { get; set; }
    public string EmployeeId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Department { get; set; }
    public string? Role { get; set; }
    public Dictionary<string, Dictionary<string, bool>> AssignedPermissions { get; set; } = new();
    public Dictionary<string, Dictionary<string, bool>> InheritedPermissions { get; set; } = new();
    public List<string> ActiveOverrides { get; set; } = new();
    public string? DepartmentScope { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastRoleChangeAt { get; set; }
    public DateTime? LastPermissionChangeAt { get; set; }
    public DateTime SnapshotDate { get; set; }
    public string CapturedBy { get; set; } = string.Empty;
}

public class UserAccessExplorerDto
{
    public string EmployeeId { get; set; } = string.Empty;
    public string? DisplayName { get; set; }
    public string? Department { get; set; }
    public string? Role { get; set; }
    public Dictionary<string, Dictionary<string, bool>> AssignedPermissions { get; set; } = new();
    public Dictionary<string, Dictionary<string, bool>> InheritedPermissions { get; set; } = new();
    public List<EmployeeAccessOverrideDto> Overrides { get; set; } = new();
    public string? DepartmentScope { get; set; }
    public DateTime? LastLoginAt { get; set; }
    public DateTime? LastRoleChangeAt { get; set; }
    public DateTime? LastPermissionChangeAt { get; set; }
}

public class CaptureSnapshotRequest
{
    [Required]
    public string EmployeeId { get; set; } = string.Empty;
    [MaxLength(500)]
    public string? Reason { get; set; }
}

// ===== Security Analytics DTOs =====

public class SecurityAnalyticsDto
{
    public UserGrowthMetrics UserGrowth { get; set; } = new();
    public RoleDistributionMetrics RoleDistribution { get; set; } = new();
    public OverrideUtilizationMetrics OverrideUtilization { get; set; } = new();
    public SecurityEventMetrics SecurityEvents { get; set; } = new();
    public AuditActivityMetrics AuditActivity { get; set; } = new();
    public PermissionChangeMetrics PermissionChanges { get; set; } = new();
}

public class UserGrowthMetrics
{
    public int TotalUsers { get; set; }
    public int ActiveUsers { get; set; }
    public int NewUsersThisMonth { get; set; }
    public int NewUsersLastMonth { get; set; }
    public double GrowthRate { get; set; }
    public List<TrendDataPoint> Trend { get; set; } = new();
}

public class RoleDistributionMetrics
{
    public Dictionary<string, int> UsersByRole { get; set; } = new();
    public Dictionary<string, double> PercentageByRole { get; set; } = new();
}

public class OverrideUtilizationMetrics
{
    public int TotalOverrides { get; set; }
    public int ActiveOverrides { get; set; }
    public int ExpiredOverrides { get; set; }
    public double UtilizationRate { get; set; }
    public List<TrendDataPoint> Trend { get; set; } = new();
}

public class SecurityEventMetrics
{
    public int TotalEvents { get; set; }
    public int UnacknowledgedEvents { get; set; }
    public Dictionary<string, int> EventsByType { get; set; } = new();
    public Dictionary<string, int> EventsBySeverity { get; set; } = new();
    public List<TrendDataPoint> Trend { get; set; } = new();
}

public class AuditActivityMetrics
{
    public int TotalActions { get; set; }
    public int ActionsToday { get; set; }
    public Dictionary<string, int> ActionsByType { get; set; } = new();
    public List<TrendDataPoint> Trend { get; set; } = new();
}

public class PermissionChangeMetrics
{
    public int TotalChanges { get; set; }
    public int ChangesThisMonth { get; set; }
    public Dictionary<string, int> ChangesByType { get; set; } = new();
    public List<TrendDataPoint> Trend { get; set; } = new();
}

public class TrendDataPoint
{
    public string Label { get; set; } = string.Empty;
    public double Value { get; set; }
}

// ===== GIS Administration DTOs =====

public class GisConnectionStatusDto
{
    public bool IsConnected { get; set; }
    public string Status { get; set; } = string.Empty;
    public DateTime? LastSyncAt { get; set; }
    public DateTime? LastSuccessfulSyncAt { get; set; }
    public int FailedRecordsCount { get; set; }
    public int RetryQueueSize { get; set; }
    public string? LastError { get; set; }
}

public class GisSyncHistoryDto
{
    public Guid Id { get; set; }
    public string SyncType { get; set; } = string.Empty;
    public string Status { get; set; } = string.Empty;
    public int TotalRecords { get; set; }
    public int SuccessCount { get; set; }
    public int ErrorCount { get; set; }
    public DateTime StartedAt { get; set; }
    public DateTime? CompletedAt { get; set; }
    public string? ErrorMessage { get; set; }
}

public class ManualSyncRequest
{
    [Required, MaxLength(50)]
    public string SyncType { get; set; } = string.Empty;
    public DateTime? FromDate { get; set; }
    public DateTime? ToDate { get; set; }
}

// ===== SuperAdmin Dashboard DTOs =====

public class SuperAdminDashboardDto
{
    public int TotalEmployees { get; set; }
    public int ActiveUsers { get; set; }
    public int LockedAccounts { get; set; }
    public Dictionary<string, int> UsersByRole { get; set; } = new();
    public Dictionary<string, int> UsersByDepartment { get; set; } = new();
    public int ActiveOverrides { get; set; }
    public int ExpiredOverrides { get; set; }
    public int FailedLoginAttempts { get; set; }
    public int SecurityEvents { get; set; }
    public int AuditEventsToday { get; set; }
    public int OpenAttendanceExceptions { get; set; }
    public int OpenCorrectiveActions { get; set; }
    public int OpenEscalations { get; set; }
    public GisConnectionStatusDto? GisSyncStatus { get; set; }
    public SystemHealthDto SystemHealth { get; set; } = new();
}

public class SystemHealthDto
{
    public bool IsHealthy { get; set; }
    public string Status { get; set; } = string.Empty;
    public double DatabaseResponseTimeMs { get; set; }
    public int ActiveSessions { get; set; }
    public int ActiveLockdowns { get; set; }
    public DateTime LastHealthCheck { get; set; }
}

// ===== SuperAdmin Protection DTOs =====

public class SuperAdminCreationRequest
{
    [Required, MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;
    [Required, MaxLength(200)]
    public string DisplayName { get; set; } = string.Empty;
    [Required, MaxLength(50)]
    public string Department { get; set; } = string.Empty;
    [Required, MaxLength(500)]
    public string Reason { get; set; } = string.Empty;
}

public class SuperAdminProtectionDto
{
    public string ProtectedEmployeeId { get; set; } = "803341";
    public string DisplayName { get; set; } = "Botlhale Rakau";
    public string Role { get; set; } = "SuperAdmin";
    public string Department { get; set; } = "Production";
    public string SecurityScope { get; set; } = "Global";
    public bool IsProtected { get; set; } = true;
    public List<string> ProtectionRules { get; set; } = new()
    {
        "Cannot be deleted",
        "Cannot be disabled",
        "Cannot be downgraded",
        "Cannot lose SuperAdmin role",
        "Cannot be modified by non-SuperAdmin users",
        "All modification attempts must be audit logged"
    };
}
