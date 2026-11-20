namespace WACAMS.Core.Enums;

/// <summary>
/// Represents the role-based access control (RBAC) roles in the system.
/// Roles are ordered by privilege level — higher values have broader permissions.
/// </summary>
public enum Role
{
    /// <summary>
    /// Super Administrator — unrestricted system-wide access, infrastructure configuration,
    /// user management, role assignment, and all system overrides.
    /// </summary>
    SuperAdmin = 7,

    /// <summary>
    /// Administrator — user management, all reports, system configuration, attendance and disciplinary rules.
    /// </summary>
    Admin = 6,

    /// <summary>
    /// Human Resources — attendance oversight, leave approvals, warning generation, employee records.
    /// </summary>
    HR = 5,

    /// <summary>
    /// Manager — department-level oversight, team management, approve rework, initiate disciplinary actions.
    /// </summary>
    Manager = 4,

    /// <summary>
    /// Supervisor — view production data within scope, manage team members, generate shift reports.
    /// </summary>
    Supervisor = 3,

    /// <summary>
    /// Operator — plant floor workers with no direct reports. Can view own attendance, exceptions, 
    /// corrective actions, warning forms, training records, profile, and notifications.
    /// </summary>
    Operator = 2,

    /// <summary>
    /// User — basic attendance clock-in/out, view own records only.
    /// </summary>
    User = 1
}
