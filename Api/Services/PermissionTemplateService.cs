using Microsoft.EntityFrameworkCore;
using Wacam.Api.Data;
using Wacam.Api.DTOs;
using WACAMS.Core.Entities;

namespace Wacam.Api.Services;

/// <summary>
/// Service for managing permission templates.
/// </summary>
public class PermissionTemplateService
{
    private readonly WacamDbContext _context;
    private readonly AuditService _auditService;

    public PermissionTemplateService(WacamDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    /// <summary>
    /// Get all permission templates.
    /// </summary>
    public async Task<List<PermissionTemplateDto>> GetAllTemplatesAsync()
    {
        var templates = await _context.PermissionTemplates
            .Include(t => t.TargetDepartment)
            .Include(t => t.Assignments)
            .OrderBy(t => t.Name)
            .ToListAsync();

        return templates.Select(t => new PermissionTemplateDto
        {
            Id = t.Id,
            Name = t.Name,
            Description = t.Description,
            Version = t.Version,
            IsActive = t.IsActive,
            TargetRole = t.TargetRole,
            TargetDepartmentId = t.TargetDepartmentId,
            TargetDepartmentName = t.TargetDepartment?.Name,
            Permissions = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, bool>>>(t.PermissionsJson) ?? new(),
            CreatedBy = t.CreatedBy,
            ModifiedBy = t.ModifiedBy,
            CreatedAt = t.CreatedAt,
            ModifiedAt = t.ModifiedAt,
            AssignmentCount = t.Assignments.Count(a => a.IsActive)
        }).ToList();
    }

    /// <summary>
    /// Get template by ID.
    /// </summary>
    public async Task<PermissionTemplateDto?> GetTemplateByIdAsync(Guid id)
    {
        var template = await _context.PermissionTemplates
            .Include(t => t.TargetDepartment)
            .Include(t => t.Assignments)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null) return null;

        return new PermissionTemplateDto
        {
            Id = template.Id,
            Name = template.Name,
            Description = template.Description,
            Version = template.Version,
            IsActive = template.IsActive,
            TargetRole = template.TargetRole,
            TargetDepartmentId = template.TargetDepartmentId,
            TargetDepartmentName = template.TargetDepartment?.Name,
            Permissions = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, bool>>>(template.PermissionsJson) ?? new(),
            CreatedBy = template.CreatedBy,
            ModifiedBy = template.ModifiedBy,
            CreatedAt = template.CreatedAt,
            ModifiedAt = template.ModifiedAt,
            AssignmentCount = template.Assignments.Count(a => a.IsActive)
        };
    }

    /// <summary>
    /// Create a new permission template.
    /// </summary>
    public async Task<PermissionTemplateDto> CreateTemplateAsync(CreatePermissionTemplateRequest request, string createdBy)
    {
        var template = new PermissionTemplate
        {
            Id = Guid.NewGuid(),
            Name = request.Name,
            Description = request.Description,
            Version = 1,
            IsActive = true,
            TargetRole = request.TargetRole,
            TargetDepartmentId = request.TargetDepartmentId,
            PermissionsJson = System.Text.Json.JsonSerializer.Serialize(request.Permissions),
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.PermissionTemplates.Add(template);

        // Create initial version
        var version = new PermissionTemplateVersion
        {
            Id = Guid.NewGuid(),
            TemplateId = template.Id,
            VersionNumber = 1,
            PermissionsJson = template.PermissionsJson,
            ChangeDescription = "Initial version",
            CreatedBy = createdBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.PermissionTemplateVersions.Add(version);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "PermissionTemplateCreated",
            "SuperAdmin",
            "PermissionTemplate",
            template.Id.ToString(),
            $"Created permission template: {template.Name}",
            null,
            new { template.Name, template.TargetRole, template.TargetDepartmentId },
            createdBy);

        return (await GetTemplateByIdAsync(template.Id))!;
    }

    /// <summary>
    /// Update an existing permission template.
    /// </summary>
    public async Task<PermissionTemplateDto?> UpdateTemplateAsync(Guid id, UpdatePermissionTemplateRequest request, string modifiedBy)
    {
        var template = await _context.PermissionTemplates
            .Include(t => t.Versions)
            .FirstOrDefaultAsync(t => t.Id == id);

        if (template == null) return null;

        var oldPermissions = template.PermissionsJson;

        if (request.Description != null) template.Description = request.Description;
        if (request.TargetRole != null) template.TargetRole = request.TargetRole;
        if (request.TargetDepartmentId.HasValue) template.TargetDepartmentId = request.TargetDepartmentId;
        if (request.IsActive.HasValue) template.IsActive = request.IsActive.Value;
        
        if (request.Permissions != null)
        {
            template.PermissionsJson = System.Text.Json.JsonSerializer.Serialize(request.Permissions);
            template.Version++;

            // Create new version
            var version = new PermissionTemplateVersion
            {
                Id = Guid.NewGuid(),
                TemplateId = template.Id,
                VersionNumber = template.Version,
                PermissionsJson = template.PermissionsJson,
                ChangeDescription = $"Updated to version {template.Version}",
                CreatedBy = modifiedBy,
                CreatedAt = DateTime.UtcNow
            };

            _context.PermissionTemplateVersions.Add(version);
        }

        template.ModifiedBy = modifiedBy;
        template.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "PermissionTemplateUpdated",
            "SuperAdmin",
            "PermissionTemplate",
            template.Id.ToString(),
            $"Updated permission template: {template.Name}",
            new { OldPermissions = oldPermissions },
            new { template.Name, template.Version, template.IsActive },
            modifiedBy);

        return await GetTemplateByIdAsync(id);
    }

