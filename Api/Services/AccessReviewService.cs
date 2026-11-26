using Microsoft.EntityFrameworkCore;
using WACAMS.Api.Data;
using WACAMS.Api.DTOs;
using WACAMS.Core.Entities;

namespace WACAMS.Api.Services;

/// <summary>
/// Service for access review and snapshot management.
/// </summary>
public class AccessReviewService
{
    private readonly WacamDbContext _context;
    private readonly AuditService _auditService;

    public AccessReviewService(WacamDbContext context, AuditService auditService)
    {
        _context = context;
        _auditService = auditService;
    }

    /// <summary>
    /// Capture access snapshot for a user.
    /// </summary>
    public async Task<AccessReviewSnapshotDto> CaptureSnapshotAsync(
        string employeeId,
        string capturedBy,
        string? reason = null)
    {
        var user = await _context.Users
            .Include(u => u.Employee)
                .ThenInclude(e => e!.Department)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);

        if (user == null)
            throw new KeyNotFoundException($"User {employeeId} not found");

        // Get assigned permissions
        var assignedPermissions = new Dictionary<string, Dictionary<string, bool>>();
        
        // Get inherited permissions from roles
        var inheritedPermissions = new Dictionary<string, Dictionary<string, bool>>();
        
        // Get active overrides
        var overrides = await _context.EmployeeAccessOverrides
            .Where(o => o.EmployeeId == employeeId && o.IsActive)
            .ToListAsync();

        var overrideList = overrides.Select(o => o.OverrideRole).ToList();

        // Get last login
        var lastLogin = await _context.UserSessions
            .Where(s => s.EmployeeId == employeeId)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync();

        // Get last role change
        var lastRoleChange = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .OrderByDescending(ur => ur.AssignedAt)
            .FirstOrDefaultAsync();

        // Get last permission change from audit log
        var lastPermissionChange = await _context.AuditLogs
            .Where(a => a.EntityId == employeeId && 
                       (a.Action == "PermissionChange" || a.Action == "RoleAssignment"))
            .OrderByDescending(a => a.PerformedAt)
            .FirstOrDefaultAsync();

        var snapshot = new AccessReviewSnapshot
        {
            Id = Guid.NewGuid(),
            EmployeeId = employeeId,
            DisplayName = user.Employee?.DisplayName ?? user.DisplayName ?? employeeId,
            Department = user.Employee?.Department?.Name,
            Role = user.UserRoles.FirstOrDefault()?.Role?.Name ?? "User",
            AssignedPermissionsJson = System.Text.Json.JsonSerializer.Serialize(assignedPermissions),
            InheritedPermissionsJson = System.Text.Json.JsonSerializer.Serialize(inheritedPermissions),
            OverridesJson = System.Text.Json.JsonSerializer.Serialize(overrideList),
            LastLoginAt = lastLogin?.StartedAt,
            LastRoleChangeAt = lastRoleChange?.AssignedAt,
            LastPermissionChangeAt = lastPermissionChange?.PerformedAt,
            SnapshotAt = DateTime.UtcNow,
            CapturedBy = capturedBy,
            Reason = reason
        };

        _context.AccessReviewSnapshots.Add(snapshot);
        await _context.SaveChangesAsync();

        await _auditService.LogActionAsync(
            capturedBy,
            "AccessSnapshotCaptured",
            "AccessReviewSnapshot",
            snapshot.Id.ToString(),
            $"Captured access snapshot for user {employeeId}",
            System.Text.Json.JsonSerializer.Serialize(new { employeeId, reason }));

