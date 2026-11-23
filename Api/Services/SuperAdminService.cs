using Microsoft.EntityFrameworkCore;
using Wacam.Api.Data;
using Wacam.Api.DTOs;
using WACAMS.Core.Entities;
using WACAMS.Core.Enums;
using Wacam.Api.Utils;

namespace Wacam.Api.Services;

/// <summary>
/// SuperAdmin service for managing users, roles, access control, and employee overrides.
/// All operations are audit logged for security compliance.
/// </summary>
public class SuperAdminService
{
    /// <summary>
    /// Protected SuperAdmin Employee ID - cannot be modified, locked, or downgraded
    /// </summary>
    private const string ProtectedSuperAdminId = "803341";

    private readonly WacamDbContext _context;
    private readonly AuditService _auditService;
    private readonly ILogger<SuperAdminService> _logger;

    public SuperAdminService(
        WacamDbContext context,
        AuditService auditService,
        ILogger<SuperAdminService> logger)
    {
        _context = context;
        _auditService = auditService;
        _logger = logger;
    }

    /// <summary>
    /// Validates that the target user is not the protected SuperAdmin account
    /// </summary>
    private void ValidateNotProtectedSuperAdmin(string employeeId, string action)
    {
        if (employeeId == ProtectedSuperAdminId)
        {
            throw new InvalidOperationException($"Cannot {action} the protected SuperAdmin account (Employee ID: {ProtectedSuperAdminId})");
        }
    }

    #region User Management

    /// <summary>
    /// Get all users with pagination and filtering
    /// </summary>
    public async Task<(List<UserManagementDto> Users, int TotalCount)> GetAllUsersAsync(
        string? search = null,
        string? role = null,
        bool? isActive = null,
        int page = 1,
        int pageSize = 50)
    {
        var query = _context.Users
            .Include(u => u.Employee)
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(search))
        {
            query = query.Where(u =>
                u.EmployeeId.Contains(search) ||
                u.DisplayName.Contains(search) ||
                (u.Email != null && u.Email.Contains(search)));
        }

        if (!string.IsNullOrWhiteSpace(role))
        {
            query = query.Where(u => u.Role.ToString() == role);
        }

        if (isActive.HasValue)
        {
            query = query.Where(u => u.IsActive == isActive.Value);
        }

        var totalCount = await query.CountAsync();

        var users = await query
            .OrderByDescending(u => u.CreatedAt)
            .Skip((page - 1) * pageSize)
            .Take(pageSize)
            .Select(u => new UserManagementDto
            {
                Id = u.Id,
                EmployeeId = u.EmployeeId,
                DisplayName = u.DisplayName,
                Email = u.Email,
                Department = u.Employee != null ? u.Employee.Department.Name : null,
                Role = u.Role.ToString(),
                IsActive = u.IsActive,
                IsLocked = u.IsLocked,
                MustChangePassword = u.MustChangePassword,
                CreatedAt = u.CreatedAt,
                LastLoginAt = u.LastLoginAt,
                UpdatedAt = u.UpdatedAt
            })
            .ToListAsync();

