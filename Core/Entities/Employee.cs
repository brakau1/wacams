using WACAMS.Core.Enums;

namespace WACAMS.Core.Entities;

/// <summary>
/// Represents an employee in the workforce management system.
/// EmployeeId is the master key — used as the primary business identifier across all tables.
/// DisplayName is for display purposes only and does not affect identity resolution.
/// </summary>
public class Employee
{
    /// <summary>
    /// Surrogate primary key — GUID internally, never exposed externally.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Master business key — unique employee identifier (e.g., "708513").
    /// Used as the primary link between all attendance, warnings, and user records.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>
    /// Human-readable display name shown in dashboards, reports, and UI elements.
    /// Not used for identity resolution — EmployeeId is the authoritative identifier.
    /// </summary>
    [Required]
    [MaxLength(150)]
    public string DisplayName { get; set; } = string.Empty;

    /// <summary>
    /// Employee email address for notifications and login credentials.
    /// </summary>
    [EmailAddress]
    [MaxLength(200)]
    public string? Email { get; set; }

    /// <summary>
    /// Employee phone number for SMS notifications and emergency contact.
    /// </summary>
    [MaxLength(20)]
    public string? Phone { get; set; }

    /// <summary>
    /// Microsoft Teams user ID for Teams notifications.
    /// </summary>
    [MaxLength(200)]
    public string? TeamsId { get; set; }

    /// <summary>
    /// Foreign key referencing the Department this employee belongs to.
    /// </summary>
    public Guid DepartmentId { get; set; }

    /// <summary>
    /// The department this employee is assigned to.
    /// </summary>
    public Department? Department { get; set; }

    /// <summary>
    /// Manager's EmployeeId — establishes the supervisor hierarchy for escalation chains.
    /// Nullable for top-level managers (e.g., Production Manager, Director).
    /// Must reference another Employee's EmployeeId within the same organization.
    /// </summary>
    [MaxLength(50)]
    public string? ManagerId { get; set; }

    /// <summary>
    /// Current employment status of the employee.
    /// </summary>
    public EmployeeStatus Status { get; set; }

    /// <summary>
    /// Date the employee was hired or onboarded into the system.
    /// </summary>
    public DateTime HireDate { get; set; }

    /// <summary>
    /// Default shift code assigned to this employee (ES, MS, NS, LDS, DS).
    /// May be overridden by individual ShiftSchedule entries for specific dates.
    /// </summary>
    [MaxLength(10)]
    public string? ShiftCode { get; set; }

    /// <summary>
    /// Navigation property — collection of all attendance records for this employee.
    /// </summary>
    public ICollection<AttendanceRecord> AttendanceRecords { get; set; } = new List<AttendanceRecord>();

    /// <summary>
    /// Navigation property — collection of all attendance exceptions logged for this employee.
    /// </summary>
    public ICollection<AttendanceException> Exceptions { get; set; } = new List<AttendanceException>();

    /// <summary>
    /// Navigation property — collection of all warning letters issued to this employee.
    /// </summary>
    public ICollection<WarningLetter> WarningLetters { get; set; } = new List<WarningLetter>();

    /// <summary>
    /// Navigation property — collection of all escalations involving this employee.
    /// </summary>
    public ICollection<Escalation> Escalations { get; set; } = new List<Escalation>();

    /// <summary>
    /// Navigation property — the user account linked to this employee.
    /// One-to-one relationship; every active employee should have a user account.
    /// </summary>
    public User? User { get; set; }

    /// <summary>
    /// Navigation property — list of direct reports (employees who have this employee's EmployeeId as their ManagerId).
    /// </summary>
    public ICollection<Employee> DirectReports { get; set; } = new List<Employee>();

    /// <summary>
    /// System role assigned to this employee (SuperAdmin, Admin, Supervisor, HR, Operator).
    /// Controls RBAC permissions across the application.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string Role { get; set; } = "Operator";

    /// <summary>
    /// Hashed password for authentication. Never store plain-text passwords.
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Refresh token value stored for active sessions.
    /// </summary>
    [MaxLength(512)]
    public string? RefreshToken { get; set; }

    /// <summary>
    /// Expiry timestamp of the current refresh token.
    /// </summary>
    public DateTime? RefreshTokenExpiry { get; set; }

    /// <summary>
    /// Whether this employee account is currently active and allowed to authenticate.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Consecutive failed login attempt counter. Reset to 0 on successful login.
    /// </summary>
    public int FailedLoginCount { get; set; } = 0;

    /// <summary>
    /// Timestamp of the last failed login attempt. Used to evaluate the 30-minute sliding window.
    /// </summary>
    public DateTime? LastFailedLoginAt { get; set; }

    /// <summary>
    /// If set, the account is locked until this UTC timestamp. Null means not locked.
    /// Lock duration: 30 minutes from trigger time.
    /// </summary>
    public DateTime? LockoutEnd { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Record last update timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }
}
