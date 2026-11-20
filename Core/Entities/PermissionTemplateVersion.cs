using System.ComponentModel.DataAnnotations;

namespace WACAMS.Core.Entities;

/// <summary>
/// Version history for permission templates.
/// Tracks all changes to templates for audit and rollback purposes.
/// </summary>
public class PermissionTemplateVersion
{
    /// <summary>
    /// Surrogate primary key.
    /// </summary>
    public Guid Id { get; set; }

    /// <summary>
    /// Foreign key to the permission template.
    /// </summary>
    public Guid TemplateId { get; set; }

    /// <summary>
    /// Version number.
    /// </summary>
    public int VersionNumber { get; set; }

    /// <summary>
    /// JSON string containing permission definitions at this version.
    /// </summary>
    [Required]
    public string PermissionsJson { get; set; } = "{}";

    /// <summary>
    /// Change description or reason for this version.
    /// </summary>
    [MaxLength(500)]
    public string? ChangeDescription { get; set; }

    /// <summary>
    /// Employee ID who created this version.
    /// </summary>
    [Required]
    [MaxLength(50)]
    public string CreatedBy { get; set; } = string.Empty;

    /// <summary>
    /// Version creation timestamp.
    /// </summary>
    public DateTime CreatedAt { get; set; } = DateTime.UtcNow;

    /// <summary>
    /// Navigation property to the parent template.
    /// </summary>
    public PermissionTemplate? Template { get; set; }
}