        return (users, totalCount);
    }

    /// <summary>
    /// Get user by EmployeeId
    /// </summary>
    public async Task<UserManagementDto?> GetUserByEmployeeIdAsync(string employeeId)
    {
        var user = await _context.Users
            .Include(u => u.Employee)
            .FirstOrDefaultAsync(u => u.EmployeeId == employeeId);

        if (user == null) return null;

        return new UserManagementDto
        {
            Id = user.Id,
            EmployeeId = user.EmployeeId,
            DisplayName = user.DisplayName,
            Email = user.Email,
            Department = user.Employee?.Department.Name,
            Role = user.Role.ToString(),
            IsActive = user.IsActive,
            IsLocked = user.IsLocked,
            MustChangePassword = user.MustChangePassword,
            CreatedAt = user.CreatedAt,
            LastLoginAt = user.LastLoginAt,
            UpdatedAt = user.UpdatedAt
        };
    }

    /// <summary>
    /// Register a new user (SuperAdmin only)
    /// </summary>
    public async Task<UserManagementDto> RegisterUserAsync(RegisterUserRequest request, string adminEmployeeId)
    {
        // Check if user already exists
        if (await _context.Users.AnyAsync(u => u.EmployeeId == request.EmployeeId))
        {
            throw new InvalidOperationException($"User with EmployeeId {request.EmployeeId} already exists");
        }

        // Validate role
        if (!Enum.TryParse<Role>(request.Role, true, out var role))
        {
            throw new InvalidOperationException($"Invalid role: {request.Role}");
        }

        // Prevent SuperAdmin role assignment through this method
        if (role == Role.SuperAdmin)
        {
            throw new InvalidOperationException("SuperAdmin role cannot be assigned through user registration");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            DisplayName = request.DisplayName,
            Email = request.Email,
            PasswordHash = PasswordHasher.HashPassword(request.Password),
            Role = role,
            IsActive = true,
            IsLocked = false,
            MustChangePassword = request.MustChangePassword,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogActionAsync(
            adminEmployeeId,
            "UserCreated",
            "User",
            user.EmployeeId,
            changesJson: $"{{\"action\":\"register\",\"role\":\"{role}\"}}"
        );

        _logger.LogInformation("User {EmployeeId} registered by admin {AdminId}", request.EmployeeId, adminEmployeeId);

        return await GetUserByEmployeeIdAsync(request.EmployeeId) ?? throw new Exception("Failed to retrieve created user");
    }

    /// <summary>
    /// Update user details
    /// </summary>
    public async Task<UserManagementDto> UpdateUserAsync(string employeeId, UpdateUserRequest request, string adminEmployeeId)
    {
        // Protect the primary SuperAdmin account
        ValidateNotProtectedSuperAdmin(employeeId, "update");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User {employeeId} not found");
        }

        var changes = new List<string>();

        if (request.DisplayName != null && request.DisplayName != user.DisplayName)
        {
            user.DisplayName = request.DisplayName;
            changes.Add($"DisplayName:{request.DisplayName}");
        }

        if (request.Email != null && request.Email != user.Email)
        {
            user.Email = request.Email;
            changes.Add($"Email:{request.Email}");
        }

        if (request.Role != null)
        {
            if (!Enum.TryParse<Role>(request.Role, true, out var newRole))
            {
                throw new InvalidOperationException($"Invalid role: {request.Role}");
            }

            var oldRole = user.Role.ToString();
            user.Role = newRole;
            changes.Add($"Role:{oldRole}->{newRole}");
        }

        if (request.IsActive.HasValue && request.IsActive.Value != user.IsActive)
        {
            user.IsActive = request.IsActive.Value;
            changes.Add($"IsActive:{request.IsActive.Value}");
        }

        if (request.IsLocked.HasValue && request.IsLocked.Value != user.IsLocked)
        {
            user.IsLocked = request.IsLocked.Value;
            changes.Add($"IsLocked:{request.IsLocked.Value}");
        }

        if (request.MustChangePassword.HasValue && request.MustChangePassword.Value != user.MustChangePassword)
        {
            user.MustChangePassword = request.MustChangePassword.Value;
            changes.Add($"MustChangePassword:{request.MustChangePassword.Value}");
        }

        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogActionAsync(
            adminEmployeeId,
            "UserUpdated",
            "User",
            employeeId,
            changesJson: $"{{\"changes\":[{string.Join(",", changes.Select(c => $"\"{c}\""))}]}}"
        );

        _logger.LogInformation("User {EmployeeId} updated by admin {AdminId}", employeeId, adminEmployeeId);

        return await GetUserByEmployeeIdAsync(employeeId) ?? throw new Exception("Failed to retrieve updated user");
    }

    /// <summary>
    /// Reset user password
    /// </summary>
    public async Task ResetPasswordAsync(string employeeId, ResetPasswordRequest request, string adminEmployeeId)
    {
        // Protect the primary SuperAdmin account
        ValidateNotProtectedSuperAdmin(employeeId, "reset password for");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User {employeeId} not found");
        }

        user.PasswordHash = PasswordHasher.HashPassword(request.NewPassword);
        user.MustChangePassword = request.ForceChangeOnNextLogin;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogActionAsync(
            adminEmployeeId,
            "PasswordReset",
            "User",
            employeeId,
            changesJson: $"{{\"forceChangeOnNextLogin\":{request.ForceChangeOnNextLogin.ToString().ToLower()}}}"
        );

        _logger.LogInformation("Password reset for user {EmployeeId} by admin {AdminId}", employeeId, adminEmployeeId);
    }

    /// <summary>
    /// Lock user account
    /// </summary>
    public async Task LockAccountAsync(string employeeId, string? reason, string adminEmployeeId)
    {
        // Protect the primary SuperAdmin account
        ValidateNotProtectedSuperAdmin(employeeId, "lock account for");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User {employeeId} not found");
        }

        user.IsLocked = true;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogActionAsync(
            adminEmployeeId,
            "AccountLocked",
            "User",
            employeeId,
            changesJson: $"{{\"reason\":\"{reason ?? "Not specified"}\"}}"
        );

        _logger.LogInformation("User {EmployeeId} locked by admin {AdminId}", employeeId, adminEmployeeId);
    }

    /// <summary>
    /// Unlock user account
    /// </summary>
    public async Task UnlockAccountAsync(string employeeId, string adminEmployeeId)
    {
        // Protect the primary SuperAdmin account
        ValidateNotProtectedSuperAdmin(employeeId, "unlock account for");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User {employeeId} not found");
        }

        user.IsLocked = false;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogActionAsync(
            adminEmployeeId,
            "AccountUnlocked",
            "User",
            employeeId
        );

        _logger.LogInformation("User {EmployeeId} unlocked by admin {AdminId}", employeeId, adminEmployeeId);
    }

    /// <summary>
    /// Get user login history (last 100 logins)
    /// </summary>
    public async Task<List<UserLoginHistoryDto>> GetUserLoginHistoryAsync(string employeeId, int limit = 100)
    {
        // Note: This would require a separate LoginHistory table to be implemented
        // For now, return empty list as placeholder
        return new List<UserLoginHistoryDto>();
    }

    #endregion

    #region Role Management

    /// <summary>
    /// Get all roles with user counts
    /// </summary>
    public async Task<List<RoleDto>> GetAllRolesAsync()
    {
        var roles = await _context.Roles
            .Select(r => new RoleDto
            {
                Id = r.Id,
                Name = r.Name,
                Description = r.Description,
                IsActive = r.IsActive,
                UserCount = r.UserRoles.Count,
                CreatedAt = r.CreatedAt,
                UpdatedAt = r.UpdatedAt
            })
            .OrderBy(r => r.Name)
            .ToListAsync();

        return roles;
    }

    /// <summary>
    /// Assign role to user
    /// </summary>
    public async Task AssignRoleAsync(AssignRoleRequest request, string adminEmployeeId)
    {
        // Protect the primary SuperAdmin account
        ValidateNotProtectedSuperAdmin(request.EmployeeId, "assign role to");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == request.EmployeeId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User {request.EmployeeId} not found");
        }

        if (!Enum.TryParse<Role>(request.Role, true, out var newRole))
        {
            throw new InvalidOperationException($"Invalid role: {request.Role}");
        }

        var oldRole = user.Role.ToString();
        user.Role = newRole;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogActionAsync(
            adminEmployeeId,
            "RoleAssigned",
            "User",
            request.EmployeeId,
            changesJson: $"{{\"oldRole\":\"{oldRole}\",\"newRole\":\"{newRole}\",\"reason\":\"{request.Reason ?? "Not specified"}\"}}"
        );

        _logger.LogInformation("Role {Role} assigned to user {EmployeeId} by admin {AdminId}",
            request.Role, request.EmployeeId, adminEmployeeId);
    }

    /// <summary>
    /// Remove role from user (set to User role)
    /// </summary>
    public async Task RemoveRoleAsync(string employeeId, string? reason, string adminEmployeeId)
    {
        // Protect the primary SuperAdmin account
        ValidateNotProtectedSuperAdmin(employeeId, "remove role from");

        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == employeeId);
        if (user == null)
        {
            throw new KeyNotFoundException($"User {employeeId} not found");
        }

        var oldRole = user.Role.ToString();
        user.Role = Role.User;
        user.UpdatedAt = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogActionAsync(
            adminEmployeeId,
            "RoleRemoved",
            "User",
            employeeId,
            changesJson: $"{{\"oldRole\":\"{oldRole}\",\"newRole\":\"User\",\"reason\":\"{reason ?? "Not specified"}\"}}"
        );

        _logger.LogInformation("Role removed from user {EmployeeId} by admin {AdminId}", employeeId, adminEmployeeId);
    }

    #endregion

    #region Access Control

    /// <summary>
    /// Get access matrix for all roles
    /// </summary>
    public async Task<List<AccessMatrixDto>> GetAccessMatrixAsync()
    {
        // This would require an AccessMatrix table to be implemented
        // For now, return default permissions based on role hierarchy
        var matrix = new List<AccessMatrixDto>();

        var modules = new[] { "Users", "Roles", "Attendance", "Exceptions", "Warnings", "Escalations", "Reports", "System" };
        var roles = Enum.GetValues<Role>();

        foreach (var role in roles)
        {
            foreach (var module in modules)
            {
                matrix.Add(new AccessMatrixDto
                {
                    Role = role.ToString(),
                    Module = module,
                    CanView = GetDefaultViewPermission(role, module),
                    CanCreate = GetDefaultCreatePermission(role, module),
                    CanEdit = GetDefaultEditPermission(role, module),
                    CanDelete = GetDefaultDeletePermission(role, module),
                    CanApprove = GetDefaultApprovePermission(role, module),
                    CanExport = GetDefaultExportPermission(role, module)
                });
            }
        }

        return matrix;
    }

    /// <summary>
    /// Update access matrix for a role and module
    /// </summary>
    public async Task UpdateAccessMatrixAsync(UpdateAccessMatrixRequest request, string adminEmployeeId)
    {
        // This would require an AccessMatrix table to be implemented
        // For now, log the action
        await _auditService.LogActionAsync(
            adminEmployeeId,
            "AccessMatrixUpdated",
            "AccessMatrix",
            $"{request.Role}:{request.Module}",
            changesJson: System.Text.Json.JsonSerializer.Serialize(request)
        );

        _logger.LogInformation("Access matrix updated for role {Role}, module {Module} by admin {AdminId}",
            request.Role, request.Module, adminEmployeeId);
    }

    private bool GetDefaultViewPermission(WACAMS.Core.Enums.Role role, string module)
    {
        return role >= WACAMS.Core.Enums.Role.User;
    }

    private bool GetDefaultCreatePermission(WACAMS.Core.Enums.Role role, string module)
    {
        return role >= WACAMS.Core.Enums.Role.Supervisor && module != "System";
    }

    private bool GetDefaultEditPermission(WACAMS.Core.Enums.Role role, string module)
    {
        return role >= WACAMS.Core.Enums.Role.Manager && module != "System";
    }

    private bool GetDefaultDeletePermission(WACAMS.Core.Enums.Role role, string module)
    {
        return role >= WACAMS.Core.Enums.Role.Admin && module != "System";
    }

    private bool GetDefaultApprovePermission(WACAMS.Core.Enums.Role role, string module)
    {
        return role >= WACAMS.Core.Enums.Role.Supervisor && (module == "Exceptions" || module == "Warnings" || module == "Escalations");
    }

    private bool GetDefaultExportPermission(WACAMS.Core.Enums.Role role, string module)
    {
        return role >= WACAMS.Core.Enums.Role.Manager;
    }

    #endregion

    #region Employee Access Overrides

    /// <summary>
    /// Get all employee access overrides
    /// </summary>
    public async Task<List<EmployeeOverrideDto>> GetAllOverridesAsync()
    {
        var overrides = await _context.EmployeeAccessOverrides
            .Include(o => o.Employee)
            .Include(o => o.CreatedByEmployee)
            .OrderByDescending(o => o.CreatedDate)
            .Select(o => new EmployeeOverrideDto
            {
                Id = o.Id,
                EmployeeId = o.EmployeeId,
                DisplayName = o.Employee != null ? o.Employee.DisplayName : "Unknown",
                OverrideRole = o.OverrideRole,
                AttendanceRuleOverride = o.AttendanceRuleOverride,
                EscalationOverride = o.EscalationOverride,
                DisciplinaryOverride = o.DisciplinaryOverride,
                DashboardOverride = o.DashboardOverride,
                EffectiveFrom = o.EffectiveFrom,
                EffectiveTo = o.EffectiveTo,
                Notes = o.Notes,
                CreatedByEmployeeId = o.CreatedByEmployeeId,
                CreatedDate = o.CreatedDate,
                UpdatedDate = o.UpdatedDate,
                IsActive = o.IsActive
            })
            .ToListAsync();

        return overrides;
    }

    /// <summary>
    /// Get override by ID
    /// </summary>
    public async Task<EmployeeOverrideDto?> GetOverrideByIdAsync(Guid id)
    {
        var overrideEntity = await _context.EmployeeAccessOverrides
            .Include(o => o.Employee)
            .Include(o => o.CreatedByEmployee)
            .FirstOrDefaultAsync(o => o.Id == id);

        if (overrideEntity == null) return null;

        return new EmployeeOverrideDto
        {
            Id = overrideEntity.Id,
            EmployeeId = overrideEntity.EmployeeId,
            DisplayName = overrideEntity.Employee?.DisplayName ?? "Unknown",
            OverrideRole = overrideEntity.OverrideRole,
            AttendanceRuleOverride = overrideEntity.AttendanceRuleOverride,
            EscalationOverride = overrideEntity.EscalationOverride,
            DisciplinaryOverride = overrideEntity.DisciplinaryOverride,
            DashboardOverride = overrideEntity.DashboardOverride,
            EffectiveFrom = overrideEntity.EffectiveFrom,
            EffectiveTo = overrideEntity.EffectiveTo,
            Notes = overrideEntity.Notes,
            CreatedByEmployeeId = overrideEntity.CreatedByEmployeeId,
            CreatedDate = overrideEntity.CreatedDate,
            UpdatedDate = overrideEntity.UpdatedDate,
            IsActive = overrideEntity.IsActive
        };
    }

    /// <summary>
    /// Create employee access override
    /// </summary>
    public async Task<EmployeeOverrideDto> CreateOverrideAsync(CreateEmployeeOverrideRequest request, string adminEmployeeId)
    {
        // Validate employee exists
        var employee = await _context.Employees.FirstOrDefaultAsync(e => e.EmployeeId == request.EmployeeId);
        if (employee == null)
        {
            throw new KeyNotFoundException($"Employee {request.EmployeeId} not found");
        }

        // Validate role
        if (!Enum.TryParse<Role>(request.OverrideRole, true, out var overrideRole))
        {
            throw new InvalidOperationException($"Invalid role: {request.OverrideRole}");
        }

        var overrideEntity = new EmployeeAccessOverride
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            OverrideRole = request.OverrideRole,
            AttendanceRuleOverride = request.AttendanceRuleOverride,
            EscalationOverride = request.EscalationOverride,
            DisciplinaryOverride = request.DisciplinaryOverride,
            DashboardOverride = request.DashboardOverride,
            EffectiveFrom = request.EffectiveFrom,
            EffectiveTo = request.EffectiveTo,
            Notes = request.Notes,
            CreatedByEmployeeId = adminEmployeeId,
            CreatedDate = DateTime.UtcNow,
            IsActive = true
        };

        _context.EmployeeAccessOverrides.Add(overrideEntity);
        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogActionAsync(
            adminEmployeeId,
            "OverrideCreated",
            "EmployeeAccessOverride",
            overrideEntity.Id.ToString(),
            changesJson: System.Text.Json.JsonSerializer.Serialize(new
            {
                employeeId = request.EmployeeId,
                overrideRole = request.OverrideRole,
                effectiveFrom = request.EffectiveFrom,
                effectiveTo = request.EffectiveTo
            })
        );

        _logger.LogInformation("Override created for employee {EmployeeId} with role {Role} by admin {AdminId}",
            request.EmployeeId, request.OverrideRole, adminEmployeeId);

        return await GetOverrideByIdAsync(overrideEntity.Id) ?? throw new Exception("Failed to retrieve created override");
    }

    /// <summary>
    /// Update employee access override
    /// </summary>
    public async Task<EmployeeOverrideDto> UpdateOverrideAsync(Guid id, UpdateEmployeeOverrideRequest request, string adminEmployeeId)
    {
        var overrideEntity = await _context.EmployeeAccessOverrides.FirstOrDefaultAsync(o => o.Id == id);
        if (overrideEntity == null)
        {
            throw new KeyNotFoundException($"Override {id} not found");
        }

        var changes = new List<string>();

        if (request.OverrideRole != null && request.OverrideRole != overrideEntity.OverrideRole)
        {
            var oldRole = overrideEntity.OverrideRole;
            overrideEntity.OverrideRole = request.OverrideRole;
            changes.Add($"OverrideRole:{oldRole}->{request.OverrideRole}");
        }

        if (request.AttendanceRuleOverride != null && request.AttendanceRuleOverride != overrideEntity.AttendanceRuleOverride)
        {
            overrideEntity.AttendanceRuleOverride = request.AttendanceRuleOverride;
            changes.Add("AttendanceRuleOverride:updated");
        }

        if (request.EscalationOverride != null && request.EscalationOverride != overrideEntity.EscalationOverride)
        {
            overrideEntity.EscalationOverride = request.EscalationOverride;
            changes.Add("EscalationOverride:updated");
        }

        if (request.DisciplinaryOverride != null && request.DisciplinaryOverride != overrideEntity.DisciplinaryOverride)
        {
            overrideEntity.DisciplinaryOverride = request.DisciplinaryOverride;
            changes.Add("DisciplinaryOverride:updated");
        }

        if (request.DashboardOverride != null && request.DashboardOverride != overrideEntity.DashboardOverride)
        {
            overrideEntity.DashboardOverride = request.DashboardOverride;
            changes.Add("DashboardOverride:updated");
        }

        if (request.EffectiveFrom.HasValue && request.EffectiveFrom.Value != overrideEntity.EffectiveFrom)
        {
            overrideEntity.EffectiveFrom = request.EffectiveFrom.Value;
            changes.Add($"EffectiveFrom:{request.EffectiveFrom.Value}");
        }

        if (request.EffectiveTo.HasValue && request.EffectiveTo.Value != overrideEntity.EffectiveTo)
        {
            overrideEntity.EffectiveTo = request.EffectiveTo.Value;
            changes.Add($"EffectiveTo:{request.EffectiveTo.Value}");
        }

        if (request.Notes != null && request.Notes != overrideEntity.Notes)
        {
            overrideEntity.Notes = request.Notes;
            changes.Add("Notes:updated");
        }

        if (request.IsActive.HasValue && request.IsActive.Value != overrideEntity.IsActive)
        {
            overrideEntity.IsActive = request.IsActive.Value;
            changes.Add($"IsActive:{request.IsActive.Value}");
        }

        overrideEntity.UpdatedDate = DateTime.UtcNow;
        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogActionAsync(
            adminEmployeeId,
            "OverrideUpdated",
            "EmployeeAccessOverride",
            id.ToString(),
            changesJson: $"{{\"changes\":[{string.Join(",", changes.Select(c => $"\"{c}\""))}]}}"
        );

        _logger.LogInformation("Override {Id} updated by admin {AdminId}", id, adminEmployeeId);

        return await GetOverrideByIdAsync(id) ?? throw new Exception("Failed to retrieve updated override");
    }

    /// <summary>
    /// Delete employee access override
    /// </summary>
    public async Task DeleteOverrideAsync(Guid id, string adminEmployeeId)
    {
        var overrideEntity = await _context.EmployeeAccessOverrides.FirstOrDefaultAsync(o => o.Id == id);
        if (overrideEntity == null)
        {
            throw new KeyNotFoundException($"Override {id} not found");
        }

        _context.EmployeeAccessOverrides.Remove(overrideEntity);
        await _context.SaveChangesAsync();

        // Audit log
        await _auditService.LogActionAsync(
            adminEmployeeId,
            "OverrideDeleted",
            "EmployeeAccessOverride",
            id.ToString(),
            changesJson: $"{{\"employeeId\":\"{overrideEntity.EmployeeId}\",\"overrideRole\":\"{overrideEntity.OverrideRole}\"}}"
        );

        _logger.LogInformation("Override {Id} deleted by admin {AdminId}", id, adminEmployeeId);
    }

    #endregion

    #region SuperAdmin Creation Workflow

    /// <summary>
    /// Create a new SuperAdmin - only the protected SuperAdmin (803341) can do this.
    /// Requires reason, audit trail, and timestamp.
    /// </summary>
    public async Task<UserManagementDto> CreateSuperAdminAsync(
        SuperAdminCreationRequest request,
        string requestingEmployeeId)
    {
        // Only the protected SuperAdmin can create new SuperAdmins
        if (requestingEmployeeId != ProtectedSuperAdminId)
        {
            throw new InvalidOperationException(
                $"Only Employee ID {ProtectedSuperAdminId} can create new SuperAdmin accounts");
        }

        // Check if user already exists
        if (await _context.Users.AnyAsync(u => u.EmployeeId == request.EmployeeId))
        {
            throw new InvalidOperationException(
                $"User with EmployeeId {request.EmployeeId} already exists");
        }

        var user = new User
        {
            Id = Guid.NewGuid(),
            EmployeeId = request.EmployeeId,
            DisplayName = request.DisplayName,
            Email = $"{request.EmployeeId}@wacams.local",
            PasswordHash = PasswordHasher.HashPassword(Guid.NewGuid().ToString()),
            Role = Role.SuperAdmin,
            IsActive = true,
            IsLocked = false,
            MustChangePassword = true,
            CreatedAt = DateTime.UtcNow
        };

        _context.Users.Add(user);
        await _context.SaveChangesAsync();

        // Audit log with full details
        await _auditService.LogActionAsync(
            requestingEmployeeId,
            "SuperAdminCreated",
            "User",
            user.EmployeeId,
            changesJson: System.Text.Json.JsonSerializer.Serialize(new
            {
                action = "create_superadmin",
                newEmployeeId = request.EmployeeId,
                displayName = request.DisplayName,
                department = request.Department,
                reason = request.Reason,
                timestamp = DateTime.UtcNow
            })
        );

        _logger.LogWarning(
            "NEW SUPERADMIN CREATED: {NewEmployeeId} ({DisplayName}) by {CreatorId}. Reason: {Reason}",
            request.EmployeeId, request.DisplayName, requestingEmployeeId, request.Reason);

        return await GetUserByEmployeeIdAsync(request.EmployeeId)
            ?? throw new Exception("Failed to retrieve created SuperAdmin user");
    }

    /// <summary>
    /// Get SuperAdmin protection status for the protected account.
    /// </summary>
    public async Task<SuperAdminProtectionDto> GetProtectionStatusAsync()
    {
        var user = await _context.Users.FirstOrDefaultAsync(u => u.EmployeeId == ProtectedSuperAdminId);

        return new SuperAdminProtectionDto
        {
            ProtectedEmployeeId = ProtectedSuperAdminId,
            DisplayName = user?.DisplayName ?? "Botlhale Rakau",
            Role = "SuperAdmin",
            Department = "Production",
            SecurityScope = "Global",
            IsProtected = true,
            ProtectionRules = new List<string>
            {
                "Cannot be deleted",
                "Cannot be disabled",
                "Cannot be downgraded",
                "Cannot lose SuperAdmin role",
                "Cannot be modified by non-SuperAdmin users",
                "All modification attempts must be audit logged"
            }
        };
    }

    #endregion

    #region Security Audit Logs

    /// <summary>
    /// Get security audit logs
    /// </summary>
    public async Task<(List<SecurityAuditLogDto> Logs, int TotalCount)> GetSecurityAuditLogsAsync(SecurityAuditQuery query)
    {
        var auditQuery = _context.AuditLogs
            .Where(a => a.Action.StartsWith("User") ||
                       a.Action.StartsWith("Role") ||
                       a.Action.StartsWith("Account") ||
                       a.Action.StartsWith("Password") ||
                       a.Action.StartsWith("Override") ||
                       a.Action.StartsWith("AccessMatrix"))
            .AsQueryable();

        if (!string.IsNullOrWhiteSpace(query.EmployeeId))
        {
            auditQuery = auditQuery.Where(a => a.EntityId == query.EmployeeId);
        }

        if (!string.IsNullOrWhiteSpace(query.Action))
        {
            auditQuery = auditQuery.Where(a => a.Action == query.Action);
        }

        if (query.StartDate.HasValue)
        {
            auditQuery = auditQuery.Where(a => a.CreatedAt >= query.StartDate.Value);
        }

        if (query.EndDate.HasValue)
        {
            auditQuery = auditQuery.Where(a => a.CreatedAt <= query.EndDate.Value);
        }

        var totalCount = await auditQuery.CountAsync();

        var logs = await auditQuery
            .OrderByDescending(a => a.CreatedAt)
            .Skip((query.Page - 1) * query.PageSize)
            .Take(query.PageSize)
            .Select(a => new SecurityAuditLogDto
            {
                Id = a.Id,
                ChangedEmployeeId = a.EntityId,
                ChangedEmployeeName = "", // Would need to join with User table
                OldRole = null, // Would need to parse from ChangesJson
                NewRole = null, // Would need to parse from ChangesJson
                ChangedByEmployeeId = a.PerformedBy ?? "system",
                ChangedByEmployeeName = "", // Would need to join with User table
                Action = a.Action,
                Reason = null, // Would need to parse from ChangesJson
                Timestamp = a.CreatedAt
            })
            .ToListAsync();

        return (logs, totalCount);
    }

    #endregion
}
