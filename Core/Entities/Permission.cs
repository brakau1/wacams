namespace WACAMS.Core.Entities;

/// <summary>
/// Represents a permission in the system (e.g., "attendance.view.team", "warnings.create").
/// Permissions are grouped by module and assigned to roles via RolePermission junction table.
/// </summary>
public class Permission
{
    /// <summary>
    /// Surrogate primary key — GUID internally.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Unique permission name in dotted notation (e.g., "attendance.view.team").
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Name { get; set; } = string.Empty;

    /// <summary>
    /// Module this permission belongs to (e.g., "Attendance", "Warnings", "System").
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Module { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable description of what this permission grants.
    /// </summary>
    [MaxLength(200)]
    public string? Description { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property — roles that have been granted this permission.
    /// </summary>
    public ICollection<RolePermission> RolePermissions { get; set; } = new List<RolePermission>();
}

/// <summary>
/// Permission constants organized by module.
/// </summary>
public static class Permissions
{
    /// <summary>Attendance module permissions.</summary>
    public static class Attendance
    {
        public const string ViewOwn = "attendance.view.own";
        public const string ViewTeam = "attendance.view.team";
        public const string ViewDepartment = "attendance.view.department";
        public const string ViewAll = "attendance.view.all";
        public const string Edit = "attendance.edit";
    }

    /// <summary>Exceptions module permissions.</summary>
    public static class Exceptions
    {
        public const string ViewOwn = "exceptions.view.own";
        public const string ViewTeam = "exceptions.view.team";
        public const string ViewDepartment = "exceptions.view.department";
        public const string ViewAll = "exceptions.view.all";
        public const string Investigate = "exceptions.investigate";
        public const string Approve = "exceptions.approve";
        public const string Reject = "exceptions.reject";
    }

    /// <summary>Warnings module permissions.</summary>
    public static class Warnings
    {
        public const string ViewOwn = "warnings.view.own";
        public const string ViewTeam = "warnings.view.team";
        public const string ViewDepartment = "warnings.view.department";
        public const string ViewAll = "warnings.view.all";
        public const string Create = "warnings.create";
        public const string Upload = "warnings.upload";
        public const string Issue = "warnings.issue";
    }

    /// <summary>Escalations module permissions.</summary>
    public static class Escalations
    {
        public const string ViewOwn = "escalations.view.own";
        public const string ViewTeam = "escalations.view.team";
        public const string ViewDepartment = "escalations.view.department";
        public const string ViewAll = "escalations.view.all";
        public const string Manage = "escalations.manage";
    }

    /// <summary>Employees module permissions.</summary>
    public static class Employees
    {
        public const string ViewOwn = "employees.view.own";
        public const string ViewAll = "employees.view.all";
        public const string Create = "employees.create";
        public const string Edit = "employees.edit";
        public const string Delete = "employees.delete";
        public const string AssignRole = "employees.assign.role";
    }

    /// <summary>Reports module permissions.</summary>
    public static class Reports
    {
        public const string ViewOwn = "reports.view.own";
        public const string ViewTeam = "reports.view.team";
        public const string ViewDepartment = "reports.view.department";
        public const string ViewAll = "reports.view.all";
        public const string Generate = "reports.generate";
    }

    /// <summary>System module permissions.</summary>
    public static class System
    {
        public const string ViewLogs = "system.view.logs";
        public const string ManageSettings = "system.manage.settings";
        public const string ManageRoles = "system.manage.roles";
        public const string ManagePermissions = "system.manage.permissions";
        public const string ViewAuditTrail = "system.view.audit";
    }
}
