using WACAMS.Core.Enums;

namespace WACAMS.Core.Entities;

/// <summary>
/// Represents a system user account linked to an employee.
/// Each active employee has exactly one user account for authentication and RBAC.
/// The EmployeeId serves as the foreign key linking this account to the master employee record.
/// </summary>
public class User
{
    /// <summary>
    /// Surrogate primary key — GUID internally.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key referencing the Employee this user account belongs to.
    /// One-to-one relationship with Employee.EmployeeId.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string EmployeeId { get; set; } = string.Empty;

    /// <summary>
    /// Login username — can be the employee ID, email address, or a separate login alias.
    /// </summary>
    [Required]
    [MaxLength(100)]
    public string Username { get; set; } = string.Empty;

    /// <summary>
    /// Email address for login and notifications.
    /// </summary>
    [MaxLength(255)]
    public string? Email { get; set; }

    /// <summary>
    /// Display name for UI presentation.
    /// </summary>
    [MaxLength(200)]
    public string? DisplayName { get; set; }

    /// <summary>
    /// Hashed password using ASP.NET Core Identity-compatible hashing (PBKDF2 with HMAC-SHA256).
    /// Never store plain-text passwords. Use BCrypt or Argon2id for additional security.
    /// </summary>
    [Required]
    public string PasswordHash { get; set; } = string.Empty;

    /// <summary>
    /// Role-based access control assignment for this user.
    /// Determines what data and actions are accessible within the system.
    /// </summary>
    [Required]
    public Enums.Role Role { get; set; }

    /// <summary>
    /// Whether the user account is currently active and allowed to authenticate.
    /// Inactive accounts cannot log in but retain historical audit trail data.
    /// </summary>
    public bool IsActive { get; set; } = true;

    /// <summary>
    /// Whether the user account is locked by an administrator.
    /// Locked accounts cannot log in until unlocked by SuperAdmin.
    /// </summary>
    public bool IsLocked { get; set; } = false;

    /// <summary>
    /// Timestamp of the last successful login.
    /// Null if the user has never logged in.
    /// </summary>
    public DateTime? LastLoginAt { get; set; }

    /// <summary>
    /// Whether the user must change their password on next login.
    /// Used for initial account setup and security resets.
    /// </summary>
    public bool MustChangePassword { get; set; } = false;

    // Alias properties for compatibility
        public int? DepartmentId { get; set; }
        public Department? Department { get; set; }
        public DateTime? SignedAt { get; set; }

    /// <summary>
    /// Record creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Record last update timestamp.
    /// </summary>
    public DateTime? UpdatedAt { get; set; }

    /// <summary>
    /// Navigation property — the employee this user account is linked to.
    /// One-to-one relationship.
    /// </summary>
    public Employee? Employee { get; set; }
}