        return new AccessReviewSnapshotDto
        {
            Id = snapshot.Id,
            EmployeeId = snapshot.EmployeeId,
            DisplayName = snapshot.DisplayName,
            Department = snapshot.Department,
            Role = snapshot.Role,
            AssignedPermissions = assignedPermissions,
            InheritedPermissions = inheritedPermissions,
            ActiveOverrides = overrideList,
            DepartmentScope = snapshot.Department,
            LastLoginAt = snapshot.LastLoginAt,
            LastRoleChangeAt = snapshot.LastRoleChangeAt,
            LastPermissionChangeAt = snapshot.LastPermissionChangeAt,
            SnapshotDate = snapshot.SnapshotAt,
            CapturedBy = snapshot.CapturedBy
        };
    }

    /// <summary>
    /// Get access review snapshots for a user.
    /// </summary>
    public async Task<List<AccessReviewSnapshotDto>> GetSnapshotsAsync(
        string employeeId,
        int limit = 50)
    {
        var snapshots = await _context.AccessReviewSnapshots
            .Where(s => s.EmployeeId == employeeId)
            .OrderByDescending(s => s.SnapshotAt)
            .Take(limit)
            .ToListAsync();

        return snapshots.Select(s => new AccessReviewSnapshotDto
        {
            Id = s.Id,
            EmployeeId = s.EmployeeId,
            DisplayName = s.DisplayName,
            Department = s.Department,
            Role = s.Role,
            AssignedPermissions = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, bool>>>(s.AssignedPermissionsJson) ?? new(),
            InheritedPermissions = System.Text.Json.JsonSerializer.Deserialize<Dictionary<string, Dictionary<string, bool>>>(s.InheritedPermissionsJson) ?? new(),
            ActiveOverrides = System.Text.Json.JsonSerializer.Deserialize<List<string>>(s.OverridesJson) ?? new(),
            DepartmentScope = s.Department,
            LastLoginAt = s.LastLoginAt,
            LastRoleChangeAt = s.LastRoleChangeAt,
            LastPermissionChangeAt = s.LastPermissionChangeAt,
            SnapshotDate = s.SnapshotAt,
            CapturedBy = s.CapturedBy
        }).ToList();
    }

    /// <summary>
    /// Get current user access explorer data.
    /// </summary>
    public async Task<UserAccessExplorerDto?> GetUserAccessAsync(string employeeId)
    {
        var user = await _context.Users
            .Include(u => u.Employee)
                .ThenInclude(e => e!.Department)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);

        if (user == null) return null;

        // Get assigned permissions
        var assignedPermissions = new Dictionary<string, Dictionary<string, bool>>();
        
        // Get inherited permissions from roles
        var inheritedPermissions = new Dictionary<string, Dictionary<string, bool>>();
        
        // Get active overrides
        var overrides = await _context.EmployeeAccessOverrides
            .Where(o => o.EmployeeId == employeeId && o.IsActive)
            .ToListAsync();

        var overrideDtos = overrides.Select(o => new EmployeeAccessOverrideDto
        {
            Id = o.Id,
            EmployeeId = o.EmployeeId,
            PermissionName = o.OverrideRole,
            GrantedBy = o.CreatedByEmployeeId,
            Reason = o.Notes,
            IsActive = o.IsActive,
            CreatedAt = o.CreatedDate,
            ExpiresAt = o.EffectiveTo
        }).ToList();

        // Get last login
        var lastLogin = await _context.UserSessions
            .Where(s => s.EmployeeId == employeeId)
            .OrderByDescending(s => s.StartedAt)
            .FirstOrDefaultAsync();

        // Get last role change
        var lastRoleChange = await _context.UserRoles
            .Where(ur => ur.UserId == user.Id)
            .OrderByDescending(ur => ur.AssignedAt)
            .FirstOrDefaultAsync();

        // Get last permission change from audit log
        var lastPermissionChange = await _context.AuditLogs
            .Where(a => a.EntityId == employeeId && 
                       (a.Action == "PermissionChange" || a.Action == "RoleAssignment"))
            .OrderByDescending(a => a.PerformedAt)
            .FirstOrDefaultAsync();

        return new UserAccessExplorerDto
        {
            EmployeeId = employeeId,
            DisplayName = user.Employee?.DisplayName ?? user.DisplayName ?? employeeId,
            Department = user.Employee?.Department?.Name,
            Role = user.UserRoles.FirstOrDefault()?.Role?.Name ?? "User",
            AssignedPermissions = assignedPermissions,
            InheritedPermissions = inheritedPermissions,
            Overrides = overrideDtos,
            LastLoginAt = lastLogin?.StartedAt,
            LastRoleChangeAt = lastRoleChange?.AssignedAt,
            LastPermissionChangeAt = lastPermissionChange?.PerformedAt
        };
    }

    /// <summary>
    /// Get all user access explorer data.
    /// </summary>
    public async Task<List<UserAccessExplorerDto>> GetAllUserAccessAsync()
    {
        var users = await _context.Users
            .Include(u => u.Employee)
                .ThenInclude(e => e!.Department)
            .Include(u => u.UserRoles)
                .ThenInclude(ur => ur.Role)
            .ToListAsync();

        var result = new List<UserAccessExplorerDto>();

        foreach (var user in users)
        {
            var overrides = await _context.EmployeeAccessOverrides
                .Where(o => o.EmployeeId == user.EmployeeId && o.IsActive)
                .ToListAsync();

            var overrideDtos = overrides.Select(o => new EmployeeAccessOverrideDto
            {
                Id = o.Id,
                EmployeeId = o.EmployeeId,
                PermissionName = o.OverrideRole,
                GrantedBy = o.CreatedByEmployeeId,
                Reason = o.Notes,
                IsActive = o.IsActive,
                CreatedAt = o.CreatedDate,
                ExpiresAt = o.EffectiveTo
            }).ToList();

            var lastLogin = await _context.UserSessions
                .Where(s => s.EmployeeId == user.EmployeeId)
                .OrderByDescending(s => s.StartedAt)
                .FirstOrDefaultAsync();

            var lastRoleChange = await _context.UserRoles
                .Where(ur => ur.UserId == user.Id)
                .OrderByDescending(ur => ur.AssignedAt)
                .FirstOrDefaultAsync();

            var lastPermissionChange = await _context.AuditLogs
                .Where(a => a.EntityId == user.EmployeeId && 
                           (a.Action == "PermissionChange" || a.Action == "RoleAssignment"))
                .OrderByDescending(a => a.PerformedAt)
                .FirstOrDefaultAsync();

            result.Add(new UserAccessExplorerDto
            {
                EmployeeId = user.EmployeeId,
                DisplayName = user.Employee?.DisplayName ?? user.DisplayName ?? user.EmployeeId,
                Department = user.Employee?.Department?.Name,
                Role = user.UserRoles.FirstOrDefault()?.Role?.Name ?? "User",
                AssignedPermissions = new Dictionary<string, Dictionary<string, bool>>(),
                InheritedPermissions = new Dictionary<string, Dictionary<string, bool>>(),
                Overrides = overrideDtos,
                LastLoginAt = lastLogin?.StartedAt,
                LastRoleChangeAt = lastRoleChange?.AssignedAt,
                LastPermissionChangeAt = lastPermissionChange?.PerformedAt
            });
        }

        return result;
    }
}