    /// <summary>
    /// Clone an existing permission template.
    /// </summary>
    public async Task<PermissionTemplateDto?> CloneTemplateAsync(Guid sourceId, ClonePermissionTemplateRequest request, string clonedBy)
    {
        var source = await _context.PermissionTemplates
            .Include(t => t.TargetDepartment)
            .FirstOrDefaultAsync(t => t.Id == sourceId);

        if (source == null) return null;

        var clone = new PermissionTemplate
        {
            Id = Guid.NewGuid(),
            Name = request.NewName,
            Description = request.Description ?? source.Description,
            Version = 1,
            IsActive = true,
            TargetRole = source.TargetRole,
            TargetDepartmentId = source.TargetDepartmentId,
            PermissionsJson = source.PermissionsJson,
            CreatedBy = clonedBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.PermissionTemplates.Add(clone);

        // Create initial version for clone
        var version = new PermissionTemplateVersion
        {
            Id = Guid.NewGuid(),
            TemplateId = clone.Id,
            VersionNumber = 1,
            PermissionsJson = clone.PermissionsJson,
            ChangeDescription = $"Cloned from template: {source.Name}",
            CreatedBy = clonedBy,
            CreatedAt = DateTime.UtcNow
        };

        _context.PermissionTemplateVersions.Add(version);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "PermissionTemplateCloned",
            "SuperAdmin",
            "PermissionTemplate",
            clone.Id.ToString(),
            $"Cloned permission template '{source.Name}' to '{clone.Name}'",
            null,
            new { SourceTemplateId = sourceId, SourceTemplateName = source.Name, NewTemplateName = clone.Name },
            clonedBy);

        return await GetTemplateByIdAsync(clone.Id);
    }

    /// <summary>
    /// Deactivate a permission template.
    /// </summary>
    public async Task<bool> DeactivateTemplateAsync(Guid id, string deactivatedBy)
    {
        var template = await _context.PermissionTemplates.FindAsync(id);
        if (template == null) return false;

        template.IsActive = false;
        template.ModifiedBy = deactivatedBy;
        template.ModifiedAt = DateTime.UtcNow;

        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "PermissionTemplateDeactivated",
            "SuperAdmin",
            "PermissionTemplate",
            template.Id.ToString(),
            $"Deactivated permission template: {template.Name}",
            null,
            new { template.Name },
            deactivatedBy);

        return true;
    }

    /// <summary>
    /// Assign template to employees.
    /// </summary>
    public async Task<int> AssignTemplateAsync(Guid templateId, AssignTemplateRequest request, string assignedBy)
    {
        var template = await _context.PermissionTemplates.FindAsync(templateId);
        if (template == null) return 0;

        var assignments = new List<PermissionTemplateAssignment>();

        foreach (var employeeId in request.EmployeeIds)
        {
            var employee = await _context.Employees.FindAsync(employeeId);
            if (employee == null) continue;

            // Check if assignment already exists
            var existing = await _context.PermissionTemplateAssignments
                .FirstOrDefaultAsync(a => a.TemplateId == templateId && a.EmployeeId == employeeId && a.IsActive);

            if (existing != null) continue;

            var assignment = new PermissionTemplateAssignment
            {
                Id = Guid.NewGuid(),
                TemplateId = templateId,
                EmployeeId = employeeId,
                IsActive = true,
                AssignedAt = DateTime.UtcNow,
                ExpiresAt = request.ExpiresAt,
                AssignedBy = assignedBy,
                Reason = request.Reason
            };

            assignments.Add(assignment);
        }

        _context.PermissionTemplateAssignments.AddRange(assignments);
        await _context.SaveChangesAsync();

        await _auditService.LogAsync(
            "PermissionTemplateAssigned",
            "SuperAdmin",
            "PermissionTemplate",
            templateId.ToString(),
            $"Assigned template '{template.Name}' to {assignments.Count} employees",
            null,
            new { TemplateName = template.Name, EmployeeCount = assignments.Count, EmployeeIds = request.EmployeeIds },
            assignedBy);

        return assignments.Count;
    }

    /// <summary>
    /// Get template version history.
    /// </summary>
    public async Task<List<PermissionTemplateVersionDto>> GetTemplateVersionsAsync(Guid templateId)
    {
        var versions = await _context.PermissionTemplateVersions
            .Where(v => v.TemplateId == templateId)
            .OrderByDescending(v => v.VersionNumber)
            .ToListAsync();

        return versions.Select(v => new PermissionTemplateVersionDto
        {
            Id = v.Id,
            TemplateId = v.TemplateId,
            VersionNumber = v.VersionNumber,
            Permissions = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, bool>>>(v.PermissionsJson) ?? new(),
            ChangeDescription = v.ChangeDescription,
            CreatedBy = v.CreatedBy,
            CreatedAt = v.CreatedAt
        }).ToList();
    }
}
